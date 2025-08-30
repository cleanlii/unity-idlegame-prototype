using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame.Analytics
{
    public class IdleLogSystem : MonoBehaviour
{
    [Header("Log Settings")]
    public int maxLogEntries = 100;
    public bool showTimestamps = true;
    
    private Queue<string> logMessages = new Queue<string>();
    
    public event System.Action<string> OnNewLogMessage;
    
    private void Start()
    {
        // 订阅游戏事件
        // GameEvents.OnPlayerLevelUp += (level) => LogMessage($"角色升级到 {level} 级!");
        // GameEvents.OnCoinGained += (amount) => LogMessage($"获得 {amount} 金币");
        // GameEvents.OnExpGained += (amount) => LogMessage($"获得 {amount} 经验值");
        // GameEvents.OnBattleResult += (result) => LogMessage($"战斗结果: {result}");
        // GameEvents.OnRouteChanged += (route) => LogMessage($"切换路线: {route}");
    }
    
    public void LogMessage(string message)
    {
        string timestampedMessage = showTimestamps 
            ? $"[{System.DateTime.Now:HH:mm:ss}] {message}"
            : message;
        
        logMessages.Enqueue(timestampedMessage);
        
        // 保持日志数量在限制内
        while (logMessages.Count > maxLogEntries)
        {
            logMessages.Dequeue();
        }
        
        // 通知UI更新
        OnNewLogMessage?.Invoke(timestampedMessage);
        
        // 输出到Unity控制台 (开发时使用)
        Debug.Log($"[Game Log] {message}");
    }
    
    public void LogDamage(float damage, bool isPlayerDamage)
    {
        string damageType = isPlayerDamage ? "造成伤害" : "受到伤害";
        LogMessage($"{damageType}: {damage:F1}");
    }
    
    public void LogOfflineReward(string rewardType, int amount, float offlineHours)
    {
        LogMessage($"离线 {offlineHours:F1} 小时，{rewardType}奖励: +{amount}");
    }
    
    public string[] GetRecentLogs(int count = 20)
    {
        var logs = logMessages.ToArray();
        int startIndex = Mathf.Max(0, logs.Length - count);
        int actualCount = Mathf.Min(count, logs.Length);
        
        string[] result = new string[actualCount];
        System.Array.Copy(logs, startIndex, result, 0, actualCount);
        
        return result;
    }
    
    public void ClearLogs()
    {
        logMessages.Clear();
        LogMessage("日志已清空");
    }
}
}
