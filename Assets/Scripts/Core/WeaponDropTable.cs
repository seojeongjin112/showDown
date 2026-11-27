using System;
using System.Collections.Generic;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace ArenaBreak.Core
{
    // 저장된 에셋의 값이 밀리지 않도록 순서를 바꾸지 않고 뒤에만 추가한다
    public enum WeaponRarity
    {
        Common,
        Rare,
        Legendary
    }

    // 굴림 결과. 무기 그림과 탄약 수를 함께 넘겨서 드랍하는 쪽이 표를 다시 뒤지지 않게 한다
    public readonly struct WeaponDrop
    {
        public WeaponDrop(WeaponData weapon, int ammoWhenOwned, Sprite[] frames)
        {
            Weapon = weapon;
            AmmoWhenOwned = ammoWhenOwned;
            Frames = frames;
        }

        public WeaponData Weapon { get; }
        public int AmmoWhenOwned { get; }
        public Sprite[] Frames { get; }
    }

    // 스테이지마다 후보 무기가 다른 표를 하나씩 둔다. 먼저 등급을 뽑고 그 등급 안에서 무기를 골라서,
    // 무기를 추가해도 등급만 붙이면 전체 균형이 크게 흔들리지 않는다
    [CreateAssetMenu(fileName = "WeaponDropTable", menuName = "ArenaBreak/Weapon Drop Table")]
    public class WeaponDropTable : ScriptableObject
    {
        private const int RarityCount = 3;

        [Serializable]
        private class Entry
        {
            [SerializeField] private WeaponData _weapon;
            [SerializeField] private WeaponRarity _rarity;
            [Tooltip("같은 등급 안에서의 비율. 기본은 1로 모두 같다")]
            [SerializeField] private float _weight = 1f;
            [Tooltip("이미 가진 무기가 나오면 무기 대신 이만큼의 예비 탄약을 준다")]
            [SerializeField] private int _ammoWhenOwned = 6;
            [Tooltip("바닥에 떨어졌을 때 도는 그림 프레임")]
            [SerializeField] private Sprite[] _dropFrames;

            public WeaponData Weapon => _weapon;
            public WeaponRarity Rarity => _rarity;
            public float Weight => _weight;
            public int AmmoWhenOwned => _ammoWhenOwned;
            public Sprite[] DropFrames => _dropFrames;
        }

        [SerializeField] private Entry[] _entries;

        [Header("등급이 나올 비율")]
        [SerializeField] private float _commonWeight = 60f;
        [SerializeField] private float _rareWeight = 30f;
        [SerializeField] private float _legendaryWeight = 10f;

        [Header("고르기 보정")]
        [Tooltip("아직 없는 무기의 가중치 배수. 새 무기가 더 자주 나와야 조합이 늘어난다")]
        [SerializeField] private float _unownedWeightMultiplier = 2f;
        [Tooltip("이미 가진 무기의 가중치 배수. 같은 무기만 계속 나오지 않게 낮춘다")]
        [SerializeField] private float _ownedWeightMultiplier = 0.5f;

        [Header("보장")]
        [Tooltip("칼만 들고 있을 때, 이 수만큼 처치하는 동안 안 나오면 반드시 나온다. 첫 무기는 일반 등급만 나온다")]
        [SerializeField] private int _firstDropGuaranteeKills = 20;
        [Tooltip("그 뒤로는 이 수만큼 처치하는 동안 안 나오면 반드시 나온다")]
        [SerializeField] private int _guaranteeKills = 40;

        private readonly List<float> _weightBuffer = new List<float>();
        private readonly float[] _rarityTotals = new float[RarityCount];
        // 보정 배수를 빼고 센 합. 이미 가진 무기뿐인 등급은 평균 배수가 낮아져 덜 뽑히게 하려고 둔다
        private readonly float[] _rarityBase = new float[RarityCount];
        // 에셋에 저장되면 다음 플레이에 이어지므로 저장하지 않는다
        [NonSerialized] private int _killsSinceDrop;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // 죽어서 씬을 다시 불러오면 처음부터 다시 시작하므로 보장 횟수도 처음으로 돌린다
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _killsSinceDrop = 0;
        }

        // 처치 한 번마다 호출한다. 떨어뜨리면 true이고, 무엇을 떨어뜨리는지를 drop으로 돌려준다.
        // rarityBoost가 1보다 크면 강한 적일수록 희귀한 등급이 더 잘 나온다
        public bool TryRoll(float dropChance, float rarityBoost, WeaponInventory inventory, out WeaponDrop drop)
        {
            drop = default;
            _killsSinceDrop++;

            int ownedCount = CountOwned(inventory);
            int guarantee = ownedCount <= 1 ? _firstDropGuaranteeKills : _guaranteeKills;
            bool guaranteed = _killsSinceDrop >= guarantee;

            if (!guaranteed && Random.value >= dropChance)
            {
                return false;
            }

            // 칼만 든 처음에는 근접 무기가 나오면 칼과 바뀔 뿐이라 첫 무기로 쓸 수 없다
            bool firstWeapon = ownedCount <= 1;
            FillWeights(inventory, firstWeapon);

            int rarity = PickRarity(rarityBoost, onlyCommon: firstWeapon);
            if (rarity < 0)
            {
                // 줄 수 있는 것이 없으면(모두 탄약이 가득) 보장 횟수를 유지해 다음 처치에서 다시 시도한다
                return false;
            }

            int entry = PickEntry(rarity);
            if (entry < 0)
            {
                return false;
            }

            drop = new WeaponDrop(_entries[entry].Weapon, _entries[entry].AmmoWhenOwned, _entries[entry].DropFrames);
            _killsSinceDrop = 0;
            return true;
        }

        private void FillWeights(WeaponInventory inventory, bool excludeMelee)
        {
            _weightBuffer.Clear();
            Array.Clear(_rarityTotals, 0, RarityCount);
            Array.Clear(_rarityBase, 0, RarityCount);

            for (int i = 0; i < _entries.Length; i++)
            {
                bool skip = excludeMelee && _entries[i].Weapon.SlotType == WeaponSlotType.Melee;
                float weight = skip ? 0f : WeightOf(_entries[i], inventory);
                _weightBuffer.Add(weight);
                _rarityTotals[(int)_entries[i].Rarity] += weight;

                if (weight > 0f)
                {
                    _rarityBase[(int)_entries[i].Rarity] += _entries[i].Weight;
                }
            }
        }

        // 등급 안에 아직 없는 무기가 많을수록 크고, 이미 가진 무기뿐이면 작은 배수
        private float RarityFactor(WeaponRarity rarity)
        {
            int index = (int)rarity;
            return _rarityBase[index] > 0f ? _rarityTotals[index] / _rarityBase[index] : 0f;
        }

        private float WeightOf(Entry entry, WeaponInventory inventory)
        {
            int index = inventory.IndexOf(entry.Weapon);
            if (index < 0)
            {
                return 0f;
            }

            if (!inventory.IsOwned(index))
            {
                return entry.Weight * _unownedWeightMultiplier;
            }

            // 예비 탄약이 가득하면 주워도 효과가 없으므로 후보에서 뺀다
            return inventory.CanAddAmmo(entry.Weapon) ? entry.Weight * _ownedWeightMultiplier : 0f;
        }

        // 후보 무기가 하나도 없는 등급은 뽑히지 않게 0으로 둔다
        private int PickRarity(float rarityBoost, bool onlyCommon)
        {
            float boost = Mathf.Max(1f, rarityBoost);
            float common = _commonWeight * RarityFactor(WeaponRarity.Common);
            float rare = _rareWeight * boost * RarityFactor(WeaponRarity.Rare);
            float legendary = _legendaryWeight * boost * boost * RarityFactor(WeaponRarity.Legendary);

            // 첫 무기는 가장 약한 등급에서만 나와야 처음부터 강한 무기로 시작하지 않는다
            if (onlyCommon && _rarityTotals[(int)WeaponRarity.Common] > 0f)
            {
                rare = 0f;
                legendary = 0f;
            }

            float total = common + rare + legendary;
            if (total <= 0f)
            {
                return -1;
            }

            float roll = Random.value * total;
            if (roll < common)
            {
                return (int)WeaponRarity.Common;
            }

            return roll < common + rare ? (int)WeaponRarity.Rare : (int)WeaponRarity.Legendary;
        }

        private int PickEntry(int rarity)
        {
            float roll = Random.value * _rarityTotals[rarity];
            int last = -1;

            for (int i = 0; i < _entries.Length; i++)
            {
                if ((int)_entries[i].Rarity != rarity || _weightBuffer[i] <= 0f)
                {
                    continue;
                }

                last = i;
                roll -= _weightBuffer[i];
                if (roll <= 0f)
                {
                    return i;
                }
            }

            // 부동소수점 오차로 끝까지 못 고르면 그 등급의 마지막 후보를 준다
            return last;
        }

        private static int CountOwned(WeaponInventory inventory)
        {
            int owned = 0;
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory.IsOwned(i))
                {
                    owned++;
                }
            }

            return owned;
        }
    }
}
