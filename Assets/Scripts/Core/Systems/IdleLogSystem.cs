using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleGame.Analytics
{
    public class IdleLogSystem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject logPanel; // 日志面板
        [SerializeField] private Transform logContentParent; // 日志内容父物体
        [SerializeField] private GameObject logEntryPrefab; // 日志条目预制体
        [SerializeField] private ScrollRect scrollRect; // 滚动区域
        [SerializeField] private Button toggleButton; // 显示/隐藏按钮
        [SerializeField] private Button clearButton; // 清空日志按钮
        [SerializeField] private TextMeshProUGUI logCountText; // 日志数量显示

        [Header("Log Entry Settings")]
        [SerializeField] private int maxLogEntries = 50; // 最大日志条目数
        [SerializeField] private bool showTimestamps = true; // 显示时间戳
        [SerializeField] private bool autoScrollToBottom = true; // 自动滚动到底部
        [SerializeField] private float logFadeInDuration = 0.3f; // 日志淡入时间
        [SerializeField] private float logLifetime = 30f; // 日志显示时长（秒）

        [Header("Highlight Settings")]
        [SerializeField] private Color battleColor = Color.red; // 战斗相关
        [SerializeField] private Color expColor = Color.blue; // 经验相关
        [SerializeField] private Color coinColor = Color.yellow; // 金币相关
        [SerializeField] private Color systemColor = Color.green; // 系统消息
        [SerializeField] private Color warningColor = Color.yellow; // 警告消息
        [SerializeField] private Color characterColor = Color.cyan; // 角色相关

        [Header("Animation Settings")]
        [SerializeField] private bool enableEntryAnimations = true;
        [SerializeField] private float entrySlideDistance = 50f;
        [SerializeField] private Ease entryEase = Ease.OutBack;

        private readonly Queue<LogEntry> _logEntries = new();
        private readonly List<GameObject> _logUIElements = new();
        private bool _isPanelVisible = true;

        public event Action<LogEntry> OnNewLogEntry;
        public event Action OnLogCleared;

        private void Awake()
        {
            InitializeLogSystem();
        }

        private void Start()
        {
            SetupUI();
            LogMessage("游戏日志系统已启动", LogType.System);
        }

        #region Initialization

        private void InitializeLogSystem()
        {
            // 确保UI组件存在
            if (logPanel == null)
            {
                Debug.LogWarning("[IdleLogSystem] 日志面板未设置，创建默认面板");
                CreateDefaultLogPanel();
            }

            // 初始化按钮事件
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleLogPanel);

            if (clearButton != null)
                clearButton.onClick.AddListener(ClearLogs);
        }

        private void SetupUI()
        {
            // 更新日志计数显示
            UpdateLogCountDisplay();

            // 设置初始面板状态
            if (logPanel != null)
                logPanel.SetActive(_isPanelVisible);
        }

        private void CreateDefaultLogPanel()
        {
            // 这里可以添加代码来创建默认的日志UI
            // 实际项目中建议在Scene中预设好UI组件
            Debug.LogWarning("[IdleLogSystem] 请在Scene中设置日志UI组件");
        }

        #endregion

        #region Core Methods

        /// <summary>
        ///     记录通用日志消息
        /// </summary>
        public void LogMessage(string message, LogType logType = LogType.General)
        {
            var entry = new LogEntry
            {
                message = message,
                logType = logType,
                timestamp = DateTime.Now,
                displayColor = GetLogTypeColor(logType)
            };

            AddLogEntry(entry);
        }

        /// <summary>
        ///     记录战斗伤害日志
        /// </summary>
        public void LogDamage(float damage, bool isPlayerDamage, string playerName = "", string enemyName = "")
        {
            string message;
            LogType logType;

            if (isPlayerDamage)
            {
                message = $"【{playerName}】对【{enemyName}】造成了 {damage:F0} 点伤害";
                logType = LogType.Battle;
            }
            else
            {
                message = $"【{enemyName}】对【{playerName}】造成了 {damage:F0} 点伤害";
                logType = LogType.Battle;
            }

            LogMessage(message, logType);
        }

        /// <summary>
        ///     记录战斗结果日志
        /// </summary>
        public void LogBattleResult(bool victory, string playerName, string enemyName, float duration, int expReward = 0, int coinReward = 0)
        {
            var resultText = victory ? "击败" : "被击败";
            var message = $"【{playerName}】{resultText}了【{enemyName}】！战斗用时 {duration:F1} 秒";

            LogMessage(message, LogType.Battle);

            if (victory && (expReward > 0 || coinReward > 0))
            {
                if (expReward > 0)
                    LogMessage($"获得经验值：+{expReward}", LogType.Experience);
                if (coinReward > 0)
                    LogMessage($"获得金币：+{coinReward}", LogType.Coin);
            }
        }

        /// <summary>
        ///     记录经验获得日志
        /// </summary>
        public void LogExpGain(long expAmount, string source = "", string characterName = "")
        {
            var sourceText = string.IsNullOrEmpty(source) ? "" : $"（{source}）";
            var message = $"【{characterName}】获得了 {expAmount} 点经验值{sourceText}";
            LogMessage(message, LogType.Experience);
        }

        /// <summary>
        ///     记录角色升级日志
        /// </summary>
        public void LogLevelUp(string characterName, int oldLevel, int newLevel)
        {
            var message = $"🎉【{characterName}】升级了！Lv.{oldLevel} → Lv.{newLevel}";
            LogMessage(message, LogType.Character);
        }

        /// <summary>
        ///     记录金币变化日志
        /// </summary>
        public void LogCoinChange(long amount, string reason = "", bool isGain = true)
        {
            var action = isGain ? "获得" : "消费";
            var reasonText = string.IsNullOrEmpty(reason) ? "" : $"（{reason}）";
            var message = $"{action}金币：{(isGain ? "+" : "-")}{Math.Abs(amount)}{reasonText}";
            LogMessage(message, LogType.Coin);
        }

        /// <summary>
        ///     记录角色切换日志
        /// </summary>
        public void LogCharacterSwitch(string oldCharacter, string newCharacter)
        {
            var message = $"切换角色：【{oldCharacter}】→【{newCharacter}】";
            LogMessage(message, LogType.Character);
        }

        /// <summary>
        ///     记录抽卡结果日志
        /// </summary>
        public void LogGachaResult(string characterName, string rarity, int cost)
        {
            var message = $"🎲 抽卡成功！获得【{characterName}】（{rarity}）花费 {cost} 金币";
            LogMessage(message, LogType.Character);
        }

        /// <summary>
        ///     记录路线切换日志
        /// </summary>
        public void LogRouteSwitch(string oldRoute, string newRoute)
        {
            var message = $"切换路线：{oldRoute} → {newRoute}";
            LogMessage(message, LogType.System);
        }

        /// <summary>
        ///     记录离线奖励日志
        /// </summary>
        public void LogOfflineReward(string rewardType, int amount, float offlineHours)
        {
            var message = $"⏰ 离线 {offlineHours:F1} 小时，{rewardType}奖励：+{amount}";
            LogMessage(message, LogType.System);
        }

        /// <summary>
        ///     记录商店购买日志
        /// </summary>
        public void LogPurchase(string itemName, int cost, int amount)
        {
            var message = $"🛒 购买【{itemName}】×{amount}，花费 {cost} 金币";
            LogMessage(message, LogType.Coin);
        }

        #endregion

        #region UI Display

        /// <summary>
        ///     添加日志条目到UI
        /// </summary>
        private void AddLogEntry(LogEntry entry)
        {
            // 添加到数据队列
            _logEntries.Enqueue(entry);

            // 移除超出限制的旧日志
            while (_logEntries.Count > maxLogEntries)
            {
                var oldEntry = _logEntries.Dequeue();
                RemoveOldestLogUI();
            }

            // 创建UI元素
            CreateLogUI(entry);

            // 触发事件
            OnNewLogEntry?.Invoke(entry);

            // 输出到Unity控制台（调试用）
            Debug.Log($"[Game Log] {entry.GetFormattedMessage()}");

            // 更新UI显示
            UpdateLogCountDisplay();
        }

        /// <summary>
        ///     创建日志UI元素
        /// </summary>
        private void CreateLogUI(LogEntry entry)
        {
            if (logContentParent == null || logEntryPrefab == null) return;

            // 实例化日志条目
            var logObject = Instantiate(logEntryPrefab, logContentParent);
            _logUIElements.Add(logObject);

            // 设置日志内容
            var textComponent = logObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = entry.GetFormattedMessage();
                textComponent.color = entry.displayColor;
            }

            // 设置背景颜色（可选）
            var backgroundImage = logObject.GetComponent<Image>();
            if (backgroundImage != null) backgroundImage.color = new Color(entry.displayColor.r, entry.displayColor.g, entry.displayColor.b, 0.1f);

            // 自动滚动到底部
            if (autoScrollToBottom && scrollRect != null)
                StartCoroutine(ScrollToBottomCoroutine());

            // 设置生命周期
            if (logLifetime > 0)
                StartCoroutine(LogLifetimeCoroutine(logObject));
        }

        /// <summary>
        ///     移除最旧的日志UI
        /// </summary>
        private void RemoveOldestLogUI()
        {
            if (_logUIElements.Count > 0)
            {
                var oldestLog = _logUIElements[0];
                _logUIElements.RemoveAt(0);

                if (oldestLog != null)
                {
                    // 播放退出动画
                    if (enableEntryAnimations)
                        oldestLog.transform.DOScale(0f, 0.2f).OnComplete(() => Destroy(oldestLog));
                    else
                        Destroy(oldestLog);
                }
            }
        }

        /// <summary>
        ///     滚动到底部协程
        /// </summary>
        private IEnumerator ScrollToBottomCoroutine()
        {
            yield return new WaitForEndOfFrame(); // 等待布局更新
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        ///     日志条目生命周期协程
        /// </summary>
        private IEnumerator LogLifetimeCoroutine(GameObject logObject)
        {
            yield return new WaitForSeconds(logLifetime);

            if (logObject != null && _logUIElements.Contains(logObject))
            {
                _logUIElements.Remove(logObject);

                // 播放淡出动画
                var canvasGroup = logObject.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                    canvasGroup.DOFade(0f, 0.5f).OnComplete(() => Destroy(logObject));
                else
                    Destroy(logObject);
            }
        }

        #endregion

        #region UI Control

        /// <summary>
        ///     切换日志面板显示/隐藏
        /// </summary>
        public void ToggleLogPanel()
        {
            _isPanelVisible = !_isPanelVisible;

            if (logPanel != null)
            {
                if (_isPanelVisible)
                    ShowLogPanel();
                else
                    HideLogPanel();
            }
        }

        /// <summary>
        ///     显示日志面板
        /// </summary>
        public void ShowLogPanel()
        {
            if (logPanel != null)
            {
                logPanel.SetActive(true);
                logPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                _isPanelVisible = true;
            }
        }

        /// <summary>
        ///     隐藏日志面板
        /// </summary>
        public void HideLogPanel()
        {
            if (logPanel != null)
            {
                logPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(() => logPanel.SetActive(false));
                _isPanelVisible = false;
            }
        }

        /// <summary>
        ///     清空所有日志
        /// </summary>
        public void ClearLogs()
        {
            // 清空数据
            _logEntries.Clear();

            // 清空UI元素
            foreach (var logUI in _logUIElements)
            {
                if (logUI != null)
                    Destroy(logUI);
            }

            _logUIElements.Clear();

            // 更新显示
            UpdateLogCountDisplay();

            // 触发事件
            OnLogCleared?.Invoke();

            LogMessage("日志已清空", LogType.System);
        }

        /// <summary>
        ///     更新日志数量显示
        /// </summary>
        private void UpdateLogCountDisplay()
        {
            if (logCountText != null)
                logCountText.text = $"日志 ({_logEntries.Count}/{maxLogEntries})";
        }

        #endregion

        #region Utility Methods

        /// <summary>
        ///     根据日志类型获取颜色
        /// </summary>
        private Color GetLogTypeColor(LogType logType)
        {
            return logType switch
            {
                LogType.Battle => battleColor,
                LogType.Experience => expColor,
                LogType.Coin => coinColor,
                LogType.Character => characterColor,
                LogType.System => systemColor,
                LogType.Warning => warningColor,
                _ => Color.white
            };
        }

        /// <summary>
        ///     获取最近的日志条目
        /// </summary>
        public LogEntry[] GetRecentLogs(int count = 20)
        {
            var logs = _logEntries.ToArray();
            var startIndex = Mathf.Max(0, logs.Length - count);
            var actualCount = Mathf.Min(count, logs.Length);

            var result = new LogEntry[actualCount];
            Array.Copy(logs, startIndex, result, 0, actualCount);

            return result;
        }

        /// <summary>
        ///     根据类型筛选日志
        /// </summary>
        public List<LogEntry> GetLogsByType(LogType logType)
        {
            var result = new List<LogEntry>();
            foreach (var entry in _logEntries)
            {
                if (entry.logType == logType)
                    result.Add(entry);
            }

            return result;
        }

        /// <summary>
        ///     获取日志统计信息
        /// </summary>
        public string GetLogStatistics()
        {
            var battleLogs = GetLogsByType(LogType.Battle).Count;
            var expLogs = GetLogsByType(LogType.Experience).Count;
            var coinLogs = GetLogsByType(LogType.Coin).Count;
            var systemLogs = GetLogsByType(LogType.System).Count;

            return $"日志统计：战斗({battleLogs}) 经验({expLogs}) 金币({coinLogs}) 系统({systemLogs})";
        }

        #endregion

        #region Debug Methods

        [ContextMenu("测试战斗日志")]
        public void TestBattleLog()
        {
            LogDamage(156f, true, "勇者", "哥布林");
            LogDamage(23f, false, "勇者", "哥布林");
            LogBattleResult(true, "勇者", "哥布林", 15.3f, 50, 30);
        }

        [ContextMenu("测试经验日志")]
        public void TestExpLog()
        {
            LogExpGain(100, "战斗胜利", "勇者");
            LogLevelUp("勇者", 5, 6);
        }

        [ContextMenu("测试金币日志")]
        public void TestCoinLog()
        {
            LogCoinChange(500, "战斗奖励");
            LogCoinChange(200, "购买经验", false);
        }

        [ContextMenu("测试系统日志")]
        public void TestSystemLog()
        {
            LogCharacterSwitch("勇者", "法师");
            LogGachaResult("剑士", "稀有", 100);
            LogRouteSwitch("战斗线", "金币线");
        }

        [ContextMenu("测试大量日志")]
        public void TestMassiveLogs()
        {
            StartCoroutine(MassiveLogTestCoroutine());
        }

        private IEnumerator MassiveLogTestCoroutine()
        {
            for (var i = 0; i < 20; i++)
            {
                LogMessage($"测试日志条目 #{i + 1}", (LogType)(i % 6));
                yield return new WaitForSeconds(0.1f);
            }
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class LogEntry
    {
        public string message;
        public LogType logType;
        public DateTime timestamp;
        public Color displayColor;

        public string GetFormattedMessage()
        {
            return $"[{timestamp:HH:mm:ss}] {message}";
        }

        public string GetShortTimeStamp()
        {
            return timestamp.ToString("HH:mm");
        }
    }

    public enum LogType
    {
        General, // 一般信息
        Battle, // 战斗相关
        Experience, // 经验相关
        Coin, // 金币相关
        Character, // 角色相关
        System, // 系统消息
        Warning // 警告消息
    }

    #endregion
}