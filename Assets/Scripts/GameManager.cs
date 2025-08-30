using System;
using System.Collections;
using IdleGame.Analytics;
using IdleGame.Core;
using IdleGame.Gameplay;
using IdleGame.Gameplay.Battle;
using UnityEngine;

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

    [Header("Game Settings")]
    [SerializeField] private float autoSaveInterval = 60f; // 自动保存间隔(秒)
    [SerializeField] private bool enableAutoSave = true;

    [Header("Economy Settings")]
    [SerializeField] private int expCost = 10; // 1点经验值的金币成本
    [SerializeField] private int gachaCost = 100; // 单次抽卡金币成本

    // 状态标记
    private bool isInitialized;
    private Coroutine autoSaveCoroutine;

    // 事件
    public Action<PlayerData> OnPlayerDataLoaded;
    public Action<PlayerData> OnPlayerDataSaved;
    public Action<long, long> OnCurrencyChanged; // oldAmount, newAmount

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
        // 确保所有组件初始化完成后再加载数据
        StartCoroutine(DelayedInitialization());
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveGame();
        else
            HandleGameResume();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveGame();
        else if (isInitialized) HandleGameResume();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SaveGame();
            if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);
        }
    }

    #region 初始化

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
    }

    private void InitializeGameplay()
    {
        characterSystem.Initialize();
    }

    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForEndOfFrame();

        // 加载玩家数据
        LoadGame();

        // 计算离线时间和奖励
        HandleOfflineProgress();

        yield return new WaitForEndOfFrame();
        InitializeGameplay();

        // 启动自动保存
        StartAutoSave();

        isInitialized = true;
        Debug.Log("[GameManager] Game initialization completed!");
    }

    private void ValidateComponents()
    {
        if (logSystem == null)
            logSystem = FindObjectOfType<IdleLogSystem>();

        if (characterSystem == null)
            characterSystem = FindObjectOfType<CharacterSystem>();

        if (spireSystem == null)
            spireSystem = FindObjectOfType<SpireSystem>();

        if (ecoSystem == null)
            ecoSystem = FindObjectOfType<EcoSystem>();

        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();

        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
    }

    #endregion

    #region 存档系统

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
            logSystem?.LogMessage("游戏数据已保存");
        else
            logSystem?.LogMessage("保存失败，请检查存储空间");
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

            OnPlayerDataLoaded?.Invoke(playerData);
            logSystem?.LogMessage($"欢迎回来，{playerData.playerName}！");
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
            logSystem?.LogMessage("存档已删除，开始新游戏");
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

        // 同步拥有的角色数据
        var ownedCharacters = characterSystem.GetOwnedCharacters();
        foreach (var character in ownedCharacters)
        {
            var saveData = character.GetSaveData();
            // 这里需要将CharacterSaveData转换为PlayerData中的CharacterData格式
            // 暂时跳过具体实现，实际需要根据数据结构调整
        }

        // 同步当前角色ID
        if (characterSystem.currentCharacter != null)
        {
            // playerData.currentCharacterID = characterSystem.currentCharacter.config.characterID;
        }
    }

    /// <summary>
    ///     同步数据到各个系统
    /// </summary>
    private void SyncDataToSystems()
    {
        if (playerData == null) return;

        // 同步到角色系统
        if (characterSystem != null)
        {
            // 这里需要从PlayerData恢复角色数据
            // characterSystem.LoadFromPlayerData(playerData);
        }

        if (spireSystem != null) SwitchRoute(playerData.selectedRoute);

        // 同步到其他系统...
    }

    #endregion

    #region 自动保存

    private void StartAutoSave()
    {
        if (enableAutoSave && autoSaveCoroutine == null) autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
    }

    private IEnumerator AutoSaveCoroutine()
    {
        while (enableAutoSave)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            SaveGame();
        }
    }

    #endregion

    #region 离线系统

    /// <summary>
    ///     处理离线进度
    /// </summary>
    private void HandleOfflineProgress()
    {
        if (playerData == null) return;

        var offlineSeconds = playerData.CalculateOfflineTime();

        if (offlineSeconds > 60) // 离线超过1分钟才计算奖励
        {
            var offlineHours = offlineSeconds / 3600f;
            CalculateOfflineRewards(offlineHours);
        }
    }

    /// <summary>
    ///     计算离线奖励
    /// </summary>
    private void CalculateOfflineRewards(float offlineHours)
    {
        // 基于选择的路线计算奖励
        switch (playerData.selectedRoute)
        {
            case RouteType.Battle:
                CalculateBattleOfflineRewards(offlineHours);
                break;
            case RouteType.Economy:
                CalculateEconomyOfflineRewards(offlineHours);
                break;
            case RouteType.Experience:
                CalculateExpOfflineRewards(offlineHours);
                break;
        }
    }

    private void CalculateBattleOfflineRewards(float offlineHours)
    {
        // 战斗线：经验 + 少量金币
        var expReward = (long)(offlineHours * 50); // 每小时50经验
        var coinReward = (long)(offlineHours * 20); // 每小时20金币

        characterSystem?.GainExperience(expReward);
        AddCoins(coinReward);

        logSystem?.LogOfflineReward("战斗", (int)(expReward + coinReward), offlineHours);
    }

    private void CalculateEconomyOfflineRewards(float offlineHours)
    {
        // 经济线：大量金币
        var coinReward = (long)(offlineHours * 80); // 每小时80金币
        AddCoins(coinReward);

        logSystem?.LogOfflineReward("金币", (int)coinReward, offlineHours);
    }

    private void CalculateExpOfflineRewards(float offlineHours)
    {
        // 经验线：大量经验
        var expReward = (long)(offlineHours * 120); // 每小时120经验
        characterSystem?.GainExperience(expReward);

        logSystem?.LogOfflineReward("经验", (int)expReward, offlineHours);
    }

    /// <summary>
    ///     处理游戏恢复（从后台回到前台）
    /// </summary>
    private void HandleGameResume()
    {
        if (playerData != null) HandleOfflineProgress();
    }

    #endregion

    #region 货币系统

    /// <summary>
    ///     添加金币
    /// </summary>
    public void AddCoins(long amount)
    {
        if (playerData == null || amount <= 0) return;

        var oldAmount = playerData.coins;
        playerData.AddCoins(amount);

        OnCurrencyChanged?.Invoke(oldAmount, playerData.coins);
        logSystem?.LogMessage($"获得金币：+{amount} (总计：{playerData.coins})");
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
            logSystem?.LogMessage($"消费金币：-{amount} {reasonText} (剩余：{playerData.coins})");
            return true;
        }

        logSystem?.LogMessage($"金币不足！需要：{amount}，当前：{playerData.coins}");
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

    #region 游戏功能

    /// <summary>
    ///     购买经验值
    /// </summary>
    public void BuyExperience(long expAmount)
    {
        var totalCost = expAmount * expCost;

        if (SpendCoins(totalCost, "购买经验"))
        {
            characterSystem?.GainExperience(expAmount);
            logSystem?.LogMessage($"购买经验：{expAmount}点 (花费{totalCost}金币)");
        }
    }

    /// <summary>
    ///     抽卡
    /// </summary>
    public void PerformGacha()
    {
        if (SpendCoins(gachaCost, "抽卡"))
        {
            // 这里需要调用GachaSystem
            // var newCharacter = gachaSystem.DrawCharacter();
            // characterSystem.AddCharacter(newCharacter);

            logSystem?.LogMessage($"进行抽卡 (花费{gachaCost}金币)");
            // 暂时的测试代码
            logSystem?.LogMessage("抽到角色：测试角色 (稀有度：普通)");
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

        logSystem?.LogMessage($"切换路线：{oldRoute} → {newRoute}");

        // 通知SpireSystem
        spireSystem.SwitchToRoute(newRoute);
    }

    #endregion

    #region 事件回调

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
        logSystem?.LogMessage($"保存出错：{error}");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     记录日志消息
    /// </summary>
    private void LogMessage(string message)
    {
        logSystem?.LogMessage(message);
        Debug.Log($"[GameManager] {message}");
    }

    #endregion

    #region 调试和测试

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

    #region 公开API

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
}