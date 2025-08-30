using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace IdleGame.Character
{
    /// <summary>
    ///     角色数据类 - 存储角色的运行时状态和升级信息
    ///     基于CharacterConfig配置，包含当前状态（血量、经验等）
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        [Header("配置引用")]
        public CharacterConfig config; // 角色配置引用

        [Header("持久化数据")]
        public int level = 1; // 当前等级 (持久化)
        public long totalExperience; // 总经验值 (持久化)

        [Header("运行时状态 - 仅当前角色有效")]
        public float currentHP; // 当前生命值 (运行时，切换角色时重置)
        public long currentLevelExp; // 当前等级经验 (运行时)

        [Header("战斗统计 - 持久化")]
        public int totalBattles;
        public int victoriesCount;
        public long totalDamageDealt;
        public long totalDamageTaken;

        public bool IsNull => config == null;

        // 缓存的计算属性 (避免重复计算)
        private float _cachedMaxHP = -1f;
        private float _cachedAttack = -1f;
        private float _cachedDefense = -1f;
        private float _cachedCritRate = -1f;
        private int _lastCalculatedLevel = -1;

        /// <summary>
        ///     基于配置创建角色数据
        /// </summary>
        public CharacterData(CharacterConfig characterConfig)
        {
            config = characterConfig;
            level = 1;
            totalExperience = 0;
            RefreshRuntimeState();
        }

        /// <summary>
        ///     序列化构造函数（用于存档加载）
        /// </summary>
        public CharacterData(CharacterConfig characterConfig, int savedLevel, long savedExp)
        {
            config = characterConfig;
            level = savedLevel;
            totalExperience = savedExp;
            RefreshRuntimeState();
        }

        #region 属性获取 (带缓存优化)

        /// <summary>
        ///     获取最大HP
        /// </summary>
        public float GetMaxHP()
        {
            if (_lastCalculatedLevel != level) RefreshCache();
            return _cachedMaxHP;
        }

        /// <summary>
        ///     获取攻击力
        /// </summary>
        public float GetAttack()
        {
            if (_lastCalculatedLevel != level) RefreshCache();
            return _cachedAttack;
        }

        /// <summary>
        ///     获取防御力
        /// </summary>
        public float GetDefense()
        {
            if (_lastCalculatedLevel != level) RefreshCache();
            return _cachedDefense;
        }

        /// <summary>
        ///     获取暴击率
        /// </summary>
        public float GetCriticalRate()
        {
            if (_lastCalculatedLevel != level) RefreshCache();
            return _cachedCritRate;
        }

        /// <summary>
        ///     获取暴击伤害倍率
        /// </summary>
        public float GetCriticalDamage()
        {
            return config.baseCriticalDamage;
        }

        /// <summary>
        ///     获取攻击速度
        /// </summary>
        public float GetAttackSpeed()
        {
            return config.baseAttackSpeed;
        }

        #endregion

        #region 经验和升级系统

        /// <summary>
        ///     获取当前等级升级所需经验
        /// </summary>
        public long GetExpToNextLevel()
        {
            return config.CalculateExpRequired(level + 1);
        }

        /// <summary>
        ///     获取当前等级经验进度 (0-1)
        /// </summary>
        public float GetExpProgress()
        {
            var expForCurrentLevel = config.CalculateExpRequired(level);
            var expForNextLevel = config.CalculateExpRequired(level + 1);
            var expInThisLevel = totalExperience - expForCurrentLevel;
            var expNeededForLevel = expForNextLevel - expForCurrentLevel;

            return expNeededForLevel > 0 ? (float)expInThisLevel / expNeededForLevel : 1f;
        }

        /// <summary>
        /// 获得经验值
        /// </summary>
        public bool GainExperience(long expAmount)
        {
            if (expAmount <= 0) return false;

            totalExperience += expAmount;
            bool leveledUp = CheckLevelUp();
            
            // 重新计算当前等级经验
            RefreshCurrentLevelExp();
            
            return leveledUp;
        }

        /// <summary>
        ///     检查并处理升级
        /// </summary>
        private bool CheckLevelUp()
        {
            var leveledUp = false;
            var expRequired = config.CalculateExpRequired(level + 1);

            while (totalExperience >= expRequired && expRequired > 0)
            {
                level++;
                leveledUp = true;
                expRequired = config.CalculateExpRequired(level + 1);
            }

            if (leveledUp)
            {
                RefreshCache();
                // 升级时恢复满血 (如果是当前使用角色)
                if (IsCurrentCharacter()) currentHP = GetMaxHP();
            }

            return leveledUp;
        }
        
        /// <summary>
        /// 刷新当前等级经验
        /// </summary>
        private void RefreshCurrentLevelExp()
        {
            long expForCurrentLevel = config.CalculateExpRequired(level);
            currentLevelExp = totalExperience - expForCurrentLevel;
            
            // 确保当前等级经验不为负数
            if (currentLevelExp < 0) currentLevelExp = 0;
        }

        #endregion

        #region 战斗系统

        /// <summary>
        ///     获取攻击伤害 (包含随机波动和暴击)
        /// </summary>
        public float GetAttackDamage()
        {
            var damage = GetAttack();

            // 添加10%的随机波动
            var randomFactor = Random.Range(0.9f, 1.1f);
            damage *= randomFactor;

            // 检查暴击
            if (Random.Range(0f, 1f) < GetCriticalRate()) damage *= GetCriticalDamage();

            // 检查特殊技能 (如果有的话)
            if (config.hasSpecialAbility && Random.Range(0f, 1f) < 0.1f) // 10%触发率
                damage *= config.specialAbilityMultiplier;

            return damage;
        }

        /// <summary>
        ///     承受伤害
        /// </summary>
        public float TakeDamage(float incomingDamage)
        {
            // 只有当前角色才能承受伤害
            if (!IsCurrentCharacter()) return 0f;

            // 计算防御减伤
            var damageReduction = GetDefense() / (GetDefense() + 100f);
            var actualDamage = incomingDamage * (1f - damageReduction);

            currentHP = Mathf.Max(0, currentHP - actualDamage);
            totalDamageTaken += (long)actualDamage;

            return actualDamage;
        }

        /// <summary>
        ///     是否死亡
        /// </summary>
        public bool IsDead()
        {
            return IsCurrentCharacter() && currentHP <= 0;
        }

        /// <summary>
        ///     恢复满血
        /// </summary>
        public void RestoreFullHP()
        {
            if (IsCurrentCharacter()) currentHP = GetMaxHP();
        }

        #endregion

        #region 状态管理

        /// <summary>
        ///     刷新运行时状态 (切换角色时调用)
        /// </summary>
        public void RefreshRuntimeState()
        {
            RefreshCache();
            currentHP = GetMaxHP(); // 重置为满血

            // 计算当前等级经验
            var expForCurrentLevel = config.CalculateExpRequired(level);
            currentLevelExp = totalExperience - expForCurrentLevel;
        }

        /// <summary>
        ///     刷新属性缓存
        /// </summary>
        private void RefreshCache()
        {
            _cachedMaxHP = config.CalculateMaxHP(level);
            _cachedAttack = config.CalculateAttack(level);
            _cachedDefense = config.CalculateDefense(level);
            _cachedCritRate = config.CalculateCriticalRate(level);
            _lastCalculatedLevel = level;
        }

        /// <summary>
        ///     检查是否为当前使用的角色
        /// </summary>
        private bool IsCurrentCharacter()
        {
            // TODO: 通过CharacterSystem检查是否为当前角色
            return GameManager.Instance?.characterSystem.currentCharacter == this;
        }

        #endregion

        #region 统计和信息

        /// <summary>
        ///     记录战斗结果
        /// </summary>
        public void RecordBattleResult(bool victory, long damageDealt)
        {
            totalBattles++;
            totalDamageDealt += damageDealt;

            if (victory) victoriesCount++;
        }

        /// <summary>
        ///     获取胜率
        /// </summary>
        public float GetWinRate()
        {
            return totalBattles > 0 ? (float)victoriesCount / totalBattles : 0f;
        }

        /// <summary>
        ///     获取战力评分
        /// </summary>
        public float GetPowerScore()
        {
            return config.CalculatePowerScore(level);
        }

        /// <summary>
        ///     获取角色基本信息
        /// </summary>
        public string GetCharacterInfo()
        {
            return $"{config.characterName} Lv.{level} (Power: {GetPowerScore():F0})";
        }

        #endregion

        #region 序列化支持

        /// <summary>
        ///     获取存档数据
        /// </summary>
        public CharacterSaveData GetSaveData()
        {
            return new CharacterSaveData
            {
                configID = config.characterID,
                level = level,
                totalExperience = totalExperience,
                totalBattles = totalBattles,
                victoriesCount = victoriesCount,
                totalDamageDealt = totalDamageDealt,
                totalDamageTaken = totalDamageTaken
            };
        }

        #endregion
    }


    /// <summary>
    ///     角色存档数据
    /// </summary>
    [Serializable]
    public class CharacterSaveData
    {
        public string configID;
        public int level;
        public long totalExperience;
        public int totalBattles;
        public int victoriesCount;
        public long totalDamageDealt;
        public long totalDamageTaken;
    }

    public enum CharacterRarity
    {
        Common = 1, // 普通 - 白色
        Rare = 2, // 稀有 - 蓝色  
        Epic = 3, // 史诗 - 紫色
        Legendary = 4 // 传说 - 金色
    }
}