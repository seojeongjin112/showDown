using System;
using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    // 풀링 대상: 사격마다 생성/파괴하면 GC가 쌓이므로 WeaponSystem이 풀에서 꺼내 쓴다
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Bullet : MonoBehaviour
    {
        // 이미지 파일 없이 단색 픽셀 스프라이트를 쓰려고 코드로 만든다. 색은 SpriteRenderer.color로 입히므로 한 장을 공유한다
        private const int SpriteTextureSize = 3;
        private static Sprite s_sharedSprite;

        [Header("Movement")]
        [SerializeField] private float _speed = 25f;
        [SerializeField] private float _lifetime = 2f;

        [Header("Hit")]
        // 플레이어 자신과 아군 레이어는 빼 둔다. 적과 벽 레이어만 포함시킨다
        [SerializeField] private LayerMask _hitLayers = ~0;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _color = new Color(1f, 0.1f, 0.1f);
        [SerializeField] private float _visualSize = 0.15f;

        private Rigidbody _body;
        private Collider _collider;
        private Vector3 _direction;
        private int _damage;
        private float _expireTime;
        private bool _released;
        private Action<Bullet> _release;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<SpriteRenderer>();
            }

            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            // 탄속이 빨라 한 물리 스텝에 얇은 적을 건너뛸 수 있다
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _collider.isTrigger = true;

            if (_renderer != null)
            {
                _renderer.sprite = GetSharedSprite();
                _renderer.color = _color;
                _renderer.transform.localScale = Vector3.one * _visualSize;
            }
            else
            {
                Debug.LogWarning("Bullet: SpriteRenderer가 없어 총알이 보이지 않습니다", this);
            }
        }

        public void Initialize(Vector3 position, Vector3 direction, int damage, Action<Bullet> release)
        {
            _direction = direction.normalized;
            _damage = damage;
            _release = release;
            _released = false;
            _expireTime = Time.time + _lifetime;

            transform.position = position;
            _body.position = position;
        }

        private void FixedUpdate()
        {
            if (Time.time >= _expireTime)
            {
                Release();
                return;
            }

            _body.MovePosition(_body.position + _direction * (_speed * Time.fixedDeltaTime));
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((_hitLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            IDamageable target = other.GetComponentInParent<IDamageable>();
            target?.TakeDamage(_damage);

            Release();
        }

        // 같은 스텝에 트리거 두 곳과 겹치면 반환이 두 번 불려 풀이 꼬이므로 막는다
        private void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            _release?.Invoke(this);
        }

        private static Sprite GetSharedSprite()
        {
            if (s_sharedSprite != null)
            {
                return s_sharedSprite;
            }

            Texture2D texture = new Texture2D(SpriteTextureSize, SpriteTextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            Color32[] pixels = new Color32[SpriteTextureSize * SpriteTextureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            // 픽셀 수 = 유닛당 픽셀 수로 두어 스프라이트가 정확히 1유닛이 되게 한다. 실제 크기는 _visualSize로 정한다
            s_sharedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, SpriteTextureSize, SpriteTextureSize),
                new Vector2(0.5f, 0.5f),
                SpriteTextureSize);

            return s_sharedSprite;
        }
    }
}
