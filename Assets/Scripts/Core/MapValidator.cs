#if UNITY_EDITOR
using System.Collections.Generic;
using ArenaBreak.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Core
{
    // 플레이하지 않고 맵 데이터 오류를 한 번에 잡는다: 스폰 포인트가 NavMesh 밖이거나 건물 속이거나 갇혀 있는 경우, 구성 데이터의 빈 칸 등
    public static class MapValidator
    {
        private const float OnMeshDistance = 0.6f;
        private const float SolidRadius = 0.3f;
        private const float TargetSnapDistance = 8f;
        private const float ProbeHeight = 1f;

        private static int _errors;
        private static int _warnings;
        private static int _checkedPoints;

        [MenuItem("ArenaBreak/맵 검증")]
        private static void RunFromMenu()
        {
            Run();
        }

        // 문제 개수를 돌려줘서 에디터 메뉴 밖(스크립트)에서도 쓸 수 있다
        public static int Run()
        {
            _errors = 0;
            _warnings = 0;
            _checkedPoints = 0;

            if (Application.isPlaying)
            {
                Debug.LogWarning("MapValidator: 플레이 모드에서는 씬을 점검하지 않는다. 편집 모드에서 실행한다.");
                return 0;
            }

            if (NavMesh.CalculateTriangulation().vertices.Length == 0)
            {
                Error("NavMesh가 비어 있다. 굽지 않았거나 지워졌다.", null);
                return Finish();
            }

            var prefabs = new HashSet<GameObject>();

            EncounterZone[] zones = Object.FindObjectsByType<EncounterZone>(FindObjectsSortMode.None);
            for (int i = 0; i < zones.Length; i++)
            {
                CheckZone(zones[i], prefabs);
            }

            WaveArena[] arenas = Object.FindObjectsByType<WaveArena>(FindObjectsSortMode.None);
            for (int i = 0; i < arenas.Length; i++)
            {
                CheckArena(arenas[i], prefabs);
            }

            foreach (GameObject prefab in prefabs)
            {
                CheckEnemyPrefab(prefab);
            }

            CheckPlayerStart(zones, arenas);
            return Finish();
        }

        private static void CheckZone(EncounterZone zone, HashSet<GameObject> prefabs)
        {
            var so = new SerializedObject(zone);
            var group = so.FindProperty("_group").objectReferenceValue as WaveData;

            if (group == null)
            {
                Error($"{PathOf(zone)}: 적 구성 데이터가 비어 있다.", zone);
            }
            else
            {
                CollectPrefabs(group, prefabs, zone);
            }

            if (so.FindProperty("_maxAlive").intValue < 1)
            {
                Error($"{PathOf(zone)}: 동시 생존 상한이 1보다 작아 적이 나오지 않는다.", zone);
            }

            CheckSpawnPoints(so.FindProperty("_spawnPoints"), zone.transform.position, PathOf(zone), zone);
        }

        private static void CheckArena(WaveArena arena, HashSet<GameObject> prefabs)
        {
            var so = new SerializedObject(arena);
            SerializedProperty waves = so.FindProperty("_waves");

            if (waves.arraySize == 0)
            {
                Error($"{PathOf(arena)}: 웨이브 데이터가 비어 있다.", arena);
            }

            for (int i = 0; i < waves.arraySize; i++)
            {
                var wave = waves.GetArrayElementAtIndex(i).objectReferenceValue as WaveData;
                if (wave == null)
                {
                    Error($"{PathOf(arena)}: {i + 1}번째 웨이브가 비어 있다.", arena);
                    continue;
                }

                CollectPrefabs(wave, prefabs, arena);
            }

            if (so.FindProperty("_entranceGate").objectReferenceValue == null)
            {
                Warn($"{PathOf(arena)}: 입구 문이 없어 웨이브 중에도 빠져나갈 수 있다.", arena);
            }

            if (so.FindProperty("_exitGate").objectReferenceValue == null)
            {
                Warn($"{PathOf(arena)}: 출구 문이 없어 웨이브를 안 끝내도 지나갈 수 있다.", arena);
            }

            CheckSpawnPoints(so.FindProperty("_spawnPoints"), arena.transform.position, PathOf(arena), arena);
        }

        private static void CheckSpawnPoints(SerializedProperty points, Vector3 target, string owner, Object context)
        {
            if (points.arraySize == 0)
            {
                Error($"{owner}: 스폰 포인트가 비어 있다.", context);
                return;
            }

            bool hasTarget = NavMesh.SamplePosition(target, out NavMeshHit targetHit, TargetSnapDistance, NavMesh.AllAreas);

            for (int i = 0; i < points.arraySize; i++)
            {
                var point = points.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (point == null)
                {
                    Error($"{owner}: {i + 1}번 스폰 포인트가 비어 있다.", context);
                    continue;
                }

                _checkedPoints++;
                Vector3 position = point.position;

                if (!NavMesh.SamplePosition(position, out NavMeshHit onMesh, OnMeshDistance, NavMesh.AllAreas))
                {
                    Error($"{owner}/{point.name}: NavMesh 밖이다. 적이 이동하지 못한다.", point);
                    continue;
                }

                if (IsInsideSolid(position))
                {
                    Error($"{owner}/{point.name}: 건물이나 바위 속이다. 적이 갇혀 나온다.", point);
                    continue;
                }

                if (!hasTarget)
                {
                    continue;
                }

                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(onMesh.position, targetHit.position, NavMesh.AllAreas, path)
                    || path.status != NavMeshPathStatus.PathComplete)
                {
                    Error($"{owner}/{point.name}: 중앙까지 걸어갈 수 없는 갇힌 자리다.", point);
                }
            }
        }

        private static bool IsInsideSolid(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(position + Vector3.up * ProbeHeight, SolidRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                // 바닥과 피해를 받는 대상은 고체 장애물로 보지 않는다
                if (hits[i].gameObject.name == "Ground" || hits[i].GetComponentInParent<IDamageable>() != null)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void CollectPrefabs(WaveData data, HashSet<GameObject> prefabs, Object context)
        {
            for (int i = 0; i < data.Entries.Count; i++)
            {
                GameObject prefab = data.Entries[i].EnemyPrefab;
                if (prefab == null)
                {
                    Error($"{data.name}: {i}번 항목에 적 프리팹이 없다.", context);
                    continue;
                }

                prefabs.Add(prefab);
            }
        }

        private static void CheckEnemyPrefab(GameObject prefab)
        {
            if (prefab.GetComponent<Health>() == null)
            {
                Error($"{prefab.name}: Health가 없어 처치를 셀 수 없다(웨이브가 끝나지 않는다).", prefab);
            }

            if (prefab.GetComponent<NavMeshAgent>() == null)
            {
                Error($"{prefab.name}: NavMeshAgent가 없어 움직이지 못한다.", prefab);
            }

            int movement = 0;
            int attack = 0;
            MonoBehaviour[] behaviours = prefab.GetComponents<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEnemyMovement)
                {
                    movement++;
                }

                if (behaviours[i] is IEnemyAttack)
                {
                    attack++;
                }
            }

            if (movement != 1)
            {
                Error($"{prefab.name}: 이동 컴포넌트가 {movement}개다(1개여야 한다).", prefab);
            }

            if (attack != 1)
            {
                Error($"{prefab.name}: 공격 컴포넌트가 {attack}개다(1개여야 한다).", prefab);
            }
        }

        private static void CheckPlayerStart(EncounterZone[] zones, WaveArena[] arenas)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Warn("Player 태그 오브젝트가 없어 시작점 겹침을 점검하지 못했다.", null);
                return;
            }

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].TryGetComponent(out Collider zoneCollider) && zoneCollider.bounds.Contains(player.transform.position))
                {
                    Error($"{PathOf(zones[i])}: 플레이어 시작점이 트리거 안이라 시작하자마자 적이 나온다.", zones[i]);
                }
            }

            for (int i = 0; i < arenas.Length; i++)
            {
                if (arenas[i].TryGetComponent(out Collider arenaCollider) && arenaCollider.bounds.Contains(player.transform.position))
                {
                    Error($"{PathOf(arenas[i])}: 플레이어 시작점이 트리거 안이라 시작하자마자 웨이브가 시작된다.", arenas[i]);
                }
            }
        }

        private static string PathOf(Component component)
        {
            Transform parent = component.transform.parent;
            return parent != null ? parent.name + "/" + component.name : component.name;
        }

        private static void Error(string message, Object context)
        {
            _errors++;
            Debug.LogError("[맵 검증] " + message, context);
        }

        private static void Warn(string message, Object context)
        {
            _warnings++;
            Debug.LogWarning("[맵 검증] " + message, context);
        }

        private static int Finish()
        {
            string summary = $"[맵 검증] 스폰 포인트 {_checkedPoints}개 점검 — 오류 {_errors}, 경고 {_warnings}";

            if (_errors == 0 && _warnings == 0)
            {
                Debug.Log(summary + " (이상 없음)");
            }
            else
            {
                Debug.LogWarning(summary);
            }

            return _errors + _warnings;
        }
    }
}
#endif
