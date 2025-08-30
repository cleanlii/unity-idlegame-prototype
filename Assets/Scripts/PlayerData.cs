using System;
using System.Collections.Generic;
using IdleGame.Character;
using IdleGame.Gameplay;
using UnityEngine;

namespace IdleGame.Analytics
{
    [Serializable]
    public class PlayerData
    {
        [Header("基本信息")]
        public string playerID; // 玩家唯一ID
        public string playerName = "Player"; // 玩家昵称
        public DateTime createTime; // 创建时间
        public DateTime lastLoginTime; // 最后登录时间
        public DateTime lastSaveTime; // 最后保存时间

        [Header("游戏进度")]
        public int playerLevel = 1; // 玩家等级 (账号等级)
        public long totalPlayTime; // 总游戏时长 (秒)
        public int currentFloor = 1; // 当前爬塔层数
        public int maxFloorReached = 1; // 历史最高层数

        [Header("当前角色")]
        public CharacterData currentCharacter; // 当前使用的角色

        [Header("拥有的角色")]
        public List<CharacterData> ownedCharacters = new();

        [Header("游戏资源")]
        public long coins = 1000; // 金币数量
        public int gems; // 钻石数量 (高级货币)
        public int gachaTickets; // 抽卡券数量

        [Header("路线系统")]
        public RouteType selectedRoute = RouteType.Battle; // 当前选择的路线
        public long battleRouteTime; // 战斗路线累计时间
        public long economyRouteTime; // 经济路线累计时间  
        public long experienceRouteTime; // 经验路线累计时间

        [Header("战斗统计")]
        public long totalBattles; // 总战斗次数
        public long totalVictories; // 总胜利次数
        public long totalDefeats; // 总失败次数
        public long totalDamageDealt; // 总造成伤害
        public long totalDamageTaken; // 总承受伤害
        public int longestWinStreak; // 最长连胜
        public int currentWinStreak; // 当前连胜

        [Header("经济统计")]
        public long totalCoinsEarned; // 总获得金币
        public long totalCoinsSpent; // 总花费金币
        public long totalExpGained; // 总获得经验
        public int totalGachaUsed; // 总抽卡次数

        [Header("离线数据")]
        public long offlineDuration; // 离线时长 (秒)
        public long totalOfflineRewards; // 总离线奖励

        [Header("成就数据")]
        public List<string> unlockedAchievements = new(); // 已解锁成就
        public Dictionary<string, int> achievementProgress = new(); // 成就进度

        [Header("设置数据")]
        public float musicVolume = 1f; // 音乐音量
        public float sfxVolume = 1f; // 音效音量
        public bool enableNotifications = true; // 是否开启通知
        public string language = "Chinese"; // 游戏语言

        // 构造函数
        public PlayerData()
        {
            playerID = Guid.NewGuid().ToString();
            createTime = DateTime.Now;
            lastLoginTime = DateTime.Now;
            lastSaveTime = DateTime.Now;

            // 创建默认角色
            CreateDefaultCharacter();
        }

        // 创建默认角色
        private void CreateDefaultCharacter()
        {
            var defaultChar = new CharacterData("char_001", "新手剑士", CharacterRarity.Common);
            ownedCharacters.Add(defaultChar);
            currentCharacter = defaultChar;
        }

        // 添加角色到收藏
        public void AddCharacter(CharacterData character)
        {
            if (!ownedCharacters.Exists(c => c.characterID == character.characterID)) ownedCharacters.Add(character);
        }

        // 切换当前角色
        public bool SwitchCharacter(string characterID)
        {
            var character = ownedCharacters.Find(c => c.characterID == characterID);
            if (character != null)
            {
                currentCharacter = character;
                return true;
            }

            return false;
        }

        // 添加金币
        public void AddCoins(long amount)
        {
            coins += amount;
            totalCoinsEarned += amount;
        }

        // 消费金币
        public bool SpendCoins(long amount)
        {
            if (coins >= amount)
            {
                coins -= amount;
                totalCoinsSpent += amount;
                return true;
            }

            return false;
        }

        // 记录战斗结果
        public void RecordBattleResult(bool victory, long damageDealt, long damageTaken)
        {
            totalBattles++;
            totalDamageDealt += damageDealt;
            totalDamageTaken += damageTaken;

            if (victory)
            {
                totalVictories++;
                currentWinStreak++;
                if (currentWinStreak > longestWinStreak) longestWinStreak = currentWinStreak;
            }
            else
            {
                totalDefeats++;
                currentWinStreak = 0;
            }
        }

        // 获取胜率
        public float GetWinRate()
        {
            return totalBattles > 0 ? (float)totalVictories / totalBattles : 0f;
        }

        // 获取玩家战力
        public float GetTotalPowerScore()
        {
            var characterPower = currentCharacter?.GetPowerScore() ?? 0f;
            var playerLevelBonus = playerLevel * 20f;
            var floorBonus = maxFloorReached * 10f;

            return characterPower + playerLevelBonus + floorBonus;
        }

        // 更新在线时长
        public void UpdatePlayTime()
        {
            var currentTime = DateTime.Now;
            var timeDiff = currentTime - lastLoginTime;
            totalPlayTime += (long)timeDiff.TotalSeconds;
            lastLoginTime = currentTime;
        }

        // 计算离线时长
        public long CalculateOfflineTime()
        {
            var currentTime = DateTime.Now;
            var timeDiff = currentTime - lastSaveTime;
            offlineDuration = (long)timeDiff.TotalSeconds;
            return offlineDuration;
        }

        // 更新保存时间
        public void UpdateSaveTime()
        {
            lastSaveTime = DateTime.Now;
        }

        // 解锁成就
        public bool UnlockAchievement(string achievementID)
        {
            if (!unlockedAchievements.Contains(achievementID))
            {
                unlockedAchievements.Add(achievementID);
                return true; // 新解锁
            }

            return false; // 已经解锁过
        }

        // 更新成就进度
        public void UpdateAchievementProgress(string achievementID, int progress)
        {
            achievementProgress[achievementID] = progress;
        }

        // 获取成就进度
        public int GetAchievementProgress(string achievementID)
        {
            return achievementProgress.ContainsKey(achievementID) ? achievementProgress[achievementID] : 0;
        }
    }
}