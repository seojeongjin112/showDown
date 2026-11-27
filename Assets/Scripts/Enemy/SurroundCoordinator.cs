using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 근접 적이 각자 가장 가까운 쪽으로만 달려가면 한 줄로 몰려서, 플레이어 둘레의 각도를 나눠 주어 고르게 에워싸게 한다
    public static class SurroundCoordinator
    {
        private struct Entry
        {
            public Transform Member;
            public float Angle;
            public float Distance;
            public int Ring;
        }

        private struct Slot
        {
            public float Angle;
            public int Ring;
        }

        // 한 겹에 서는 수. 이보다 많으면 바깥 겹으로 밀어 서로 겹쳐 서지 않게 한다
        private const int SlotsPerRing = 8;
        // 이 거리 밖의 적은 아직 접근 중이라 자리를 나누지 않는다
        private const float EngageDistance = 16f;
        private const float TwoPi = Mathf.PI * 2f;

        private static readonly List<Transform> Members = new List<Transform>();
        private static readonly List<Entry> Entries = new List<Entry>();
        private static readonly Dictionary<Transform, Slot> Slots = new Dictionary<Transform, Slot>();
        private static readonly Comparison<Entry> ByAngle = (a, b) => a.Angle.CompareTo(b.Angle);
        private static readonly Comparison<Entry> ByDistance = (a, b) => a.Distance.CompareTo(b.Distance);

        private static int _computedFrame = -1;
        private static Transform _computedTarget;

        // 에디터에서 도메인 리로드 없이 재생을 시작해도 지난 판의 적이 남지 않게 비운다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Members.Clear();
            Entries.Clear();
            Slots.Clear();
            _computedFrame = -1;
            _computedTarget = null;
        }

        public static void Register(Transform member)
        {
            if (!Members.Contains(member))
            {
                Members.Add(member);
            }
        }

        public static void Unregister(Transform member)
        {
            Members.Remove(member);
            Slots.Remove(member);
        }

        // 가까이 온 근접 적에게만 자리를 준다. 아직 멀면 false라서 호출한 쪽이 평소대로 접근한다
        public static bool TryGetSlot(Transform member, Transform target, out float angle, out int ring)
        {
            Refresh(target);

            if (Slots.TryGetValue(member, out Slot slot))
            {
                angle = slot.Angle;
                ring = slot.Ring;
                return true;
            }

            angle = 0f;
            ring = 0;
            return false;
        }

        // 같은 프레임에 여러 적이 부르므로 한 번만 계산해서 나눠 쓴다
        private static void Refresh(Transform target)
        {
            if (_computedFrame == Time.frameCount && _computedTarget == target)
            {
                return;
            }

            _computedFrame = Time.frameCount;
            _computedTarget = target;

            Entries.Clear();
            Slots.Clear();

            for (int i = Members.Count - 1; i >= 0; i--)
            {
                Transform member = Members[i];
                if (member == null)
                {
                    Members.RemoveAt(i);
                    continue;
                }

                Vector3 offset = member.position - target.position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance > EngageDistance)
                {
                    continue;
                }

                Entries.Add(new Entry
                {
                    Member = member,
                    Angle = Mathf.Atan2(offset.z, offset.x),
                    Distance = distance,
                });
            }

            int count = Entries.Count;
            if (count == 0)
            {
                return;
            }

            // 가까운 쪽이 안쪽 겹을 차지해야 자리를 옮기는 거리가 짧다
            Entries.Sort(ByDistance);
            for (int k = 0; k < count; k++)
            {
                Entry entry = Entries[k];
                entry.Ring = k / SlotsPerRing;
                Entries[k] = entry;
            }

            // 현재 각도 순서대로 같은 간격의 자리를 주되, 지금 위치에 가장 잘 맞도록 원을 돌려 맞춘다
            Entries.Sort(ByAngle);

            float sumCos = 0f;
            float sumSin = 0f;
            for (int k = 0; k < count; k++)
            {
                float diff = Entries[k].Angle - TwoPi * k / count;
                sumCos += Mathf.Cos(diff);
                sumSin += Mathf.Sin(diff);
            }

            float rotation = Mathf.Atan2(sumSin, sumCos);

            for (int k = 0; k < count; k++)
            {
                Slots[Entries[k].Member] = new Slot
                {
                    Angle = rotation + TwoPi * k / count,
                    Ring = Entries[k].Ring,
                };
            }
        }
    }
}
