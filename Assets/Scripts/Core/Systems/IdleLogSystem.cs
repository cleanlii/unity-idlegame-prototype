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
        [SerializeField] private GameObject logPanel; // Log panel
        [SerializeField] private Transform logContentParent; // Log content parent object
        [SerializeField] private GameObject logEntryPrefab; // Log entry prefab
        [SerializeField] private ScrollRect scrollRect; // Scroll area
        [SerializeField] private Button toggleButton; // Show/hide button
        [SerializeField] private Button clearButton; // Clear logs button
        [SerializeField] private TextMeshProUGUI logCountText; // Log count display

        [Header("Log Entry Settings")]
        [SerializeField] private int maxLogEntries = 50; // Maximum log entries
        [SerializeField] private bool showTimestamps = true; // Show timestamps
        [SerializeField] private bool autoScrollToBottom = true; // Auto scroll to bottom
        [SerializeField] private float logFadeInDuration = 0.3f; // Log fade in duration
        [SerializeField] private float logLifetime = 30f; // Log display lifetime (seconds)

        [Header("Highlight Settings")]
        [SerializeField] private Color battleColor = Color.red; // Battle related
        [SerializeField] private Color expColor = Color.blue; // Experience related
        [SerializeField] private Color coinColor = Color.yellow; // Coin related
        [SerializeField] private Color systemColor = Color.green; // System messages
        [SerializeField] private Color warningColor = Color.yellow; // Warning messages
        [SerializeField] private Color characterColor = Color.cyan; // Character related

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
            LogMessage("Game Log System Started", LogType.System);
        }

        #region Initialization

        private void InitializeLogSystem()
        {
            // Ensure UI components exist
            if (logPanel == null)
            {
                Debug.LogWarning("[IdleLogSystem] Log panel not set, creating default panel");
                CreateDefaultLogPanel();
            }

            // Initialize button events
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleLogPanel);

            if (clearButton != null)
                clearButton.onClick.AddListener(ClearLogs);
        }

        private void SetupUI()
        {
            // Update log count display
            UpdateLogCountDisplay();

            // Set initial panel state
            if (logPanel != null)
                logPanel.SetActive(_isPanelVisible);
        }

        private void CreateDefaultLogPanel()
        {
            // Code can be added here to create default log UI
            // It's recommended to preset UI components in the Scene
            Debug.LogWarning("[IdleLogSystem] Please set up log UI components in Scene");
        }

        #endregion

        #region Core Methods

        /// <summary>
        ///     Log general messages
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
        ///     Log battle damage
        /// </summary>
        public void LogDamage(float damage, bool isPlayerDamage, string playerName = "", string enemyName = "")
        {
            string message;
            LogType logType;

            if (isPlayerDamage)
            {
                message = $"[{playerName}] dealt {damage:F0} damage to [{enemyName}]";
                logType = LogType.Battle;
            }
            else
            {
                message = $"[{enemyName}] dealt {damage:F0} damage to [{playerName}]";
                logType = LogType.Battle;
            }

            LogMessage(message, logType);
        }

        /// <summary>
        ///     Log battle results
        /// </summary>
        public void LogBattleResult(bool victory, string playerName, string enemyName, float duration, int expReward = 0, int coinReward = 0)
        {
            var resultText = victory ? "defeated" : "was defeated by";
            var message = $"[{playerName}] {resultText} [{enemyName}]! Battle duration: {duration:F1}s";

            LogMessage(message, LogType.Battle);

            if (victory && (expReward > 0 || coinReward > 0))
            {
                if (expReward > 0)
                    LogMessage($"Gained experience: +{expReward}", LogType.Experience);
                if (coinReward > 0)
                    LogMessage($"Gained coins: +{coinReward}", LogType.Coin);
            }
        }

        /// <summary>
        ///     Log experience gain
        /// </summary>
        public void LogExpGain(long expAmount, string source = "", string characterName = IdleGameConst.LOG_PLAYER_NAME)
        {
            var sourceText = string.IsNullOrEmpty(source) ? "" : $" ({source})";
            var message = $"[{characterName}] gained {expAmount} experience{sourceText}";
            LogMessage(message, LogType.Experience);
        }

        /// <summary>
        ///     Log character level up
        /// </summary>
        public void LogLevelUp(string characterName, int oldLevel, int newLevel)
        {
            var message = $"[{characterName}] leveled up! Lv.{oldLevel} → Lv.{newLevel}";
            LogMessage(message, LogType.Character);
        }

        /// <summary>
        ///     Log coin changes
        /// </summary>
        public void LogCoinChange(long amount, string reason = "", bool isGain = true)
        {
            var action = isGain ? "Gained" : "Spent";
            var reasonText = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
            var message = $"{action} coins: {(isGain ? "+" : "-")}{Math.Abs(amount)}{reasonText}";
            LogMessage(message, LogType.Coin);
        }

        /// <summary>
        ///     Log character switching
        /// </summary>
        public void LogCharacterSwitch(string oldCharacter, string newCharacter)
        {
            var message = $"Character switched: [{oldCharacter}] → [{newCharacter}]";
            LogMessage(message, LogType.Character);
        }

        /// <summary>
        ///     Log gacha results
        /// </summary>
        public void LogGachaResult(string characterName, string rarity, int cost)
        {
            var message = $"Gacha success! Obtained [{characterName}] ({rarity}) - Cost: {cost} coins";
            LogMessage(message, LogType.Character);
        }

        /// <summary>
        ///     Log route switching
        /// </summary>
        public void LogRouteSwitch(string oldRoute, string newRoute)
        {
            var message = $"Route switched: {oldRoute} → {newRoute}";
            LogMessage(message, LogType.System);
        }

        /// <summary>
        ///     Log offline rewards
        /// </summary>
        public void LogOfflineReward(string rewardType, int amount, float offlineHours)
        {
            var message = $"Offline for {offlineHours:F1} hours, {rewardType} reward: +{amount}";
            LogMessage(message, LogType.System);
        }

        /// <summary>
        ///     Log shop purchases
        /// </summary>
        public void LogPurchase(string itemName, int cost, int amount)
        {
            var message = $"Purchased [{itemName}] x{amount}, cost: {cost} coins";
            LogMessage(message, LogType.Coin);
        }

        /// <summary>
        ///     Log character death
        /// </summary>
        public void LogCharacterDeath(string characterName)
        {
            var message = $"[{characterName}] has been defeated!";
            LogMessage(message, LogType.Battle);
        }

        /// <summary>
        ///     Log character revival
        /// </summary>
        public void LogCharacterRevive(string characterName)
        {
            var message = $"✨ [{characterName}] has been revived!";
            LogMessage(message, LogType.System);
        }

        /// <summary>
        ///     Log battle restart
        /// </summary>
        public void LogBattleRestart()
        {
            var message = "⚔Battle restarted - Both fighters at full health!";
            LogMessage(message, LogType.Battle);
        }

        /// <summary>
        ///     Log enemy spawn
        /// </summary>
        public void LogEnemySpawn(string enemyName, int enemyLevel)
        {
            var message = $"New enemy appeared: [{enemyName}] Lv.{enemyLevel}";
            LogMessage(message, LogType.Battle);
        }

        /// <summary>
        ///     Log save/load operations
        /// </summary>
        public void LogSaveOperation(bool isLoad, bool success, string details = "")
        {
            var operation = isLoad ? "Load" : "Save";
            var result = success ? "successful" : "failed";
            var message = $"{operation} {result}";

            if (!string.IsNullOrEmpty(details))
                message += $" - {details}";

            LogMessage(message, LogType.System);
        }

        /// <summary>
        ///     Log critical hits
        /// </summary>
        public void LogCriticalHit(string attackerName, float damage)
        {
            var message = $"CRITICAL HIT! [{attackerName}] dealt {damage:F0} critical damage!";
            LogMessage(message, LogType.Battle);
        }

        /// <summary>
        ///     Log special abilities
        /// </summary>
        public void LogSpecialAbility(string characterName, string abilityName, float damage = 0)
        {
            var message = damage > 0
                ? $"[{characterName}] used [{abilityName}] for {damage:F0} damage!"
                : $"[{characterName}] activated [{abilityName}]!";
            LogMessage(message, LogType.Battle);
        }

        /// <summary>
        ///     Log system warnings
        /// </summary>
        public void LogWarning(string warningMessage)
        {
            var message = $"Warning: {warningMessage}";
            LogMessage(message, LogType.Warning);
        }

        /// <summary>
        ///     Log system errors
        /// </summary>
        public void LogError(string errorMessage)
        {
            var message = $"Error: {errorMessage}";
            LogMessage(message, LogType.Warning);
        }

        #endregion

        #region UI Display

        /// <summary>
        ///     Add log entry to UI
        /// </summary>
        private void AddLogEntry(LogEntry entry)
        {
            // Add to data queue
            _logEntries.Enqueue(entry);

            // Remove old logs exceeding limit
            while (_logEntries.Count > maxLogEntries)
            {
                var oldEntry = _logEntries.Dequeue();
                RemoveOldestLogUI();
            }

            // Create UI element
            CreateLogUI(entry);

            // Trigger event
            OnNewLogEntry?.Invoke(entry);

            // Output to Unity console (for debugging)
            Debug.Log($"[Game Log] {entry.GetFormattedMessage()}");

            // Update UI display
            UpdateLogCountDisplay();
        }

        /// <summary>
        ///     Create log UI element
        /// </summary>
        private void CreateLogUI(LogEntry entry)
        {
            if (logContentParent == null || logEntryPrefab == null) return;

            // Instantiate log entry
            var logObject = Instantiate(logEntryPrefab, logContentParent);
            _logUIElements.Add(logObject);

            // Set log content
            var textComponent = logObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = entry.GetFormattedMessage();
                textComponent.color = entry.displayColor;
            }

            // Set background color (optional)
            var backgroundImage = logObject.GetComponent<Image>();
            if (backgroundImage != null)
                backgroundImage.color = new Color(entry.displayColor.r, entry.displayColor.g, entry.displayColor.b, 0.1f);

            // Auto scroll to bottom
            if (autoScrollToBottom && scrollRect != null)
                StartCoroutine(ScrollToBottomCoroutine());

            // Set lifetime
            if (logLifetime > 0)
                StartCoroutine(LogLifetimeCoroutine(logObject));
        }

        /// <summary>
        ///     Remove oldest log UI
        /// </summary>
        private void RemoveOldestLogUI()
        {
            if (_logUIElements.Count > 0)
            {
                var oldestLog = _logUIElements[0];
                _logUIElements.RemoveAt(0);

                if (oldestLog != null)
                {
                    // Play exit animation
                    if (enableEntryAnimations)
                        oldestLog.transform.DOScale(0f, 0.2f).OnComplete(() => Destroy(oldestLog));
                    else
                        Destroy(oldestLog);
                }
            }
        }

        /// <summary>
        ///     Scroll to bottom coroutine
        /// </summary>
        private IEnumerator ScrollToBottomCoroutine()
        {
            yield return new WaitForEndOfFrame(); // Wait for layout update
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        ///     Log entry lifetime coroutine
        /// </summary>
        private IEnumerator LogLifetimeCoroutine(GameObject logObject)
        {
            yield return new WaitForSeconds(logLifetime);

            if (logObject != null && _logUIElements.Contains(logObject))
            {
                _logUIElements.Remove(logObject);

                // Play fade out animation
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
        ///     Toggle log panel show/hide
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
        ///     Show log panel
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
        ///     Hide log panel
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
        ///     Clear all logs
        /// </summary>
        public void ClearLogs()
        {
            // Clear data
            _logEntries.Clear();

            // Clear UI elements
            foreach (var logUI in _logUIElements)
            {
                if (logUI != null)
                    Destroy(logUI);
            }

            _logUIElements.Clear();

            // Update display
            UpdateLogCountDisplay();

            // Trigger event
            OnLogCleared?.Invoke();

            LogMessage("Logs cleared", LogType.System);
        }

        /// <summary>
        ///     Update log count display
        /// </summary>
        private void UpdateLogCountDisplay()
        {
            if (logCountText != null)
                logCountText.text = $"Logs ({_logEntries.Count}/{maxLogEntries})";
        }

        #endregion

        #region Utility Methods

        /// <summary>
        ///     Get color based on log type
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
        ///     Get recent log entries
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
        ///     Filter logs by type
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
        ///     Get log statistics
        /// </summary>
        public string GetLogStatistics()
        {
            var battleLogs = GetLogsByType(LogType.Battle).Count;
            var expLogs = GetLogsByType(LogType.Experience).Count;
            var coinLogs = GetLogsByType(LogType.Coin).Count;
            var systemLogs = GetLogsByType(LogType.System).Count;

            return $"Log Stats: Battle({battleLogs}) Experience({expLogs}) Coins({coinLogs}) System({systemLogs})";
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Battle Logs")]
        public void TestBattleLog()
        {
            LogDamage(156f, true, "Hero", "Goblin");
            LogDamage(23f, false, "Hero", "Goblin");
            LogBattleResult(true, "Hero", "Goblin", 15.3f, 50, 30);
            LogCriticalHit("Hero", 234f);
            LogSpecialAbility("Hero", "Power Strike", 180f);
        }

        [ContextMenu("Test Experience Logs")]
        public void TestExpLog()
        {
            LogExpGain(100, "Battle Victory", "Hero");
            LogLevelUp("Hero", 5, 6);
        }

        [ContextMenu("Test Coin Logs")]
        public void TestCoinLog()
        {
            LogCoinChange(500, "Battle Reward");
            LogCoinChange(200, "Experience Purchase", false);
            LogPurchase("Health Potion", 50, 3);
        }

        [ContextMenu("Test System Logs")]
        public void TestSystemLog()
        {
            LogCharacterSwitch("Hero", "Mage");
            LogGachaResult("Swordsman", "Rare", 100);
            LogRouteSwitch("Battle Route", "Coin Route");
            LogSaveOperation(false, true, "Auto-save completed");
        }

        [ContextMenu("Test Warning Logs")]
        public void TestWarningLog()
        {
            LogWarning("Low health detected");
            LogError("Failed to load character data");
            LogCharacterDeath("Hero");
            LogCharacterRevive("Hero");
        }

        [ContextMenu("Test Massive Logs")]
        public void TestMassiveLogs()
        {
            StartCoroutine(MassiveLogTestCoroutine());
        }

        private IEnumerator MassiveLogTestCoroutine()
        {
            for (var i = 0; i < 20; i++)
            {
                LogMessage($"Test log entry #{i + 1}", (LogType)(i % 6));
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
        General, // General information
        Battle, // Battle related
        Experience, // Experience related
        Coin, // Coin related
        Character, // Character related
        System, // System messages
        Warning // Warning messages
    }

    #endregion
}