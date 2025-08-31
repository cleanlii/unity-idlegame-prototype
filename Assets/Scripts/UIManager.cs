using System.Collections;
using DG.Tweening;
using IdleGame.Character;
using IdleGame.Gameplay;
using IdleGame.Gameplay.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;

    [Header("角色信息UI")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterLevelText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText; // "当前HP/最大HP" 格式
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText; // "当前EXP/下级所需EXP" 格式

    [Header("敌人信息UI")]
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI enemyLevelText;
    [SerializeField] private Slider enemyHpSlider;
    [SerializeField] private TextMeshProUGUI enemyHpText;

    [Header("功能按钮")]
    [SerializeField] private Button getCoinButton;
    [SerializeField] private Button buyExpButton;
    [SerializeField] private Button gachaButton;
    [SerializeField] private Button battleRouteButton;
    [SerializeField] private Button economyRouteButton;
    [SerializeField] private Button experienceRouteButton;

    [Header("路线显示")]
    [SerializeField] private TextMeshProUGUI currentRouteText;
    [SerializeField] private Image routeIndicator; // 路线指示器
    [SerializeField] private Color battleRouteColor = Color.red;
    [SerializeField] private Color economyRouteColor = Color.yellow;
    [SerializeField] private Color experienceRouteColor = Color.blue;

    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease animationEase = Ease.OutQuart;
    [SerializeField] private float numberCountDuration = 1f;

    [Header("特效")]
    [SerializeField] private GameObject levelUpEffect; // 升级特效
    [SerializeField] private GameObject coinGainEffect; // 金币获得特效
    [SerializeField] private GameObject expGainEffect; // 经验获得特效

    // 缓存数据，用于检测变化
    private long lastCoinAmount;
    private float lastHP;
    private float lastMaxHP;
    private long lastExp;
    private long lastExpToNext;
    private int lastLevel;
    private string lastCharacterName = "";

    // 敌人相关缓存
    private string lastEnemyName = "";
    private int lastEnemyLevel;
    private float lastEnemyHP;
    private float lastEnemyMaxHP;
    private bool lastBattleActiveState;

    // 动画序列
    private Sequence coinAnimSequence;
    private Sequence hpAnimSequence;
    private Sequence expAnimSequence;
    private Sequence enemyHpAnimSequence;

    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                if (_instance == null)
                    Debug.LogError("No UIManager found in the scene.");
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            InitializeUI();
        }
        else if (_instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        SubscribeToEvents();
        StartCoroutine(DelayedInitialization());
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        KillAllAnimations();
    }

    #region 初始化

    private void InitializeUI()
    {
        // 初始化按钮事件
        SetupButtons();

        // 初始化UI显示
        UpdateAllUI();

        Debug.Log("[UIManager] UI initialized");
    }

    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForSeconds(0.1f);

        // 等待GameManager初始化完成后更新UI
        if (GameManager.Instance != null) UpdateAllUI();
    }

    private void SetupButtons()
    {
        if (getCoinButton != null)
        {
            getCoinButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.TestAddCoins();
                AnimateButton(getCoinButton);
            });
        }

        // 购买经验按钮
        if (buyExpButton != null)
        {
            buyExpButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.TestBuyExp();
                AnimateButton(buyExpButton);
            });
        }

        // 抽卡按钮
        if (gachaButton != null)
        {
            gachaButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.TestGacha();
                AnimateButton(gachaButton);
            });
        }

        // 路线切换按钮
        if (battleRouteButton != null)
        {
            battleRouteButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.SwitchRoute(RouteType.Battle);
                AnimateButton(battleRouteButton);
                AnimateRouteSwitch();
            });
        }

        if (economyRouteButton != null)
        {
            economyRouteButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.SwitchRoute(RouteType.Economy);
                AnimateButton(economyRouteButton);
                AnimateRouteSwitch();
            });
        }

        if (experienceRouteButton != null)
        {
            experienceRouteButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.SwitchRoute(RouteType.Experience);
                AnimateButton(experienceRouteButton);
                AnimateRouteSwitch();
            });
        }
    }

    #endregion

    #region 事件订阅

    private void SubscribeToEvents()
    {
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            // 订阅货币变化事件
            gameManager.OnCurrencyChanged += OnCurrencyChanged;

            // 订阅角色系统事件
            if (gameManager.characterSystem != null)
            {
                gameManager.characterSystem.OnCharacterSwitched += OnCharacterSwitched;
                gameManager.characterSystem.OnCharacterLevelUp += OnCharacterLevelUp;
                gameManager.characterSystem.OnExperienceGained += OnExperienceGained;
            }

            if (gameManager.spireSystem != null)
            {
                gameManager.spireSystem.OnRouteChanged += UpdateRouteDisplay;
                gameManager.spireSystem.OnEnemySpawned += UpdateEnemyInfo;
            }

            if (gameManager.battleManager != null)
            {
                gameManager.battleManager.OnEnemyHPChanged += OnEnemyHPChanged;
                gameManager.battleManager.OnDamageDealt += OnDamageDealt;
                gameManager.battleManager.OnBattleStarted += OnBattleStarted;
                gameManager.battleManager.OnBattleEnded += OnBattleEnded;
                gameManager.battleManager.OnBattleRestarted += OnBattleRestarted;
                gameManager.battleManager.OnPlayerDied += OnPlayerDied;
                gameManager.battleManager.OnPlayerRevived += OnPlayerRevived;
            }
        }
    }

    private void UnsubscribeFromEvents()
    {
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnCurrencyChanged -= OnCurrencyChanged;

            if (gameManager.characterSystem != null)
            {
                gameManager.characterSystem.OnCharacterSwitched -= OnCharacterSwitched;
                gameManager.characterSystem.OnCharacterLevelUp -= OnCharacterLevelUp;
                gameManager.characterSystem.OnExperienceGained -= OnExperienceGained;
            }

            if (gameManager.uiManager != null) gameManager.spireSystem.OnRouteChanged -= UpdateRouteDisplay;

            if (gameManager.battleManager != null)
            {
                gameManager.battleManager.OnDamageDealt -= OnDamageDealt;
                gameManager.battleManager.OnBattleStarted -= OnBattleStarted;
                gameManager.battleManager.OnBattleEnded -= OnBattleEnded;
                gameManager.battleManager.OnBattleRestarted -= OnBattleRestarted;
                gameManager.battleManager.OnPlayerDied -= OnPlayerDied;
                gameManager.battleManager.OnPlayerRevived -= OnPlayerRevived;
            }
        }
    }

    #endregion

    #region UI更新方法

    /// <summary>
    ///     更新所有UI元素
    /// </summary>
    public void UpdateAllUI()
    {
        UpdateCharacterInfo();
        UpdateEnemyInfo();
        UpdateCoinDisplay();
        UpdateRouteDisplay();
    }

    /// <summary>
    ///     更新角色信息显示
    /// </summary>
    private void UpdateCharacterInfo()
    {
        var characterSystem = GameManager.Instance?.characterSystem;
        if (!characterSystem) return;

        if (characterSystem.currentCharacter.IsNull)
        {
            SetDefaultCharacterDisplay();
            return;
        }

        var character = characterSystem.currentCharacter;

        // 更新角色名称
        UpdateCharacterName(character.config.characterName);

        // 更新等级
        UpdateLevel(character.level);

        // 更新血量
        UpdatePlayerHp(character.currentHP, character.GetMaxHP());

        // 更新经验
        UpdateExperience(character);
    }

    /// <summary>
    ///     设置默认角色显示（无角色时）
    /// </summary>
    private void SetDefaultCharacterDisplay()
    {
        if (characterNameText != null) characterNameText.text = "Unknown";
        if (characterLevelText != null) characterLevelText.text = "Lv.0";
        if (hpSlider != null) hpSlider.value = 0;
        if (hpText != null) hpText.text = "0/0";
        if (expSlider != null) expSlider.value = 0;
        if (expText != null) expText.text = "0/0";
    }

    /// <summary>
    ///     更新角色名称
    /// </summary>
    private void UpdateCharacterName(string newName)
    {
        if (characterNameText == null || lastCharacterName == newName) return;

        lastCharacterName = newName;

        // 文字缩放动画
        characterNameText.text = newName;
        characterNameText.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
    }

    /// <summary>
    ///     更新等级显示
    /// </summary>
    private void UpdateLevel(int newLevel)
    {
        if (characterLevelText == null || lastLevel == newLevel) return;

        lastLevel = newLevel;

        // 等级变化动画
        characterLevelText.text = $"Lv.{newLevel}";
        characterLevelText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
    }

    private void UpdateEnemyInfo()
    {
        var battleManager = GameManager.Instance?.battleManager;
        var spireSystem = GameManager.Instance?.spireSystem;
        var currentEnemy = battleManager?.GetCurrentEnemy();

        // 检查是否应该显示敌人信息
        var shouldShowEnemy = currentEnemy != null && spireSystem?.currentRoute == RouteType.Battle;

        if (!shouldShowEnemy)
        {
            ResetEnemyUICache();
            return;
        }

        // 显示并更新敌人信息
        UpdateEnemyName(currentEnemy.enemyName);
        UpdateEnemyLevel(currentEnemy.recommendedLevel);
        UpdateEnemyHp(currentEnemy.currentHP, currentEnemy.maxHP);
    }


    /// <summary>
    ///     重置敌人UI缓存
    /// </summary>
    private void ResetEnemyUICache()
    {
        lastEnemyName = "";
        lastEnemyLevel = 0;
        lastEnemyHP = 0;
        lastEnemyMaxHP = 0;
    }

    /// <summary>
    ///     玩家死亡事件处理
    /// </summary>
    private void OnPlayerDied()
    {
        ShowMessage("角色阵亡！准备复活...");

        // 播放玩家死亡特效
        if (characterNameText != null)
        {
            var originalColor = characterNameText.color;
            characterNameText.DOColor(Color.red, 0.3f).OnComplete(() => { characterNameText.DOColor(originalColor, 0.5f); });
        }

        // 角色血量条闪烁
        if (hpSlider != null)
        {
            var fillImage = hpSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.DOFade(0.3f, 0.5f).SetLoops(4, LoopType.Yoyo);
        }
    }

    /// <summary>
    ///     玩家复活事件处理
    /// </summary>
    private void OnPlayerRevived()
    {
        ShowMessage("角色复活！", 1.5f);

        // 播放复活特效
        if (characterNameText != null)
        {
            var originalColor = characterNameText.color;
            characterNameText.DOColor(Color.green, 0.2f).OnComplete(() => { characterNameText.DOColor(originalColor, 0.3f); });
        }

        // 血量条恢复动画
        if (hpSlider != null)
        {
            hpSlider.DOValue(1f, 0.5f).SetEase(Ease.OutQuart);

            var fillImage = hpSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.DOFade(1f, 0.3f); // 恢复不透明度
        }

        // 角色面板发光效果
        if (characterNameText != null) characterNameText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
    }

    /// <summary>
    ///     战斗重启事件处理
    /// </summary>
    private void OnBattleRestarted()
    {
        ShowMessage("战斗重新开始！", 1.5f);

        // 强制刷新所有UI
        UpdateAllUI();
    }


    /// <summary>
    ///     更新敌人名称
    /// </summary>
    private void UpdateEnemyName(string newName)
    {
        if (enemyNameText == null) return;

        if (lastEnemyName != newName)
        {
            lastEnemyName = newName;
            enemyNameText.text = newName;

            // 名称变化动画
            enemyNameText.transform.DOKill();
            enemyNameText.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f).SetEase(Ease.OutElastic);
        }
        else
            enemyNameText.text = newName;
    }

    /// <summary>
    ///     更新敌人等级
    /// </summary>
    private void UpdateEnemyLevel(int newLevel)
    {
        if (enemyLevelText == null) return;

        if (lastEnemyLevel != newLevel)
        {
            lastEnemyLevel = newLevel;
            enemyLevelText.text = $"Lv.{newLevel}";

            // 等级变化闪烁动画
            var originalColor = enemyLevelText.color;
            enemyLevelText.DOColor(Color.yellow, 0.2f).OnComplete(() => { enemyLevelText.DOColor(originalColor, 0.3f); });
        }
        else
            enemyLevelText.text = $"Lv.{newLevel}";
    }

    /// <summary>
    ///     敌人血量条专用动画
    /// </summary>
    private void AnimateEnemySlider(Slider slider, float targetValue)
    {
        if (slider == null) return;

        // 终止之前的动画
        enemyHpAnimSequence?.Kill();

        enemyHpAnimSequence = DOTween.Sequence();
        enemyHpAnimSequence.Append(slider.DOValue(targetValue, animationDuration).SetEase(animationEase));

        // 血量减少时的特殊效果
        if (targetValue < slider.value)
        {
            var fillImage = slider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                var originalColor = fillImage.color;
                enemyHpAnimSequence.Join(
                    fillImage.DOColor(Color.white, 0.1f).OnComplete(() => { fillImage.DOColor(originalColor, 0.3f); })
                );
            }
        }
    }

    /// <summary>
    ///     强制刷新敌人UI
    /// </summary>
    public void ForceRefreshEnemyUI()
    {
        ResetEnemyUICache();
        UpdateEnemyInfo();
    }

    private void OnBattleStarted(EnemyData enemy)
    {
        UpdateEnemyInfo();
        ShowMessage($"开始战斗：{enemy.enemyName}！", 1.5f);
    }

    private void OnBattleEnded(bool victory, EnemyData enemy, float duration)
    {
        var resultText = victory ? "胜利" : "失败";
        var message = $"战斗{resultText}！用时{duration:F1}秒";
        ShowMessage(message);

        // if (victory) PlayVictoryEffect();
    }

    private void OnEnemyHPChanged(EnemyData enemy)
    {
        if (enemy != null)
        {
            // 检查是否受伤
            var wasDamaged = enemy.currentHP < lastEnemyHP && lastEnemyHP > 0;

            UpdateEnemyHp(enemy.currentHP, enemy.maxHP);
        }
    }

    private void OnDamageDealt(float damage, bool isPlayerAttack)
    {
        if (isPlayerAttack)
        {
            // 玩家攻击敌人特效
            PlayDamageNumberEffect(damage, Color.yellow, enemyHpSlider?.transform);
        }
        else
        {
            // 敌人攻击玩家特效
            PlayDamageNumberEffect(damage, Color.red, hpSlider?.transform);
        }
    }

    private void OnEnemySpawned(EnemyData enemy)
    {
        ResetEnemyUICache();
        UpdateEnemyInfo();
        ShowMessage($"遭遇敌人：{enemy.enemyName}！", 1.5f);
    }


    /// <summary>
    ///     播放伤害数字特效
    /// </summary>
    private void PlayDamageNumberEffect(float damage, Color color, Transform target)
    {
        if (target == null) return;

        target.DOPunchScale(Vector3.one * 0.08f, 0.15f);

        var targetImage = target.GetComponent<Image>();
        if (targetImage != null)
        {
            var originalColor = targetImage.color;
            targetImage.DOColor(color, 0.1f).OnComplete(() => { targetImage.DOColor(originalColor, 0.2f); });
        }
    }

    /// <summary>
    ///     更新血量显示
    /// </summary>
    private void UpdatePlayerHp(float currentHP, float maxHP)
    {
        if (hpSlider == null && hpText == null) return;

        var hpChanged = !Mathf.Approximately(lastHP, currentHP) ||
                        !Mathf.Approximately(lastMaxHP, maxHP);

        if (!hpChanged) return;

        lastHP = currentHP;
        lastMaxHP = maxHP;

        // 更新血量条
        if (hpSlider != null)
        {
            var targetValue = maxHP > 0 ? currentHP / maxHP : 0;
            AnimateSlider(hpSlider, targetValue, Color.red);
        }

        // 更新血量文字
        if (hpText != null) AnimateNumberText(hpText, $"{currentHP:F0}/{maxHP:F0}", Color.white);
    }

    private void UpdateEnemyHp(float currentHP, float maxHP)
    {
        var hpChanged = !Mathf.Approximately(lastEnemyHP, currentHP) ||
                        !Mathf.Approximately(lastEnemyMaxHP, maxHP);

        // 更新血量条
        if (enemyHpSlider != null)
        {
            var targetValue = maxHP > 0 ? currentHP / maxHP : 0;

            if (hpChanged)
                AnimateEnemySlider(enemyHpSlider, targetValue);
            else
                enemyHpSlider.value = targetValue;
        }

        // 更新血量文字
        if (enemyHpText != null)
        {
            var hpDisplayText = $"{currentHP:F0}/{maxHP:F0}";

            if (hpChanged)
                AnimateNumberText(enemyHpText, hpDisplayText, Color.white);
            else
                enemyHpText.text = hpDisplayText;
        }

        // 更新缓存
        if (hpChanged)
        {
            lastEnemyHP = currentHP;
            lastEnemyMaxHP = maxHP;
        }
    }

    /// <summary>
    ///     更新经验显示
    /// </summary>
    private void UpdateExperience(CharacterData character)
    {
        if (expSlider == null && expText == null) return;

        var currentLevelExp = character.currentLevelExp;
        var expToNext = character.GetExpToNextLevel() - character.config.CalculateExpRequired(character.level);
        var expProgress = character.GetExpProgress();

        var expChanged = lastExp != currentLevelExp || lastExpToNext != expToNext;

        if (!expChanged) return;

        lastExp = currentLevelExp;
        lastExpToNext = expToNext;

        // 更新经验条
        if (expSlider != null) AnimateSlider(expSlider, expProgress, Color.blue);

        // 更新经验文字
        if (expText != null)
        {
            var expText = expToNext > 0 ? $"{currentLevelExp}/{expToNext}" : $"{character.totalExperience}/MAX";
            AnimateNumberText(this.expText, expText, Color.white);
        }
    }

    /// <summary>
    ///     更新金币显示
    /// </summary>
    private void UpdateCoinDisplay()
    {
        if (coinText == null) return;

        var currentCoins = GameManager.Instance?.GetCoins() ?? 0;

        if (lastCoinAmount != currentCoins)
        {
            AnimateNumberChange(coinText, lastCoinAmount, currentCoins, "");
            lastCoinAmount = currentCoins;
        }
    }

    /// <summary>
    ///     更新路线显示
    /// </summary>
    private void UpdateRouteDisplay()
    {
        var playerData = GameManager.Instance?.playerData;
        if (playerData == null) return;

        var currentRoute = playerData.selectedRoute;

        if (currentRouteText != null)
        {
            var routeText = currentRoute switch
            {
                RouteType.Battle => "Fight!",
                RouteType.Economy => "COIN++",
                RouteType.Experience => "EXP++",
                _ => "Unknown"
            };
            currentRouteText.text = routeText;
        }

        if (routeIndicator != null)
        {
            var routeColor = currentRoute switch
            {
                RouteType.Battle => battleRouteColor,
                RouteType.Economy => economyRouteColor,
                RouteType.Experience => experienceRouteColor,
                _ => Color.white
            };
            routeIndicator.DOColor(routeColor, 0.3f);
        }
    }

    private void UpdateRouteDisplay(RouteType newRoute)
    {
        if (GameManager.Instance?.playerData.selectedRoute != newRoute) return;

        UpdateRouteDisplay();
    }

    private void UpdateEnemyInfo(EnemyData newEnemy)
    {
        if (GameManager.Instance?.spireSystem.currentEnemy != newEnemy) return;

        UpdateEnemyInfo();
    }

    #endregion

    #region 动画方法

    /// <summary>
    ///     滑动条动画
    /// </summary>
    private void AnimateSlider(Slider slider, float targetValue, Color flashColor)
    {
        slider.DOValue(targetValue, animationDuration).SetEase(animationEase);

        // 添加闪烁效果
        var fillImage = slider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            var originalColor = fillImage.color;
            fillImage.DOColor(flashColor, 0.1f).OnComplete(() => { fillImage.DOColor(originalColor, 0.2f); });
        }
    }

    /// <summary>
    ///     数字文字动画
    /// </summary>
    private void AnimateNumberText(TextMeshProUGUI textComponent, string newText, Color flashColor)
    {
        if (textComponent == null) return;

        var originalColor = textComponent.color;

        textComponent.text = newText;
        textComponent.DOColor(flashColor, 0.1f).OnComplete(() => { textComponent.DOColor(originalColor, 0.2f); });

        textComponent.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f);
    }

    /// <summary>
    ///     数值变化动画（计数器效果）
    /// </summary>
    private void AnimateNumberChange(TextMeshProUGUI textComponent, long fromValue, long toValue, string suffix)
    {
        if (textComponent == null) return;

        // 终止之前的动画
        coinAnimSequence?.Kill();
        textComponent.transform.localScale = Vector3.one;

        // 创建新的动画序列
        coinAnimSequence = DOTween.Sequence();

        coinAnimSequence.Append(
            DOTween.To(() => fromValue,
                    x => textComponent.text = $"{x:N0}{suffix}",
                    toValue,
                    numberCountDuration)
                .SetEase(Ease.OutQuart)
        );

        // 添加缩放效果
        coinAnimSequence.Join(
            textComponent.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f)
        );
    }

    /// <summary>
    ///     按钮点击动画
    /// </summary>
    private void AnimateButton(Button button)
    {
        if (button == null) return;

        button.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
    }

    /// <summary>
    ///     路线切换动画
    /// </summary>
    private void AnimateRouteSwitch()
    {
        if (routeIndicator != null) routeIndicator.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f);

        if (currentRouteText != null) currentRouteText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
    }

    /// <summary>
    ///     升级特效动画
    /// </summary>
    private void PlayLevelUpEffect()
    {
    }

    /// <summary>
    ///     金币获得特效
    /// </summary>
    private void PlayCoinGainEffect()
    {
    }

    /// <summary>
    ///     经验获得特效
    /// </summary>
    private void PlayExpGainEffect()
    {
    }

    #endregion

    #region 事件处理

    private void OnCharacterSwitched(CharacterData character)
    {
        // 立即更新角色信息
        UpdateCharacterInfo();

        // 播放切换动画
        if (characterNameText != null) characterNameText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f);
    }

    private void OnCharacterLevelUp(CharacterData character, int newLevel)
    {
        // 播放升级特效
        PlayLevelUpEffect();

        // 更新UI
        UpdateCharacterInfo();
    }

    private void OnExperienceGained(CharacterData character, long expAmount)
    {
        // 播放经验获得特效
        PlayExpGainEffect();

        // 更新经验显示
        UpdateExperience(character);
    }

    private void OnCurrencyChanged(long oldAmount, long newAmount)
    {
        if (newAmount > oldAmount)
        {
            // 金币增加时播放特效
            PlayCoinGainEffect();
        }

        // 更新金币显示
        AnimateNumberChange(coinText, oldAmount, newAmount, "");
        lastCoinAmount = newAmount;
    }

    #endregion

    #region 公开方法

    /// <summary>
    ///     强制更新UI（外部调用）
    /// </summary>
    public void RefreshUI()
    {
        UpdateAllUI();
    }

    /// <summary>
    ///     显示消息提示
    /// </summary>
    public void ShowMessage(string message, float duration = 2f)
    {
        // TODO: 实现消息提示UI
        Debug.Log($"[UI Message] {message}");
    }

    #endregion

    #region 工具方法

    private void Update()
    {
        // 定期更新UI（低频率）
        if (Time.frameCount % 30 == 0) // 每0.5秒更新一次
        {
            UpdateCharacterInfo();
            UpdateCoinDisplay();
        }
    }

    private void KillAllAnimations()
    {
        coinAnimSequence?.Kill();
        hpAnimSequence?.Kill();
        expAnimSequence?.Kill();

        DOTween.Kill(this);
    }

    #endregion

    #region 调试方法

    [ContextMenu("测试升级动画")]
    public void TestLevelUpAnimation()
    {
        PlayLevelUpEffect();
    }

    [ContextMenu("测试金币动画")]
    public void TestCoinAnimation()
    {
        AnimateNumberChange(coinText, lastCoinAmount, lastCoinAmount + 1000, "");
        PlayCoinGainEffect();
    }

    [ContextMenu("测试经验动画")]
    public void TestExpAnimation()
    {
        PlayExpGainEffect();
    }

    #endregion
}