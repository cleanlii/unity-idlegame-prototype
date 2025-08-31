using System;
using System.Collections.Generic;
using IdleGame.Character;
using IdleGame.Gameplay;
using UnityEngine;

namespace IdleGame.Analytics
{
    /// <summary>
    ///     Main save data for player
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [Header("Basic Information")]
        public string playerID; // 玩家唯一ID
        public string playerName = "Player"; // 玩家昵称
        public DateTime createTime; // 创建时间
        public DateTime lastLoginTime; // 最后登录时间
        public DateTime lastSaveTime; // 最后保存时间

        [Header("Game Progress")]
        public int playerLevel = 1; // 玩家等级 (账号等级)
        public long totalPlayTime; // 总游戏时长 (秒)
        public int currentFloor = 1; // 当前爬塔层数
        public int maxFloorReached = 1; // 历史最高层数

        [Header("Currency Info")]
        public long coins = 1000; // 金币数量 (主货币，用于所有消费)
        public long totalCoinsEarned; // 总获得金币
        public long totalCoinsSpent; // 总花费金币

        [Header("Character Info")]
        public string currentCharacterID = ""; // 当前使用角色ID
        public List<CharacterSaveData> ownedCharacters = new(); // 拥有的角色存档数据

        [Header("Route Info")]
        public RouteType selectedRoute = RouteType.Battle; // 当前选择的路线
        public long battleRouteTime; // 战斗路线累计时间
        public long economyRouteTime; // 经济路线累计时间  
        public long experienceRouteTime; // 经验路线累计时间

        [Header("Battle Info")]
        public long totalBattles; // 总战斗次数
        public long totalVictories; // 总胜利次数
        public long totalDefeats; // 总失败次数
        public long totalDamageDealt; // 总造成伤害
        public long totalDamageTaken; // 总承受伤害
        public int longestWinStreak; // 最长连胜
        public int currentWinStreak; // 当前连胜

        [Header("Statistics")]
        public long totalExpPurchased; // 总购买经验值
        public long totalExpCostPaid; // 购买经验花费的金币
        public int totalGachaUsed; // 总抽卡次数
        public long totalGachaCostPaid; // 抽卡花费的金币

        [Header("Offline Data")]
        public long offlineDuration; // 离线时长 (秒)
        public long totalOfflineRewards; // 总离线奖励

        [Header("Game Settings (TODO)")]
        public float musicVolume = 1f; // 音乐音量
        public float sfxVolume = 1f; // 音效音量
        public bool enableNotifications = true; // 是否开启通知
        public string language = "English"; // 游戏语言
        public bool enableAutoSave = true; // 自动保存

        // 构造函数
        public PlayerData()
        {
            playerID = Guid.NewGuid().ToString();
            createTime = DateTime.Now;
            lastLoginTime = DateTime.Now;
            lastSaveTime = DateTime.Now;

            // 初始化数据
            InitializeDefaultData();
        }

        #region Initialization

        /// <summary>
        ///     初始化默认数据
        /// </summary>
        private void InitializeDefaultData()
        {
            // 如果没有角色，创建默认角色数据占位
            if (ownedCharacters.Count == 0)
            {
                var defaultCharacterData = new CharacterSaveData
                {
                    configID = "warrior_001", // 默认角色ID，需要在CharacterSystem中处理
                    level = 1,
                    totalExperience = 0,
                    totalBattles = 0,
                    victoriesCount = 0,
                    totalDamageDealt = 0,
                    totalDamageTaken = 0
                };
                ownedCharacters.Add(defaultCharacterData);
                currentCharacterID = defaultCharacterData.configID;
            }
        }

        #endregion

        #region Currency

        /// <summary>
        ///     添加金币
        /// </summary>
        public void AddCoins(long amount)
        {
            if (amount <= 0) return;

            coins += amount;
            totalCoinsEarned += amount;
        }

