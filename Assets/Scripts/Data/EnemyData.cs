using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace IdleGame.Gameplay.Battle
{
    [Serializable]
    public class EnemyData
    {
        [Header("Basic Information")]
        public string enemyID; // 敌人唯一ID
        public string enemyName; // 敌人名称
        public EnemyType enemyType; // 敌人类型
        public int recommendedLevel; // 推荐等级

        [Header("Battle Attributes")]
        public float maxHP = 80f; // 最大生命值
        public float currentHP = 80f; // 当前生命值
        public float attackPower = 15f; // 攻击力
        public float defense = 8f; // 防御力
        public float attackSpeed = 1f; // 攻击速度
        public float criticalRate = 0.05f; // 暴击率

        [Header("Reward Settings")]
        public int baseExpReward = 50; // 基础经验奖励
        public int baseCoinReward = 30; // 基础金币奖励
        public float rewardMultiplier = 1f; // 奖励倍率

        [Header("Skill Settings (TODO)")]
        public bool hasSpecialAbility; // 是否有特殊技能
        public string specialAbilityName = ""; // 特殊技能名称
        public float specialAbilityChance = 0.2f; // 特殊技能触发概率

        [Header("Appearance (TODO)")]
        public Color enemyColor = Color.red; // 敌人颜色
        public float sizeScale = 1f; // 大小缩放

        // 构造函数
        public EnemyData()
        {
            currentHP = maxHP;
        }

        public EnemyData(string id, string name, int level)
        {
            enemyID = id;
            enemyName = name;
            recommendedLevel = level;
            GenerateStatsForLevel(level);
        }

        // 根据等级生成属性
        public void GenerateStatsForLevel(int level)
        {
            recommendedLevel = level;

            // 基础属性随等级线性增长
            maxHP = 80f + level * 15f;
            attackPower = 15f + level * 3f;
            defense = 8f + level * 1.5f;

            // 根据敌人类型调整属性
            ApplyEnemyTypeModifiers();

            // 添加一些随机性 (±10%)
            var randomFactor = Random.Range(0.9f, 1.1f);
            maxHP *= randomFactor;
            attackPower *= randomFactor;
            defense *= randomFactor;

            currentHP = maxHP;

            // 计算奖励
            baseExpReward = 50 + level * 10;
            baseCoinReward = 30 + level * 5;
        }

        // 应用敌人类型修正
        private void ApplyEnemyTypeModifiers()
        {
            switch (enemyType)
            {
                case EnemyType.Normal:
                    rewardMultiplier = 1f;
                    break;
                case EnemyType.Elite:
                    maxHP *= 1.5f;
                    attackPower *= 1.3f;
                    defense *= 1.2f;
                    rewardMultiplier = 1.5f;
                    hasSpecialAbility = true;
                    specialAbilityName = "Elite Buff";
                    break;
                case EnemyType.Boss:
                    maxHP *= 2.5f;
                    attackPower *= 1.8f;
                    defense *= 1.5f;
                    rewardMultiplier = 3f;
                    hasSpecialAbility = true;
                    specialAbilityName = "Boos Skill";
                    sizeScale = 1.5f;
                    break;
                case EnemyType.Special:
                    maxHP *= 0.8f;
                    attackPower *= 2f;
                    defense *= 0.5f;
                    rewardMultiplier = 2f;
                    hasSpecialAbility = true;
                    specialAbilityName = "Special Ability";
                    break;
            }
        }

        // 获取攻击伤害
        public float GetAttackDamage()
        {
            var damage = attackPower;

            // 添加随机波动
            var randomFactor = Random.Range(0.8f, 1.2f);
            damage *= randomFactor;

            // 检查暴击
            if (Random.Range(0f, 1f) < criticalRate) damage *= 1.5f; // 敌人暴击倍率固定1.5倍

            // 检查特殊技能
            if (hasSpecialAbility && Random.Range(0f, 1f) < specialAbilityChance) damage *= 1.8f; // 特殊技能额外伤害

            return damage;
        }

        // 承受伤害
        public float TakeDamage(float incomingDamage)
        {
            // 计算防御减伤
            var damageReduction = defense / (defense + 100f);
            var actualDamage = incomingDamage * (1f - damageReduction);

            currentHP = Mathf.Max(0, currentHP - actualDamage);

            return actualDamage;
        }

        // 获取实际经验奖励
        public int GetExpReward()
        {
            return Mathf.RoundToInt(baseExpReward * rewardMultiplier);
        }

        // 获取实际金币奖励
        public int GetCoinReward()
        {
            return Mathf.RoundToInt(baseCoinReward * rewardMultiplier);
        }

        // 是否已死亡
        public bool IsDead()
        {
            return currentHP <= 0;
        }

        // 重置血量
        public void ResetHP()
        {
            currentHP = maxHP;
        }

        // 获取敌人强度评分
        public float GetPowerScore()
        {
            return maxHP * 0.4f + attackPower * 2.5f + defense * 1.2f +
                   recommendedLevel * 8f;
        }
    }

    public enum EnemyType
    {
        Normal, // 普通敌人
        Elite, // 精英敌人
        Boss, // Boss敌人
        Special // 特殊敌人
    }
}