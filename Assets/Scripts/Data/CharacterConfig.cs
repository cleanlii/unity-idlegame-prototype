using UnityEngine;

namespace IdleGame.Character
{
    /// <summary>
    ///     角色配置数据 - ScriptableObject
    ///     用于配置不同角色的基础属性和成长曲线
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "IdleGame/Character Config")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("基础信息")]
        public string characterID; // 角色唯一ID
        public string characterName; // 角色名称
        public CharacterRarity rarity; // 稀有度
        public Sprite characterIcon; // 角色头像
        public Color characterColor = Color.white; // 角色主题色

        [Header("初始属性")]
        public float baseMaxHP = 100f; // 基础最大生命值
        public float baseAttack = 20f; // 基础攻击力
        public float baseDefense = 10f; // 基础防御力
        public float baseCriticalRate = 0.1f; // 基础暴击率
        public float baseCriticalDamage = 1.5f; // 基础暴击伤害倍率
        public float baseAttackSpeed = 1f; // 基础攻击速度

        [Header("成长曲线")]
        public float hpGrowthPerLevel = 25f; // 每级HP增长
        public float attackGrowthPerLevel = 5f; // 每级攻击增长  
        public float defenseGrowthPerLevel = 2f; // 每级防御增长
        public float critRateGrowthPerLevel = 0.002f; // 每级暴击率增长 (0.2%)

        [Header("稀有度加成")]
        [Range(0.5f, 3f)]
        public float rarityStatMultiplier = 1f; // 稀有度属性倍率

        [Header("经验设置")]
        public long baseExpRequired = 100; // 升到2级所需基础经验
        public float expGrowthFactor = 1.8f; // 经验增长指数

        [Header("特殊属性")]
        [TextArea(2, 4)]
        public string characterDescription; // 角色描述
        public bool hasSpecialAbility; // 是否有特殊技能
        public string specialAbilityName; // 特殊技能名称
        public float specialAbilityMultiplier = 1.2f; // 特殊技能伤害倍率

        /// <summary>
        ///     根据等级计算最大HP
        /// </summary>
        public float CalculateMaxHP(int level)
        {
            return (baseMaxHP + hpGrowthPerLevel * (level - 1)) * GetRarityMultiplier();
        }

        /// <summary>
        ///     根据等级计算攻击力
        /// </summary>
        public float CalculateAttack(int level)
        {
            return (baseAttack + attackGrowthPerLevel * (level - 1)) * GetRarityMultiplier();
        }

        /// <summary>
        ///     根据等级计算防御力
        /// </summary>
        public float CalculateDefense(int level)
        {
            return (baseDefense + defenseGrowthPerLevel * (level - 1)) * GetRarityMultiplier();
        }

        /// <summary>
        ///     根据等级计算暴击率
        /// </summary>
        public float CalculateCriticalRate(int level)
        {
            return Mathf.Min(0.5f, baseCriticalRate + critRateGrowthPerLevel * (level - 1));
        }

        /// <summary>
        ///     计算升级所需经验值
        /// </summary>
        public long CalculateExpRequired(int level)
        {
            if (level <= 1) return 0;
            return (long)(baseExpRequired * Mathf.Pow(level, expGrowthFactor));
        }

        /// <summary>
        ///     获取稀有度倍率
        /// </summary>
        private float GetRarityMultiplier()
        {
            return rarityStatMultiplier > 0 ? rarityStatMultiplier : GetDefaultRarityMultiplier();
        }

        /// <summary>
        ///     获取默认稀有度倍率
        /// </summary>
        private float GetDefaultRarityMultiplier()
        {
            switch (rarity)
            {
                case CharacterRarity.Common: return 1f;
                case CharacterRarity.Rare: return 1.2f;
                case CharacterRarity.Epic: return 1.5f;
                case CharacterRarity.Legendary: return 2f;
                default: return 1f;
            }
        }

        /// <summary>
        ///     获取角色战力评分
        /// </summary>
        public float CalculatePowerScore(int level)
        {
            var hp = CalculateMaxHP(level);
            var attack = CalculateAttack(level);
            var defense = CalculateDefense(level);
            var critRate = CalculateCriticalRate(level);

            return hp * 0.5f + attack * 2f + defense * 1f + critRate * 100f + level * 10f;
        }

        /// <summary>
        ///     创建基于此配置的角色数据
        /// </summary>
        public CharacterData CreateCharacterData()
        {
            return new CharacterData(this);
        }

        private void OnValidate()
        {
            // 确保ID不为空
            if (string.IsNullOrEmpty(characterID)) characterID = name.Replace(" ", "_").ToLower();

            // 确保稀有度倍率合理
            if (rarityStatMultiplier <= 0) rarityStatMultiplier = GetDefaultRarityMultiplier();
        }
    }
}