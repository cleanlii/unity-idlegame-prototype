using System;
using System.Collections;
using System.Collections.Generic;
using IdleGame;
using IdleGame.Analytics;
using IdleGame.Character;
using IdleGame.Core;
using IdleGame.Gameplay;
using IdleGame.Gameplay.Battle;
using UnityEngine;
using CharacterSaveData = IdleGame.Analytics.CharacterSaveData;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    [Header("Game Data")]
    public PlayerData playerData;

    [Header("Managers")]
    public BattleManager battleManager;
    public UIManager uiManager;

    [Header("Systems")]
    public EcoSystem ecoSystem;
    public IdleLogSystem logSystem;
    public SpireSystem spireSystem;
    public CharacterSystem characterSystem;

    [Header("Controllers")]
    public PlayerController playerController;

    [Header("Game Settings")]
    [SerializeField] private float autoSaveInterval = 60f; // 自动保存间隔(秒)
    [SerializeField] private bool enableAutoSave = true;

    [Header("Economy Settings")]
    [SerializeField] private int expCost = 10; // 1点经验值的金币成本
    [SerializeField] private int gachaCost = 100; // 单次抽卡金币成本

    [Header("离线奖励设置")]
    [SerializeField] private float maxOfflineHours = 24f; // 最大离线时长
    [SerializeField] private float minOfflineMinutes = 1f; // 最小离线时长
    [SerializeField] private bool enableOfflineRewards = true;

    [Header("离线奖励倍率")]
    [SerializeField] private OfflineRewardConfig offlineConfig;

    [Header("防作弊设置")]
    [SerializeField] private bool enableAntiCheat = true;
    [SerializeField] private float maxReasonableOfflineHours = 72f; // 合理的最大离线时长
    [SerializeField] private int timeSyncCheckInterval = 300; // 时间同步检查间隔(秒)

    // 状态标记
    private bool _isInitialized;
    private Coroutine _autoSaveCoroutine;
    private DateTime _lastTimeSyncCheck;
    private float _suspiciousTimeCount; // 可疑时间修改计数

    // 事件
    public Action<PlayerData> OnPlayerDataLoaded;
    public Action<PlayerData> OnPlayerDataSaved;
    public Action<long, long> OnCurrencyChanged; // oldAmount, newAmount

    public Action<OfflineRewardData> OnOfflineRewardsCalculated;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null) Debug.LogError("No GameManager found in the scene.");
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeService();
        }
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        Screen.SetResolution(900, 1947, false);

        // 确保所有组件初始化完成后再加载数据
        StartCoroutine(DelayedInitialization());
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SaveGame();
            if (_autoSaveCoroutine != null) StopCoroutine(_autoSaveCoroutine);
        }
    }

    #region Initialization

    private void InitializeService()
    {
        Debug.Log("[GameManager] Starting game initialization...");

        // 注册存档系统事件
        SaveSystem.OnDataLoaded += OnDataLoaded;
        SaveSystem.OnDataSaved += OnDataSaved;
        SaveSystem.OnSaveError += OnSaveError;

        ServiceLocator.Register(ecoSystem);
        ServiceLocator.Register(logSystem);
        ServiceLocator.Register(spireSystem);
        ServiceLocator.Register(characterSystem);
        ServiceLocator.Register(battleManager);
        ServiceLocator.Register(uiManager);
        ServiceLocator.Register(playerController);
    }

    private void InitializeSystems()
    {
        characterSystem.Initialize();
        spireSystem.Initialize();

        // TODO: Optimize general initialization flow
        // logSystem.Initilize();
        // uiManager.Initilize();
    }

    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForEndOfFrame();

        // 加载玩家数据
        LoadGame();

        yield return new WaitForEndOfFrame();

        InitializeSystems();

        // 启动自动保存
        StartAutoSave();

        yield return new WaitForEndOfFrame();

        StartGame();

        _isInitialized = true;
        Debug.Log("[GameManager] Game initialization completed!");
    }

    private void StartGame()
    {
        Debug.Log("[GameManager] Starting game...");

        if (spireSystem != null) SwitchRoute(playerData.selectedRoute);
    }

    #endregion

    #region Save System

    /// <summary>
    ///     保存游戏数据
    /// </summary>
    public void SaveGame()
    {
        if (playerData == null) return;

        // 更新游戏时长
        playerData.UpdatePlayTime();

        // 同步角色数据到PlayerData
        SyncCharacterDataToPlayerData();

        // 执行保存
        var success = SaveSystem.SavePlayerData(playerData);

        if (success)
            LogMessage("Game data saved: " + playerData.currentCharacterID);
        else
            LogMessage("Save failed, please check storage space");
    }

    /// <summary>
    ///     加载游戏数据
    /// </summary>
    public void LoadGame()
    {
        playerData = SaveSystem.LoadPlayerData();

        if (playerData != null)
        {
            Debug.Log($"[GameManager] Loaded player: {playerData.playerName}, Level: {playerData.playerLevel}");

            // 同步数据到各个系统
            SyncDataToSystems();

            // 计算离线时间和奖励
            HandleOfflineProgress();

            OnPlayerDataLoaded?.Invoke(playerData);
            LogMessage($"Welcome back, {playerData.playerName}!");
        }
        else
            Debug.LogError("[GameManager] Failed to load player data!");
    }

    /// <summary>
    ///     删除存档
    /// </summary>
    public void DeleteSave()
    {
        var success = SaveSystem.DeleteSaveData();
        if (success)
        {
            // 重新创建新的PlayerData
            playerData = new PlayerData();
            SyncDataToSystems();
            LogMessage("Save data deleted, starting a new game");
        }
    }

    private void StartAutoSave()
    {
        if (enableAutoSave && _autoSaveCoroutine == null) _autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
    }

    private IEnumerator AutoSaveCoroutine()
    {
        while (enableAutoSave)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            SaveGame();
        }
    }

    /// <summary>
    ///     同步角色数据到PlayerData
    /// </summary>
    private void SyncCharacterDataToPlayerData()
    {
        if (characterSystem == null || playerData == null) return;

        // 清空现有数据
        playerData.ownedCharacters.Clear();

        // TODO: Better sync methods
        var ownedCharacters = characterSystem.GetOwnedCharacters();
        foreach (var character in ownedCharacters)
        {
            if (character.IsNull) continue;

            // CharacterData -> CharacterSaveData
            var saveData = new CharacterSaveData
            {
                configID = character.config.characterID,
                level = character.level,
                totalExperience = character.totalExperience,
                totalBattles = character.totalBattles,
                victoriesCount = character.victoriesCount,
                totalDamageDealt = character.totalDamageDealt,
                totalDamageTaken = character.totalDamageTaken,
                lastUsedTime = DateTime.Now
            };

            playerData.ownedCharacters.Add(saveData);
        }

        // 同步当前角色ID
        if (!characterSystem.currentCharacter.IsNull) playerData.currentCharacterID = characterSystem.currentCharacter.config.characterID;

        LogMessage($"Synchronized {ownedCharacters.Count} character data entries to save");
    }

    /// <summary>
    ///     同步数据到各个系统
    /// </summary>
    private void SyncDataToSystems()
    {
        if (playerData == null) return;

        // TODO: Better sync methods

        // 同步到角色系统
        if (characterSystem != null) LoadCharacterDataFromPlayerData();

        // 同步UI显示
        if (uiManager != null) uiManager.UpdateAllUI();
    }

    /// <summary>
    ///     从PlayerData加载角色数据到CharacterSystem
    /// </summary>
    private void LoadCharacterDataFromPlayerData()
    {
        if (characterSystem?.characterDb == null || playerData?.ownedCharacters == null) return;

        // 清空CharacterSystem中的角色数据
        characterSystem.GetOwnedCharacters().Clear();

        // 重新加载角色
        foreach (var characterSave in playerData.ownedCharacters)
        {
            if (string.IsNullOrEmpty(characterSave.configID)) continue;

            // 查找角色配置
            var config = characterSystem.characterDb.GetCharacterConfig(characterSave.configID);
            if (config == null)
            {
                Debug.LogWarning($"Character config not found: {characterSave.configID}");
                continue;
            }

            // 创建CharacterData并恢复存档状态
            var characterData = new CharacterData(config, characterSave.level, characterSave.totalExperience);

            // 恢复战斗统计
            characterData.totalBattles = characterSave.totalBattles;
            characterData.victoriesCount = characterSave.victoriesCount;
            characterData.totalDamageDealt = characterSave.totalDamageDealt;
            characterData.totalDamageTaken = characterSave.totalDamageTaken;

            // 添加到角色系统
            characterSystem.AddCharacter(characterData);
        }

        // 设置当前角色
        if (!string.IsNullOrEmpty(playerData.currentCharacterID))
        {
            var success = characterSystem.SwitchCharacter(playerData.currentCharacterID);
            if (!success)
            {
                Debug.LogWarning($"Failed to switch to character {playerData.currentCharacterID}, using default character instead");
                SetDefaultCurrentCharacter();
            }
        }
        else
            SetDefaultCurrentCharacter();

        LogMessage($"Loaded {playerData.ownedCharacters.Count} characters from save data");
    }

    private void SetDefaultCurrentCharacter()
    {
        var ownedCharacters = characterSystem.GetOwnedCharacters();
        if (ownedCharacters.Count > 0)
            characterSystem.SwitchCharacter(ownedCharacters[0].config.characterID);
        else
        {
            // 如果没有角色，创建默认角色
            if (characterSystem.characterDb?.defaultCharacter != null)
            {
                characterSystem.AddCharacterByConfig(characterSystem.characterDb.defaultCharacter);
                characterSystem.SwitchCharacter(characterSystem.characterDb.defaultCharacter.characterID);
            }
        }
    }

    #endregion

    #region Currency System

    /// <summary>
    ///     添加金币
    /// </summary>
    public void AddCoins(long amount)
    {
        if (playerData == null || amount <= 0) return;

        var oldAmount = playerData.coins;
        playerData.AddCoins(amount);

        OnCurrencyChanged?.Invoke(oldAmount, playerData.coins);
        LogMessage($"{IdleGameConst.LOG_PLAYER_NAME} +{amount} Coins (Total: {playerData.coins})");
    }

    /// <summary>
    ///     消费金币
    /// </summary>
    public bool SpendCoins(long amount, string reason = "")
    {
        if (playerData == null || amount <= 0) return false;

        if (playerData.SpendCoins(amount))
        {
            OnCurrencyChanged?.Invoke(playerData.coins + amount, playerData.coins);
            var reasonText = string.IsNullOrEmpty(reason) ? "" : $"({reason})";
            LogMessage($"{IdleGameConst.LOG_PLAYER_NAME} spent coins: -{amount} {reasonText} (Remaining: {playerData.coins})");
            return true;
        }

        LogMessage($"{IdleGameConst.LOG_PLAYER_NAME} don't have enough coins! Required: {amount}, Current: {playerData.coins}");
        return false;
    }

    /// <summary>
    ///     获取当前金币数量
    /// </summary>
    public long GetCoins()
    {
        return playerData?.coins ?? 0;
    }

    /// <summary>
    ///     检查金币是否足够
    /// </summary>
    public bool CanAfford(long amount)
    {
        return playerData != null && playerData.coins >= amount;
    }

    #endregion

    #region Gameplay Logic

    /// <summary>
    ///     购买经验值
    /// </summary>
    public void BuyExperience(long expAmount)
    {
        var totalCost = expAmount * expCost;

        if (SpendCoins(totalCost, "Buy EXP"))
        {
            characterSystem?.GainExperience(expAmount);
            LogMessage($"{IdleGameConst.LOG_PLAYER_NAME} purchased {expAmount} EXP (Cost: {totalCost} coins)");
        }
    }

    /// <summary>
    ///     抽卡
    /// </summary>
    public void PerformGacha()
    {
        if (SpendCoins(gachaCost, "Gacha"))
        {
            // 实际的抽卡逻辑
            if (characterSystem?.characterDb != null)
            {
                var newCharacter = characterSystem.characterDb.GetRandomCharacter();
                if (newCharacter != null)
                {
                    characterSystem.AddCharacterByConfig(newCharacter);
                    logSystem?.LogGachaResult(newCharacter.characterName,
                        newCharacter.rarity.ToString(),
                        gachaCost);
                }
            }
            else
            {
                // 测试代码
                logSystem?.LogGachaResult("Test Character", "Common", gachaCost);
            }
        }
    }

    /// <summary>
    ///     切换路线
    /// </summary>
    public void SwitchRoute(RouteType newRoute)
    {
        if (playerData == null) return;

        var oldRoute = playerData.selectedRoute;
        playerData.selectedRoute = newRoute;

        playerController.MoveToRoute(newRoute);

        Debug.Log($"{IdleGameConst.LOG_PLAYER_NAME} switched Route：{oldRoute} → {newRoute}");

        // 通知SpireSystem
        spireSystem.SwitchToRoute(newRoute);
    }

    #endregion

    #region Offline Rewarding

    /// <summary>
    ///     处理离线进度
    /// </summary>
    private void HandleOfflineProgress()
    {
        if (playerData == null) return;

        var offlineData = CalculateOfflineTime();

        if (!offlineData.isValid)
        {
            LogMessage($"Invalid offline time: {offlineData.reason}");
            return;
        }

        if (offlineData.totalMinutes < minOfflineMinutes)
        {
            LogMessage($"Offline time insufficient ({offlineData.totalMinutes:F1} min), minimum required: {minOfflineMinutes} min");
            return;
        }

        // 计算离线奖励
        var rewards = CalculateOfflineRewards(offlineData);

        // 应用奖励
        ApplyOfflineRewards(rewards);

        // 记录离线数据
        playerData.offlineDuration = (long)offlineData.totalSeconds;
        playerData.totalOfflineRewards += rewards.totalValue;

        // 触发事件
        OnOfflineRewardsCalculated?.Invoke(rewards);

        LogMessage($"离线奖励处理完成：{rewards.GetSummary()}");
    }

    /// <summary>
    ///     计算离线时间
    /// </summary>
    private OfflineTimeData CalculateOfflineTime()
    {
        var currentTime = DateTime.Now;
        var lastSaveTime = playerData.lastSaveTime;
        var offlineTimeSpan = currentTime - lastSaveTime;

        // Debug.LogError($"offline time: {playerData.CalculateOfflineTime()}");
        // Debug.LogError($"lastSaveTime: {playerData.lastSaveTime}");
        // Debug.LogError($"currentTime: {DateTime.Now}");

        var data = new OfflineTimeData
        {
            startTime = lastSaveTime,
            endTime = currentTime,
            totalSeconds = offlineTimeSpan.TotalSeconds,
            totalMinutes = offlineTimeSpan.TotalMinutes,
            totalHours = offlineTimeSpan.TotalHours
        };

        // 防作弊检测
        data.isValid = data.totalMinutes >= 0; // 基础验证：时间不能倒退
        data.reason = data.isValid ? "正常" : "时间倒退";

        return data;
    }

    /// <summary>
    ///     计算离线奖励
    /// </summary>
    private OfflineRewardData CalculateOfflineRewards(OfflineTimeData offlineData)
    {
        var rewards = new OfflineRewardData
        {
            offlineHours = (float)offlineData.totalHours,
            route = playerData.selectedRoute
        };

        // 限制最大奖励时长
        var effectiveHours = Math.Min(offlineData.totalHours, maxOfflineHours);

        // 根据角色等级调整基础倍率
        var levelBonus = 1f + (characterSystem.currentCharacter?.level ?? 1) * 0.02f;

        switch (playerData.selectedRoute)
        {
            case RouteType.Battle:
                CalculateBattleOfflineRewards(rewards, effectiveHours, levelBonus);
                break;
            case RouteType.Economy:
                CalculateEconomyOfflineRewards(rewards, effectiveHours, levelBonus);
                break;
            case RouteType.Experience:
                CalculateExpOfflineRewards(rewards, effectiveHours, levelBonus);
                break;
        }

        return rewards;
    }

    private void CalculateBattleOfflineRewards(OfflineRewardData rewards, double effectiveHours, float levelBonus)
    {
        // 战斗线：经验为主，少量金币
        var baseExpPerHour = offlineConfig.battleExpPerHour * levelBonus;
        var baseCoinPerHour = offlineConfig.battleCoinPerHour * levelBonus;

        // 模拟战斗效率递减（长时间离线效率降低）
        var efficiencyFactor = CalculateEfficiencyFactor(effectiveHours);

        rewards.expReward = (long)(baseExpPerHour * effectiveHours * efficiencyFactor);
        rewards.coinReward = (long)(baseCoinPerHour * effectiveHours * efficiencyFactor);

        // 模拟战斗次数
        var avgBattleTime = 10f; // 平均每场战斗10秒
        var estimatedBattles = (int)(effectiveHours * 3600 / avgBattleTime * efficiencyFactor);
        rewards.simulatedBattles = estimatedBattles;

        rewards.description = $"模拟 {estimatedBattles} 场战斗";
    }

    private void CalculateEconomyOfflineRewards(OfflineRewardData rewards, double effectiveHours, float levelBonus)
    {
        // 经济线：金币为主
        var baseCoinPerHour = offlineConfig.economyCoinPerHour * levelBonus;
        var efficiencyFactor = CalculateEfficiencyFactor(effectiveHours);

        rewards.coinReward = (long)(baseCoinPerHour * effectiveHours * efficiencyFactor);
        rewards.expReward = 0; // 经济线不给经验

        rewards.description = $"金币挖掘 {effectiveHours:F1} 小时";
    }

    private void CalculateExpOfflineRewards(OfflineRewardData rewards, double effectiveHours, float levelBonus)
    {
        // 经验线：经验为主
        var baseExpPerHour = offlineConfig.experienceExpPerHour * levelBonus;
        var efficiencyFactor = CalculateEfficiencyFactor(effectiveHours);

        rewards.expReward = (long)(baseExpPerHour * effectiveHours * efficiencyFactor);
        rewards.coinReward = 0; // 经验线不给金币

        rewards.description = $"经验修炼 {effectiveHours:F1} 小时";
    }

    /// <summary>
    ///     计算效率因子（长时间离线效率递减）
    /// </summary>
    private float CalculateEfficiencyFactor(double hours)
    {
        if (hours <= 1) return 1f; // 1小时内全效率
        if (hours <= 6) return 0.9f; // 6小时内90%效率
        if (hours <= 12) return 0.8f; // 12小时内80%效率
        if (hours <= 24) return 0.7f; // 24小时内70%效率
        return 0.5f; // 超过24小时50%效率
    }

    /// <summary>
    ///     应用离线奖励
    /// </summary>
    private void ApplyOfflineRewards(OfflineRewardData rewards)
    {
        var rewardMessages = new List<string>();

        // Apply experience rewards
        if (rewards.expReward > 0)
        {
            characterSystem?.GainExperience(rewards.expReward);
            rewardMessages.Add($"EXP +{rewards.expReward:N0}");
        }

        // Apply coin rewards
        if (rewards.coinReward > 0)
        {
            AddCoins(rewards.coinReward);
            rewardMessages.Add($"Coins +{rewards.coinReward:N0}");
        }

        // Log detailed information
        var rewardSummary = string.Join(", ", rewardMessages);
        var bonusText = rewards.hasBonus ? $" (Multiplier: {rewards.bonusMultiplier:F1}x)" : "";

        LogMessage("=== OFFLINE REWARDS ===");
        LogMessage($"Offline Duration: {rewards.offlineHours:F1} hours");
        LogMessage($"Route: {GetRouteDisplayName(rewards.route)}");
        LogMessage($"Rewards: {rewardSummary}{bonusText}");
        LogMessage($"Details: {rewards.description}");

        if (rewards.simulatedBattles > 0)
            LogMessage($"Simulated Battles: {rewards.simulatedBattles} fights");
    }

    #endregion

    #region Event Handlers

    private void OnDataLoaded(PlayerData data)
    {
        Debug.Log($"[GameManager] Player data loaded: {data.playerName}");
    }

    private void OnDataSaved(PlayerData data)
    {
        Debug.Log($"[GameManager] Player data saved: {data.playerName}");
        OnPlayerDataSaved?.Invoke(data);
    }

    private void OnSaveError(string error)
    {
        Debug.LogError($"[GameManager] Save error: {error}");
        // LogMessage($"Save error：{error}");
    }

    #endregion

    #region Debug Methods

    [ContextMenu("保存游戏")]
    public void TestSaveGame()
    {
        SaveGame();
    }

    [ContextMenu("加载游戏")]
    public void TestLoadGame()
    {
        LoadGame();
    }

    [ContextMenu("添加测试金币")]
    public void TestAddCoins()
    {
        AddCoins(1000);
    }

    [ContextMenu("测试购买经验")]
    public void TestBuyExp()
    {
        BuyExperience(100);
    }

    [ContextMenu("测试抽卡")]
    public void TestGacha()
    {
        PerformGacha();
    }

    [ContextMenu("删除存档")]
    public void TestDeleteSave()
    {
        DeleteSave();
    }

    #endregion

    #region Public API

    /// <summary>
    ///     获取存档信息
    /// </summary>
    public string GetSaveFileInfo()
    {
        return SaveSystem.GetSaveFileInfo();
    }

    /// <summary>
    ///     检查是否有存档
    /// </summary>
    public bool HasSaveData()
    {
        return SaveSystem.HasSaveData();
    }

    /// <summary>
    ///     获取经验购买成本
    /// </summary>
    public long GetExpCost(long expAmount)
    {
        return expAmount * expCost;
    }

    /// <summary>
    ///     获取抽卡成本
    /// </summary>
    public int GetGachaCost()
    {
        return gachaCost;
    }

    #endregion


    #region Utility Methods

    private string GetRouteDisplayName(RouteType route)
    {
        return route switch
        {
            RouteType.Battle => "战斗线",
            RouteType.Economy => "金币线",
            RouteType.Experience => "经验线",
            _ => "未知路线"
        };
    }

    private void LogMessage(string message)
    {
        logSystem?.LogMessage(message);
    }

    #endregion
}