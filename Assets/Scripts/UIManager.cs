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

    [Header("Character Info UI")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterLevelText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText; // "currentHP/maxHP" format
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText; // "currentEXP/neededEXP" format

    [Header("Enemy Info UI")]
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI enemyLevelText;
    [SerializeField] private Slider enemyHpSlider;
    [SerializeField] private TextMeshProUGUI enemyHpText;

    [Header("Buttons")]
    [SerializeField] private Button getCoinButton;
    [SerializeField] private Button buyExpButton;
    [SerializeField] private Button gachaButton;
    [SerializeField] private Button battleRouteButton;
    [SerializeField] private Button economyRouteButton;
    [SerializeField] private Button experienceRouteButton;

    [Header("Route Display")]
    [SerializeField] private TextMeshProUGUI currentRouteText;
    [SerializeField] private Image routeIndicator;
    [SerializeField] private Color battleRouteColor = Color.red;
    [SerializeField] private Color economyRouteColor = Color.yellow;
    [SerializeField] private Color experienceRouteColor = Color.blue;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease animationEase = Ease.OutQuart;
    [SerializeField] private float numberCountDuration = 1f;

    [Header("VFX (TODO)")]
    [SerializeField] private GameObject levelUpEffect;
    [SerializeField] private GameObject coinGainEffect;
    [SerializeField] private GameObject expGainEffect;

    // Cached data for player
    private long _lastCoinAmount;
    private float _lastHp;
    private float _lastMaxHp;
    private long _lastExp;
    private long _lastExpToNext;
    private int _lastLevel;
    private string _lastCharacterName = "";

    // Cached data for enemy
    private string _lastEnemyName = "";
    private int _lastEnemyLevel;
    private float _lastEnemyHp;
    private float _lastEnemyMaxHp;
    private bool _lastBattleActiveState;

    // Anim sequence
    private Sequence _coinAnimSequence;
    private Sequence _hpAnimSequence;
    private Sequence _expAnimSequence;
    private Sequence _enemyHpAnimSequence;

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

    private void Update()
    {
        // Low pace update for essential UI elements
        if (Time.frameCount % 30 == 0) // 0.5s
        {
            UpdateCharacterInfo();
            UpdateCoinDisplay();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        KillAllAnimations();
    }

    private void KillAllAnimations()
    {
        _coinAnimSequence?.Kill();
        _hpAnimSequence?.Kill();
        _expAnimSequence?.Kill();

        DOTween.Kill(this);
    }

    #region Initialization

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

        // But the XP
        if (buyExpButton != null)
        {
            buyExpButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.TestBuyExp();
                AnimateButton(buyExpButton);
            });
        }

        // Gacha Test
        if (gachaButton != null)
        {
            gachaButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.TestGacha();
                AnimateButton(gachaButton);
            });
        }

        // Switch to battle route
        if (battleRouteButton != null)
        {
            battleRouteButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.SwitchRoute(RouteType.Battle);
                AnimateButton(battleRouteButton);
                AnimateRouteSwitch();
            });
        }

        // Switch to eco route
        if (economyRouteButton != null)
        {
            economyRouteButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.SwitchRoute(RouteType.Economy);
                AnimateButton(economyRouteButton);
                AnimateRouteSwitch();
            });
        }

        // Switch to exp route
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

    #region Event Subscription

    private void SubscribeToEvents()
    {
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            // Eco system
            gameManager.OnCurrencyChanged += OnCurrencyChanged;

            // Character system
            if (gameManager.characterSystem != null)
            {
                gameManager.characterSystem.OnCharacterSwitched += OnCharacterSwitched;
                gameManager.characterSystem.OnCharacterLevelUp += OnCharacterLevelUp;
                gameManager.characterSystem.OnExperienceGained += OnExperienceGained;
            }

            // Spire system
            if (gameManager.spireSystem != null)
            {
                gameManager.spireSystem.OnRouteChanged += UpdateRouteDisplay;
                gameManager.spireSystem.OnEnemySpawned += UpdateEnemyInfo;
            }

            // Battle feature
            if (gameManager.battleManager != null)
            {
                gameManager.battleManager.onEnemyHpChanged += OnEnemyHPChanged;
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

    #region Update UI Display

    /// <summary>
    ///     Update all UI elements
    /// </summary>
    public void UpdateAllUI()
    {
        UpdateCharacterInfo();
        UpdateEnemyInfo();
        UpdateCoinDisplay();
        UpdateRouteDisplay();
    }

    /// <summary>
    ///     Damaging VFX
    /// </summary>
    private void PlayDamageNumberEffect(float damage, Color color, Transform target)
    {
        if (target == null) return;

        target.DOPunchScale(Vector3.one * 0.08f, 0.15f);

        // TODO: Jumping numbers...
        // TODO: more efficient VFX...
        var targetImage = target.GetComponent<Image>();
        if (targetImage != null)
        {
            var originalColor = targetImage.color;
            targetImage.DOColor(color, 0.1f).OnComplete(() => { targetImage.DOColor(originalColor, 0.2f); });
        }
    }

    #endregion

    #region Player UI

    /// <summary>
    ///     Update character info display
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

        // Name
        UpdateCharacterName(character.config.characterName);

        // Level
        UpdateLevel(character.level);

        // HP
        UpdatePlayerHp(character.currentHP, character.GetMaxHP());

        // EXP
        UpdateExperience(character);
    }

    /// <summary>
    ///     Set default character display (when no character is available)
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

    private void UpdateCharacterName(string newName)
    {
        if (characterNameText == null || _lastCharacterName == newName) return;

        _lastCharacterName = newName;

        // Animation when update the text
        characterNameText.text = newName;
        characterNameText.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
    }

    private void UpdateLevel(int newLevel)
    {
        if (!characterLevelText || _lastLevel == newLevel) return;

        _lastLevel = newLevel;

        // Animation for upgrading
        characterLevelText.text = $"Lv.{newLevel}";
        characterLevelText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
    }

    private void UpdatePlayerHp(float currentHp, float maxHp)
    {
        if (!hpSlider && !hpText) return;

        var hpChanged = !Mathf.Approximately(_lastHp, currentHp) ||
                        !Mathf.Approximately(_lastMaxHp, maxHp);

        if (!hpChanged) return;

        _lastHp = currentHp;
        _lastMaxHp = maxHp;

        // Update slider
        if (hpSlider)
        {
            var targetValue = maxHp > 0 ? currentHp / maxHp : 0;
            AnimateSlider(hpSlider, targetValue, Color.red);
        }

        // Update TMP text
        if (hpText) AnimateNumberText(hpText, $"{currentHp:F0}/{maxHp:F0}", Color.white);
    }


    private void UpdateExperience(CharacterData character)
    {
        if (!expSlider && !expText) return;

        var currentLevelExp = character.currentLevelExp;
        var expToNext = character.GetExpToNextLevel() - character.config.CalculateExpRequired(character.level);
        var expProgress = character.GetExpProgress();

        var expChanged = _lastExp != currentLevelExp || _lastExpToNext != expToNext;

        if (!expChanged) return;

        _lastExp = currentLevelExp;
        _lastExpToNext = expToNext;

        // Update slider
        if (expSlider) AnimateSlider(expSlider, expProgress, Color.blue);

        // Update TMP text
        if (expText)
        {
            var newText = expToNext > 0 ? $"{currentLevelExp}/{expToNext}" : $"{character.totalExperience}/MAX";
            AnimateNumberText(expText, newText, Color.white);
        }
    }

    private void UpdateCoinDisplay()
    {
        if (coinText == null) return;

        var currentCoins = GameManager.Instance?.GetCoins() ?? 0;

        if (_lastCoinAmount != currentCoins)
        {
            AnimateNumberChange(coinText, _lastCoinAmount, currentCoins, "");
            _lastCoinAmount = currentCoins;
        }
    }

    #endregion

    #region Route UI

    private void UpdateRouteDisplay(RouteType newRoute)
    {
        if (GameManager.Instance?.playerData.selectedRoute != newRoute) return;

        UpdateRouteDisplay();
    }

    /// <summary>
    ///     Update current route status
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

    #endregion

    #region Enemy UI

    private void UpdateEnemyInfo(EnemyData newEnemy)
    {
        if (GameManager.Instance?.spireSystem.currentEnemy != newEnemy) return;

        UpdateEnemyInfo();
    }

    private void UpdateEnemyInfo()
    {
        var battleManager = GameManager.Instance?.battleManager;
        var spireSystem = GameManager.Instance?.spireSystem;
        var currentEnemy = battleManager?.GetCurrentEnemy();

        // Check null
        var shouldShowEnemy = currentEnemy != null && spireSystem?.currentRoute == RouteType.Battle;

        if (!shouldShowEnemy)
        {
            ResetEnemyUICache();
            return;
        }

        // Update enemy info elements
        UpdateEnemyName(currentEnemy.enemyName);
        UpdateEnemyLevel(currentEnemy.recommendedLevel);
        UpdateEnemyHp(currentEnemy.currentHP, currentEnemy.maxHP);
    }

    private void UpdateEnemyHp(float currentHp, float maxHp)
    {
        var hpChanged = !Mathf.Approximately(_lastEnemyHp, currentHp) ||
                        !Mathf.Approximately(_lastEnemyMaxHp, maxHp);

        // HP bar
        if (enemyHpSlider != null)
        {
            var targetValue = maxHp > 0 ? currentHp / maxHp : 0;

            if (hpChanged)
                AnimateEnemySlider(enemyHpSlider, targetValue);
            else
                enemyHpSlider.value = targetValue;
        }

        // Text
        if (enemyHpText != null)
        {
            var hpDisplayText = $"{currentHp:F0}/{maxHp:F0}";

            if (hpChanged)
                AnimateNumberText(enemyHpText, hpDisplayText, Color.white);
            else
                enemyHpText.text = hpDisplayText;
        }

        // Cache
        if (hpChanged)
        {
            _lastEnemyHp = currentHp;
            _lastEnemyMaxHp = maxHp;
        }
    }

    private void UpdateEnemyName(string newName)
    {
        if (enemyNameText == null) return;

        if (_lastEnemyName != newName)
        {
            _lastEnemyName = newName;
            enemyNameText.text = newName;

            // 名称变化动画
            enemyNameText.transform.DOKill();
            enemyNameText.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f).SetEase(Ease.OutElastic);
        }
        else
            enemyNameText.text = newName;
    }

    private void UpdateEnemyLevel(int newLevel)
    {
        if (!enemyLevelText) return;

        if (_lastEnemyLevel != newLevel)
        {
            _lastEnemyLevel = newLevel;
            enemyLevelText.text = $"Lv.{newLevel}";

            // 等级变化闪烁动画
            var originalColor = enemyLevelText.color;
            enemyLevelText.DOColor(Color.yellow, 0.2f).OnComplete(() => { enemyLevelText.DOColor(originalColor, 0.3f); });
        }
        else
            enemyLevelText.text = $"Lv.{newLevel}";
    }

    private void AnimateEnemySlider(Slider slider, float targetValue)
    {
        if (!slider) return;

        // 终止之前的动画
        _enemyHpAnimSequence?.Kill();

        _enemyHpAnimSequence = DOTween.Sequence();
        _enemyHpAnimSequence.Append(slider.DOValue(targetValue, animationDuration).SetEase(animationEase));

        // TODO: Temp animation
        if (targetValue < slider.value)
        {
            var fillImage = slider.fillRect?.GetComponent<Image>();
            if (fillImage)
            {
                var originalColor = fillImage.color;
                _enemyHpAnimSequence.Join(
                    fillImage.DOColor(Color.white, 0.1f).OnComplete(() => { fillImage.DOColor(originalColor, 0.3f); })
                );
            }
        }
    }

    /// <summary>
    ///     Force to fresh if needed
    /// </summary>
    public void ForceRefreshEnemyUI()
    {
        ResetEnemyUICache();
        UpdateEnemyInfo();
    }


    /// <summary>
    ///     Reset enemy UI cache
    /// </summary>
    private void ResetEnemyUICache()
    {
        _lastEnemyName = "";
        _lastEnemyLevel = 0;
        _lastEnemyHp = 0;
        _lastEnemyMaxHp = 0;
    }

    #endregion

    #region Aniamtion Methods

    /// <summary>
    ///     Animate slider for player
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
    ///     Animate for the scale
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
    ///     Animate counting effects
    /// </summary>
    private void AnimateNumberChange(TextMeshProUGUI textComponent, long fromValue, long toValue, string suffix)
    {
        if (textComponent == null) return;

        // Reset the sequence
        _coinAnimSequence?.Kill();
        textComponent.transform.localScale = Vector3.one;

        // Create new sequence
        _coinAnimSequence = DOTween.Sequence();

        _coinAnimSequence.Append(
            DOTween.To(() => fromValue,
                    x => textComponent.text = $"{x:N0}{suffix}",
                    toValue,
                    numberCountDuration)
                .SetEase(Ease.OutQuart)
        );

        // TODO: Temp effect
        _coinAnimSequence.Join(
            textComponent.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f)
        );
    }

    /// <summary>
    ///     Animate for button press
    /// </summary>
    private void AnimateButton(Button button)
    {
        if (button == null) return;

        button.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
    }

    /// <summary>
    ///     Animate for route switch
    /// </summary>
    private void AnimateRouteSwitch()
    {
        if (routeIndicator != null) routeIndicator.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f);

        if (currentRouteText != null) currentRouteText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
    }

    private void PlayLevelUpEffect()
    {
        // TODO: Particle VFX
    }

    private void PlayCoinGainEffect()
    {
        // TODO: Particle VFX
    }

    private void PlayExpGainEffect()
    {
        // TODO: Particle VFX
    }

    #endregion

    #region Action Handlers

    private void OnCharacterSwitched(CharacterData character)
    {
        UpdateCharacterInfo();

        // TODO: temp effect
        if (characterNameText) characterNameText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f);
    }

    private void OnCharacterLevelUp(CharacterData character, int newLevel)
    {
        // VFX
        PlayLevelUpEffect();

        // Update UI
        UpdateCharacterInfo();
    }

    private void OnExperienceGained(CharacterData character, long expAmount)
    {
        // VFX
        PlayExpGainEffect();

        // Update UI
        UpdateExperience(character);
    }

    private void OnCurrencyChanged(long oldAmount, long newAmount)
    {
        if (newAmount > oldAmount)
        {
            // VFX
            PlayCoinGainEffect();
        }

        // Update UI
        AnimateNumberChange(coinText, oldAmount, newAmount, "");
        _lastCoinAmount = newAmount;
    }

    private void OnEnemySpawned(EnemyData enemy)
    {
        ResetEnemyUICache();
        UpdateEnemyInfo();
        ShowMessage($"遭遇敌人：{enemy.enemyName}！", 1.5f);
    }

    private void OnBattleStarted(EnemyData enemy)
    {
        UpdateEnemyInfo();
        ShowMessage($"开始战斗：{enemy.enemyName}！", 1.5f);
    }

    private void OnPlayerDied()
    {
        ShowMessage("角色阵亡！准备复活...");

        // VFX
        if (characterNameText != null)
        {
            var originalColor = characterNameText.color;
            characterNameText.DOColor(Color.red, 0.3f).OnComplete(() => { characterNameText.DOColor(originalColor, 0.5f); });
        }

        // Dying effect
        if (hpSlider != null)
        {
            var fillImage = hpSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.DOFade(0.3f, 0.5f).SetLoops(4, LoopType.Yoyo);
        }
    }

    private void OnPlayerRevived()
    {
        ShowMessage("角色复活！", 1.5f);

        // Revive VFX
        if (characterNameText != null)
        {
            var originalColor = characterNameText.color;
            characterNameText.DOColor(Color.green, 0.2f).OnComplete(() => { characterNameText.DOColor(originalColor, 0.3f); });
        }

        // Reset the HP
        if (hpSlider != null)
        {
            hpSlider.DOValue(1f, 0.5f).SetEase(Ease.OutQuart);

            var fillImage = hpSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.DOFade(1f, 0.3f); // 恢复不透明度
        }
    }

    private void OnBattleRestarted()
    {
        ShowMessage("战斗重新开始！", 1.5f);

        // 强制刷新所有UI
        UpdateAllUI();
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
            // Check HP
            var wasDamaged = enemy.currentHP < _lastEnemyHp && _lastEnemyHp > 0;

            UpdateEnemyHp(enemy.currentHP, enemy.maxHP);
        }
    }

    private void OnDamageDealt(float damage, bool isPlayerAttack)
    {
        if (isPlayerAttack)
        {
            // VFX
            PlayDamageNumberEffect(damage, Color.yellow, enemyHpSlider?.transform);
        }
        else
        {
            // VFX
            PlayDamageNumberEffect(damage, Color.red, hpSlider?.transform);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    ///     Force to refresh all UI
    /// </summary>
    public void RefreshUI()
    {
        UpdateAllUI();
    }

    /// <summary>
    ///     Message
    /// </summary>
    public void ShowMessage(string message, float duration = 2f)
    {
        // TODO: 实现消息提示UI
        Debug.Log($"[UI Message] {message}");
    }

    #endregion
}