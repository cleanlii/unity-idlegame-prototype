using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame.Gameplay
{
    /// <summary>
    ///     离线奖励配置
    /// </summary>
    [CreateAssetMenu(fileName = "OfflineRewardConfig", menuName = "IdleGame/Offline Reward Config")]
    public class OfflineRewardConfig : ScriptableObject
    {
        [Header("战斗线每小时奖励")]
        public float battleExpPerHour = 120f;
        public float battleCoinPerHour = 50f;

        [Header("经济线每小时奖励")]
        public float economyCoinPerHour = 200f;

        [Header("经验线每小时奖励")]
        public float experienceExpPerHour = 300f;

        [Header("奖励上限")]
        public float maxDailyExp = 10000f;
        public float maxDailyCoin = 5000f;
    }

    /// <summary>
    ///     离线时间数据
    /// </summary>
    [Serializable]
    public class OfflineTimeData
    {
        public DateTime startTime;
        public DateTime endTime;
        public double totalSeconds;
        public double totalMinutes;
        public double totalHours;
        public bool isValid;
        public string reason = "";
    }

    /// <summary>
    ///     离线奖励数据
    /// </summary>
    [Serializable]
    public class OfflineRewardData
    {
        public float offlineHours;
        public RouteType route;
        public long expReward;
        public long coinReward;
        public int simulatedBattles;
        public bool hasBonus;
        public float bonusMultiplier = 1f;
        public string description = "";

        public long totalValue => expReward + coinReward;

        public string GetSummary()
        {
            var parts = new List<string>();
            if (expReward > 0) parts.Add($"经验 {expReward:N0}");
            if (coinReward > 0) parts.Add($"金币 {coinReward:N0}");

            var bonusText = hasBonus ? $" (x{bonusMultiplier:F1})" : "";
            return $"{string.Join(", ", parts)}{bonusText}";
        }
    }
}