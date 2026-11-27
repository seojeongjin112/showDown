using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 번개가 내리꽂힌 뒤 땅에 튀는 연출. 한 번 쓰고 사라지므로 번개를 여러 줄기 내리는 보스는 풀링 대상이다
    public class LightningStrikeEffect : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite[] _strike;
        private Sprite[] _impact;
        private float _framesPerSecond;
        private float _elapsed;

        // 그림 기준점은 땅에 닿는 곳이라, 땅 위 낙뢰 지점에 그대로 세운다
        public static void Spawn(Material material, Sprite[] strike, Sprite[] impact, Vector3 groundPoint, float scale, float framesPerSecond)
        {
            if (strike == null || strike.Length == 0)
            {
                return;
            }

            GameObject effect = new GameObject("LightningStrike");
            effect.transform.position = groundPoint;
            effect.transform.localScale = Vector3.one * scale;

            SpriteRenderer sprite = effect.AddComponent<SpriteRenderer>();
            sprite.sharedMaterial = material;
            sprite.sprite = strike[0];
            // 적과 이펙트가 겹쳐도 번개가 가려지지 않게 위에 그린다
            sprite.sortingOrder = 6;

            effect.AddComponent<YawBillboard>();

            LightningStrikeEffect result = effect.AddComponent<LightningStrikeEffect>();
            result._renderer = sprite;
            result._strike = strike;
            result._impact = impact;
            result._framesPerSecond = Mathf.Max(framesPerSecond, 1f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            int frame = (int)(_elapsed * _framesPerSecond);

            if (frame < _strike.Length)
            {
                _renderer.sprite = _strike[frame];
                return;
            }

            int impactFrame = frame - _strike.Length;
            if (_impact != null && impactFrame < _impact.Length)
            {
                _renderer.sprite = _impact[impactFrame];
                return;
            }

            Destroy(gameObject);
        }
    }
}