        /// <summary>
        ///     消费金币
        /// </summary>
        public bool SpendCoins(long amount)
        {
            if (amount <= 0) return false;

            if (coins >= amount)
            {
                coins -= amount;
                totalCoinsSpent += amount;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     检查金币是否充足
        /// </summary>
        public bool CanAfford(long amount)
        {
            return coins >= amount;
        }

        /// <summary>
        ///     购买经验值消费记录
        /// </summary>
        public void RecordExpPurchase(long expAmount, long coinCost)
        {
            totalExpPurchased += expAmount;
            totalExpCostPaid += coinCost;
        }

        /// <summary>
        ///     抽卡消费记录
        /// </summary>
        public void RecordGachaPurchase(long coinCost)
        {
            totalGachaUsed++;
            totalGachaCostPaid += coinCost;
        }

        /// <summary>
        ///     获取平均每次抽卡成本
        /// </summary>
        public float GetAverageGachaCost()
        {
            return totalGachaUsed > 0 ? (float)totalGachaCostPaid / totalGachaUsed : 0f;
        }

        /// <summary>
        ///     获取平均经验购买效率 (每金币换取经验)
        /// </summary>
        public float GetExpBuyingEfficiency()
        {
            return totalExpCostPaid > 0 ? (float)totalExpPurchased / totalExpCostPaid : 0f;
        }

        #endregion

        #region Character

        public bool HasCharacter(string characterID)
        {
            // 检查是否已经拥有该角色
            var existingChar = ownedCharacters.Find(c => c.configID == characterID);
            return existingChar != null;
        }

        /// <summary>
        ///     添加角色到收藏
        /// </summary>
        public void AddCharacter(CharacterSaveData characterSave)
        {
            if (characterSave == null || string.IsNullOrEmpty(characterSave.configID)) return;

            // 检查是否已经拥有该角色
            var existingChar = ownedCharacters.Find(c => c.configID == characterSave.configID);
            if (existingChar == null) ownedCharacters.Add(characterSave);
        }

        /// <summary>
        ///     移除角色
        /// </summary>
        public bool RemoveCharacter(string characterID)
        {
            var character = ownedCharacters.Find(c => c.configID == characterID);
            if (character != null)
            {
                ownedCharacters.Remove(character);

                // 如果删除的是当前角色，切换到第一个角色
                if (currentCharacterID == characterID && ownedCharacters.Count > 0) currentCharacterID = ownedCharacters[0].configID;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     切换当前角色
        /// </summary>
        public bool SwitchCharacter(string characterID)
        {
            var character = ownedCharacters.Find(c => c.configID == characterID);
            if (character != null)
            {
                currentCharacterID = characterID;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     获取当前角色数据
        /// </summary>
        public CharacterSaveData GetCurrentCharacter()
        {
            return ownedCharacters.Find(c => c.configID == currentCharacterID);
        }

        /// <summary>
        ///     更新角色数据
        /// </summary>
        public void UpdateCharacter(CharacterSaveData updatedData)
        {
            var index = ownedCharacters.FindIndex(c => c.configID == updatedData.configID);
            if (index >= 0) ownedCharacters[index] = updatedData;
        }

        /// <summary>
        ///     获取拥有角色数量
        /// </summary>
        public int GetOwnedCharacterCount()
        {
            return ownedCharacters.Count;
        }

        /// <summary>
        ///     按稀有度获取角色数量
        /// </summary>
        public Dictionary<CharacterRarity, int> GetCharacterCountByRarity()
        {
            // 这个方法需要角色配置信息，实际实现时需要通过CharacterDatabase查询
            // 暂时返回空字典
            return new Dictionary<CharacterRarity, int>();
        }

        #endregion

        #region Battle

        /// <summary>
        ///     记录战斗结果
        /// </summary>
        public void RecordBattleResult(bool victory, long damageDealt, long damageTaken)
        {
            totalBattles++;
            totalDamageDealt += damageDealt;
            totalDamageTaken += damageTaken;

            if (victory)
            {
                totalVictories++;
                currentWinStreak++;
                if (currentWinStreak > longestWinStreak)
                    longestWinStreak = currentWinStreak;
            }
            else
            {
                totalDefeats++;
                currentWinStreak = 0;
            }
        }

        /// <summary>
        ///     获取胜率
        /// </summary>
        public float GetWinRate()
        {
            return totalBattles > 0 ? (float)totalVictories / totalBattles : 0f;
        }

        /// <summary>
        ///     获取平均每场战斗伤害
        /// </summary>
        public float GetAverageDamagePerBattle()
        {
            return totalBattles > 0 ? (float)totalDamageDealt / totalBattles : 0f;
        }

        #endregion

        #region Timer

        /// <summary>
        ///     更新在线时长
        /// </summary>
        public void UpdatePlayTime()
        {
            var currentTime = DateTime.Now;
            var timeDiff = currentTime - lastLoginTime;
            totalPlayTime += (long)timeDiff.TotalSeconds;
            lastLoginTime = currentTime;
        }

        /// <summary>
        ///     计算离线时长
        /// </summary>
        public long CalculateOfflineTime()
        {
            var currentTime = DateTime.Now;
            var timeDiff = currentTime - lastSaveTime;
            offlineDuration = (long)timeDiff.TotalSeconds;
            return offlineDuration;
        }

        /// <summary>
        ///     更新保存时间
        /// </summary>
        public void UpdateSaveTime()
        {
            lastSaveTime = DateTime.Now;
        }

        /// <summary>
        ///     获取总游戏时长（格式化）
        /// </summary>
        public string GetFormattedPlayTime()
        {
            var hours = totalPlayTime / 3600;
            var minutes = totalPlayTime % 3600 / 60;
            return $"{hours}小时{minutes}分钟";
        }

        /// <summary>
        ///     更新路线时间
        /// </summary>
        public void UpdateRouteTime(RouteType route, long seconds)
        {
            switch (route)
            {
                case RouteType.Battle:
                    battleRouteTime += seconds;
                    break;
                case RouteType.Economy:
                    economyRouteTime += seconds;
                    break;
                case RouteType.Experience:
                    experienceRouteTime += seconds;
                    break;
            }
        }

        #endregion

        #region Analytics

        /// <summary>
        ///     获取玩家综合战力评分
        /// </summary>
        public float GetTotalPowerScore()
        {
            var playerLevelBonus = playerLevel * 20f;
            var floorBonus = maxFloorReached * 10f;
            var experienceBonus = totalExpPurchased * 0.1f;
            var wealthBonus = coins * 0.001f;

            return playerLevelBonus + floorBonus + experienceBonus + wealthBonus;
        }

        /// <summary>
        ///     获取经济实力评级
        /// </summary>
        public string GetWealthRating()
        {
            if (coins >= 100000) return "富豪";
            if (coins >= 50000) return "富裕";
            if (coins >= 20000) return "小康";
            if (coins >= 5000) return "普通";
            return "贫穷";
        }

        /// <summary>
        ///     获取战斗经验评级
        /// </summary>
        public string GetBattleExperienceRating()
        {
            if (totalBattles >= 1000) return "传奇战士";
            if (totalBattles >= 500) return "资深战士";
            if (totalBattles >= 200) return "经验丰富";
            if (totalBattles >= 50) return "初有经验";
            return "新手";
        }

        /// <summary>
        ///     获取游戏统计摘要
        /// </summary>
        public string GetGameStatsSummary()
        {
            return $"等级：{playerLevel}\n" +
                   $"金币：{coins:N0}\n" +
                   $"总战斗：{totalBattles}\n" +
                   $"胜率：{GetWinRate():P1}\n" +
                   $"最高层数：{maxFloorReached}\n" +
                   $"游戏时长：{GetFormattedPlayTime()}\n" +
                   $"拥有角色：{GetOwnedCharacterCount()}";
        }

        #endregion

        #region Validation

        /// <summary>
        ///     验证数据完整性
        /// </summary>
        public bool ValidateData()
        {
            // 基础验证
            if (string.IsNullOrEmpty(playerID)) return false;
            if (playerLevel < 1) return false;
            if (coins < 0) return false;
            if (currentFloor < 1) return false;
            if (maxFloorReached < currentFloor) return false;

            // 统计数据一致性
            if (totalBattles < totalVictories + totalDefeats) return false;
            if (totalCoinsEarned < totalCoinsSpent) return false;

            // 角色数据验证
            if (ownedCharacters.Count == 0) return false;
            if (!string.IsNullOrEmpty(currentCharacterID))
            {
                if (ownedCharacters.Find(c => c.configID == currentCharacterID) == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     修复数据异常
        /// </summary>
        public void FixDataAnomalies()
        {
            // 修复负数
            if (coins < 0) coins = 0;
            if (playerLevel < 1) playerLevel = 1;
            if (currentFloor < 1) currentFloor = 1;
            if (maxFloorReached < currentFloor) maxFloorReached = currentFloor;

            // 修复统计异常
            if (totalBattles < totalVictories + totalDefeats)
                totalBattles = totalVictories + totalDefeats;

            // 修复角色数据
            if (ownedCharacters.Count == 0)
                InitializeDefaultData();

            if (!string.IsNullOrEmpty(currentCharacterID))
            {
                if (ownedCharacters.Find(c => c.configID == currentCharacterID) == null)
                    currentCharacterID = ownedCharacters.Count > 0 ? ownedCharacters[0].configID : "";
            }
        }

        #endregion
    }

    /// <summary>
    ///     Save data for specific character
    /// </summary>
    [Serializable]
    public class CharacterSaveData
    {
        public string configID; // 角色配置ID
        public int level; // 角色等级
        public long totalExperience; // 总经验值
        public int totalBattles; // 总战斗次数
        public int victoriesCount; // 胜利次数
        public long totalDamageDealt; // 总造成伤害
        public long totalDamageTaken; // 总承受伤害
        public DateTime lastUsedTime; // 最后使用时间

        public CharacterSaveData()
        {
            lastUsedTime = DateTime.Now;
        }

        public CharacterSaveData(string configID)
        {
            this.configID = configID;
            level = 1;
            totalExperience = 0;
            lastUsedTime = DateTime.Now;
        }
    }
}