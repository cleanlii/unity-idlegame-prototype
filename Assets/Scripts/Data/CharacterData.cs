using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame.Character
{
    public class CharacterData
    {
        [Header("基础信息")]
        public string characterID; // 角色唯一ID
        public string characterName; // 角色名称
        public CharacterRarity rarity; // 稀有度
        public int level = 1; // 当前等级
        public long experience = 0; // 当前经验值
        public long expToNextLevel = 100; // 升级所需经验

        [Header("战斗属性")]
        public float maxHP = 100f; // 最大生命值
        public float currentHP = 100f; // 当前生命值
        public float baseAttack = 20f; // 基础攻击力
        public float baseDefense = 10f; // 基础防御力
        public float criticalRate = 0.1f; // 暴击率 (10%)
        public float criticalDamage = 1.5f; // 暴击伤害倍率
        public float attackSpeed = 1f; // 攻击速度

        [Header("成长属性")]
        public float hpGrowth = 25f; // 每级HP增长
        public float attackGrowth = 5f; // 每级攻击增长
        public float defenseGrowth = 2f; // 每级防御增长

        [Header("稀有度加成")]
        public float rarityHPMultiplier = 1f; // 稀有度HP倍率
        public float rarityAttackMultiplier = 1f; // 稀有度攻击倍率
        public float rarityDefenseMultiplier = 1f; // 稀有度防御倍率

        [Header("战斗统计")]
        public int totalBattles = 0; // 总战斗次数
        public int victoriesCount = 0; // 胜利次数
        public long totalDamageDealt = 0; // 总造成伤害
        public long totalDamageTaken = 0; // 总承受伤害

        // 构造函数
        public CharacterData()
        {
            InitializeRarityMultipliers();
            RecalculateStats();
        }

        public CharacterData(string id, string name, CharacterRarity rarity)
        {
            this.characterID = id;
            this.characterName = name;
            this.rarity = rarity;
            InitializeRarityMultipliers();
            RecalculateStats();
        }

        // 初始化稀有度加成
        private void InitializeRarityMultipliers()
        {
            switch (rarity)
            {
                case CharacterRarity.Common:
                    rarityHPMultiplier = 1f;
                    rarityAttackMultiplier = 1f;
                    rarityDefenseMultiplier = 1f;
                    break;
                case CharacterRarity.Rare:
                    rarityHPMultiplier = 1.2f;
                    rarityAttackMultiplier = 1.15f;
                    rarityDefenseMultiplier = 1.1f;
                    break;
                case CharacterRarity.Epic:
                    rarityHPMultiplier = 1.5f;
                    rarityAttackMultiplier = 1.3f;
                    rarityDefenseMultiplier = 1.2f;
                    break;
                case CharacterRarity.Legendary:
                    rarityHPMultiplier = 2f;
                    rarityAttackMultiplier = 1.5f;
                    rarityDefenseMultiplier = 1.4f;
                    break;
            }
        }

        // 重新计算所有属性
        public void RecalculateStats()
        {
            // 计算等级加成后的属性
            float levelBonusHP = hpGrowth * (level - 1);
            float levelBonusAttack = attackGrowth * (level - 1);
            float levelBonusDefense = defenseGrowth * (level - 1);

            // 应用稀有度加成
            maxHP = (100f + levelBonusHP) * rarityHPMultiplier;
            baseAttack = (20f + levelBonusAttack) * rarityAttackMultiplier;
            baseDefense = (10f + levelBonusDefense) * rarityDefenseMultiplier;

            // 确保当前HP不超过最大值
            if (currentHP > maxHP)
            {
                currentHP = maxHP;
            }

            // 计算升级所需经验 (指数增长)
            expToNextLevel = CalculateExpRequired(level + 1);
        }

        // 计算指定等级所需的经验值
        private long CalculateExpRequired(int targetLevel)
        {
            return (long)(100 * Mathf.Pow(targetLevel, 1.8f));
        }

        // 获取实际攻击力 (包含随机波动)
        public float GetAttackDamage()
        {
            float damage = baseAttack;

            // 添加10%的随机波动
            float randomFactor = Random.Range(0.9f, 1.1f);
            damage *= randomFactor;

            // 检查暴击
            if (Random.Range(0f, 1f) < criticalRate)
            {
                damage *= criticalDamage;
            }

            return damage;
        }

        // 承受伤害
        public float TakeDamage(float incomingDamage)
        {
            // 计算防御减伤 (防御力越高减伤越多，但不会完全免疫)
            float damageReduction = baseDefense / (baseDefense + 100f);
            float actualDamage = incomingDamage * (1f - damageReduction);

            currentHP = Mathf.Max(0, currentHP - actualDamage);
            totalDamageTaken += (long)actualDamage;

            return actualDamage;
        }

        // 恢复满血
        public void RestoreFullHP()
        {
            currentHP = maxHP;
        }

        // 获得经验值
        public bool GainExperience(long expAmount)
        {
            experience += expAmount;
            bool leveledUp = false;

            // 检查是否可以升级 (可能连续升级)
            while (experience >= expToNextLevel)
            {
                LevelUp();
                leveledUp = true;
            }

            return leveledUp;
        }

        // 升级
        private void LevelUp()
        {
            experience -= expToNextLevel;
            level++;

            // 重新计算属性
            RecalculateStats();

            // 升级后恢复满血
            RestoreFullHP();
        }

        // 获取战斗力评分
        public float GetPowerScore()
        {
            return (maxHP * 0.5f) + (baseAttack * 2f) + (baseDefense * 1f) +
                   (criticalRate * 100f) + (level * 10f);
        }

        // 获取胜率
        public float GetWinRate()
        {
            return totalBattles > 0 ? (float)victoriesCount / totalBattles : 0f;
        }

        // 是否已死亡
        public bool IsDead()
        {
            return currentHP <= 0;
        }
    }

    public enum CharacterRarity
    {
        Common = 1, // 普通 - 白色
        Rare = 2, // 稀有 - 蓝色  
        Epic = 3, // 史诗 - 紫色
        Legendary = 4 // 传说 - 金色
    }
}
