using System;
using System.Collections;
using IdleGame.Analytics;
using IdleGame.Character;
using UnityEngine;

namespace IdleGame.Gameplay.Battle
{
    public class BattleManager : MonoBehaviour
    {
        [Header("Revive Settings")]
        [SerializeField] private float reviveDelay = 2f; // 复活延迟时间
        [SerializeField] private bool autoReviveEnabled = true; // 是否启用自动复活
        [SerializeField] private float battleRestartDelay = 1f; // 战斗重启延迟
        [SerializeField] private float baseAttackInterval = 1f; // 基础攻击间隔
        [SerializeField] private float battleStartDelay = 0.5f; // 战斗开始延迟
        [SerializeField] private float victoryDelay = 1f; // 胜利后延迟
        [SerializeField] private float defeatDelay = 2f; // 失败后延迟

        [Header("Battle Status")]
        public bool isBattleActive; // 是否正在战斗
        public EnemyData currentEnemy; // 当前敌人
        public float battleTimer; // 战斗计时器

        [Header("Statistics")]
        public int consecutiveWins; // 连续胜利次数
        public int totalBattlesThisSession; // 本次会话战斗次数
        public float totalBattleTime; // 总战斗时间

        // 系统引用
        private CharacterSystem _characterSystem;
        private IdleLogSystem _logSystem;
        private GameManager _gameManager;

        // 协程引用
        private Coroutine _battleCoroutine;

        // 事件
        public Action<EnemyData> OnBattleStarted;
        public Action<bool, EnemyData, float> OnBattleEnded; // victory, enemy, duration
        public Action<float, bool> OnDamageDealt; // damage, isPlayerAttack
        public Action<EnemyData> onEnemyHpChanged;
        public Action OnPlayerDied;
        public Action OnPlayerRevived;
        public Action OnBattleRestarted;

        private void Start()
        {
            InitializeBattleManager();
        }

        private void OnDestroy()
        {
            StopAllBattleCoroutines();
        }

        #region Initialization

        private void InitializeBattleManager()
        {
            // 获取系统引用
            _gameManager = GameManager.Instance;
            _characterSystem = ServiceLocator.Get<CharacterSystem>();
            _logSystem = ServiceLocator.Get<IdleLogSystem>();

            // 订阅角色系统事件
            if (_characterSystem != null)
            {
                _characterSystem.OnCharacterDied += OnCharacterDied;
                _characterSystem.OnCharacterSwitched += OnCharacterSwitched;
            }

            LogMessage("[BattleManager] 战斗管理器初始化完成");
        }

        #endregion

        #region Battle Processing

        /// <summary>
        ///     开始与指定敌人的战斗
        /// </summary>
        public void StartBattle(EnemyData enemy)
        {
            if (isBattleActive || enemy == null)
            {
                LogMessage("无法开始战斗：已在战斗中或敌人为空");
                return;
            }

            // 检查角色状态
            if (_characterSystem.currentCharacter.IsNull)
            {
                LogMessage("无法开始战斗：没有可用角色");
                return;
            }

            if (_characterSystem.currentCharacter.IsDead())
            {
                LogMessage("角色已阵亡，正在复活...");
                RevivePlayer();
                return;
            }

            currentEnemy = enemy;
            currentEnemy.ResetHP(); // 确保敌人满血开始
            isBattleActive = true;
            battleTimer = 0f;

            // 启动战斗协程
            _battleCoroutine = StartCoroutine(BattleCoroutine());

            // 触发战斗开始事件
            OnBattleStarted?.Invoke(currentEnemy);

            LogMessage($"开始战斗！对手：{currentEnemy.enemyName} (Lv.{currentEnemy.recommendedLevel})");
        }

        /// <summary>
        ///     停止当前战斗
        /// </summary>
        public void StopBattle()
        {
            if (!isBattleActive) return;

            isBattleActive = false;

            if (_battleCoroutine != null)
            {
                StopCoroutine(_battleCoroutine);
                _battleCoroutine = null;
            }

            LogMessage("战斗被中断");
        }

        /// <summary>
        ///     强制结束战斗
        /// </summary>
        public void ForceBattleEnd()
        {
            if (!isBattleActive) return;

            StopBattle();
            currentEnemy = null;
            LogMessage("战斗被强制结束");
        }

        #endregion

        #region Main Loop

        /// <summary>
        ///     战斗主协程
        /// </summary>
        private IEnumerator BattleCoroutine()
        {
            var character = _characterSystem.currentCharacter;
            var battleStartTime = Time.time;

            // 战斗开始延迟
            yield return new WaitForSeconds(battleStartDelay);

            LogMessage($"战斗正式开始！{character.config.characterName} VS {currentEnemy.enemyName}");

            while (isBattleActive && currentEnemy != null &&
                   !currentEnemy.IsDead() && !character.IsDead())
            {
                battleTimer += Time.deltaTime;

                // 玩家攻击回合
                yield return StartCoroutine(PlayerAttackPhase(character));

                // 检查敌人是否死亡
                if (currentEnemy.IsDead())
                {
                    yield return new WaitForSeconds(victoryDelay);
                    OnBattleVictory(battleStartTime);
                    break;
                }

                // 敌人攻击回合
                yield return StartCoroutine(EnemyAttackPhase(character));

                // 检查玩家是否死亡
                if (character.IsDead())
                {
                    yield return new WaitForSeconds(defeatDelay);
                    OnBattleDefeat(battleStartTime);
                    break;
                }
            }

            isBattleActive = false;
        }

        /// <summary>
        ///     玩家攻击阶段
        /// </summary>
        private IEnumerator PlayerAttackPhase(CharacterData character)
        {
            var playerDamage = character.GetAttackDamage();
            var actualDamage = currentEnemy.TakeDamage(playerDamage);

            // 记录伤害
            character.totalDamageDealt += (long)actualDamage;

            // 触发伤害事件
            OnDamageDealt?.Invoke(actualDamage, true);
            onEnemyHpChanged?.Invoke(currentEnemy);

            LogMessage($"{character.config.characterName} 造成伤害: {actualDamage:F1}");
            _logSystem?.LogDamage(actualDamage, true);

            // 等待玩家攻击间隔
            var attackInterval = baseAttackInterval / character.GetAttackSpeed();
            yield return new WaitForSeconds(attackInterval);
        }

        /// <summary>
        ///     敌人攻击阶段
        /// </summary>
        private IEnumerator EnemyAttackPhase(CharacterData character)
        {
            var enemyDamage = currentEnemy.GetAttackDamage();
            var damageToPlayer = _characterSystem.TakeDamage(enemyDamage);

            // 触发伤害事件
            OnDamageDealt?.Invoke(damageToPlayer, false);

            LogMessage($"{currentEnemy.enemyName} 造成伤害: {damageToPlayer:F1}");
            _logSystem?.LogDamage(damageToPlayer, false);

            // 等待敌人攻击间隔
            var attackInterval = baseAttackInterval / currentEnemy.attackSpeed;
            yield return new WaitForSeconds(attackInterval);
        }

        #endregion

        #region Result Handling

        /// <summary>
        ///     战斗胜利处理
        /// </summary>
        private void OnBattleVictory(float battleStartTime)
        {
            var battleDuration = Time.time - battleStartTime;
            var expReward = currentEnemy.GetExpReward();
            var coinReward = currentEnemy.GetCoinReward();

            // 更新统计
            consecutiveWins++;
            totalBattlesThisSession++;
            totalBattleTime += battleDuration;

            // 给予奖励
            _characterSystem.GainExperience(expReward);
            _gameManager.AddCoins(coinReward);

            // 记录角色战斗统计
            _characterSystem.RecordBattleResult(true, (long)currentEnemy.maxHP);

            // 角色恢复满血
            _characterSystem.RestoreHP();

            LogMessage($"战斗胜利！用时 {battleDuration:F1}秒");
            LogMessage($"获得奖励 - 经验: {expReward}, 金币: {coinReward}");
            LogMessage($"连胜: {consecutiveWins}次");

            // 触发战斗结束事件
            OnBattleEnded?.Invoke(true, currentEnemy, battleDuration);

            // 清理当前敌人
            currentEnemy = null;
        }

        /// <summary>
        ///     战斗失败处理 - 修改为刷新战斗
        /// </summary>
        private void OnBattleDefeat(float battleStartTime)
        {
            var battleDuration = Time.time - battleStartTime;

            // 重置连胜
            consecutiveWins = 0;
            totalBattlesThisSession++;
            totalBattleTime += battleDuration;

            // 记录角色战斗统计（无奖励）
            _characterSystem.RecordBattleResult(false, 0);

            LogMessage($"战斗失败！用时 {battleDuration:F1}秒");

            // 触发战斗结束事件
            OnBattleEnded?.Invoke(false, currentEnemy, battleDuration);

            // 启动战斗刷新流程
            if (autoReviveEnabled)
                StartCoroutine(BattleRefreshCoroutine());
            else
            {
                // 如果不自动复活，敌人恢复满血等待手动复活
                currentEnemy.ResetHP();
                onEnemyHpChanged?.Invoke(currentEnemy);
            }
        }

        #endregion

        #region Refresh Battle

        /// <summary>
        ///     战斗刷新协程 - 玩家死亡后的完整刷新流程
        /// </summary>
        private IEnumerator BattleRefreshCoroutine()
        {
            LogMessage("开始战斗刷新流程...");

            // 1. 复活玩家
            yield return new WaitForSeconds(reviveDelay);
            RevivePlayer();

            // 2. 敌人也恢复满血
            if (currentEnemy != null)
            {
                currentEnemy.ResetHP();
                onEnemyHpChanged?.Invoke(currentEnemy);
                LogMessage($"{currentEnemy.enemyName} 血量已重置");
            }

            // 3. 等待一段时间后重新开始战斗
            yield return new WaitForSeconds(battleRestartDelay);

            // 4. 重新开始战斗
            if (CanStartBattle())
            {
                LogMessage("战斗重新开始！");
                OnBattleRestarted?.Invoke();

                // 重新启动战斗
                isBattleActive = true;
                _battleCoroutine = StartCoroutine(BattleCoroutine());
            }
        }

        /// <summary>
        ///     手动刷新战斗 - 外部调用接口
        /// </summary>
        public void ManualRefreshBattle()
        {
            if (!isBattleActive) return;

            LogMessage("手动刷新战斗");

            // 停止当前战斗
            StopBattle();

            // 启动刷新流程
            StartCoroutine(BattleRefreshCoroutine());
        }

        #endregion

        #region Player Handling

        /// <summary>
        ///     复活玩家
        /// </summary>
        private void RevivePlayer()
        {
            if (!_characterSystem.currentCharacter.IsNull)
            {
                _characterSystem.RestoreHP();
                OnPlayerRevived?.Invoke();
                LogMessage($"{_characterSystem.currentCharacter.config.characterName} 已复活！血量已恢复");
            }
        }

        /// <summary>
        ///     角色死亡事件处理
        /// </summary>
        private void OnCharacterDied(CharacterData character)
        {
            OnPlayerDied?.Invoke();
            LogMessage($"{character.config.characterName} 阵亡！");

            // 不立即停止战斗，让OnBattleDefeat处理刷新逻辑
        }

        /// <summary>
        ///     角色切换事件处理 - 修改为保持战斗状态
        /// </summary>
        private void OnCharacterSwitched(CharacterData newCharacter)
        {
            LogMessage($"角色切换至: {newCharacter.config.characterName}");

            // 角色切换时，如果在战斗中，刷新战斗而不是停止
            if (isBattleActive)
            {
                LogMessage("角色切换，刷新当前战斗");
                StartCoroutine(BattleRefreshCoroutine());
            }
        }

        #endregion

        #region Public API

        /// <summary>
        ///     检查是否可以开始战斗
        /// </summary>
        public bool CanStartBattle()
        {
            return _gameManager.spireSystem.currentRoute == RouteType.Battle && !isBattleActive &&
                   !_characterSystem.currentCharacter.IsNull &&
                   !_characterSystem.currentCharacter.IsDead() &&
                   currentEnemy != null;
        }

        /// <summary>
        ///     立即刷新战斗 - 外部调用
        /// </summary>
        public void RefreshBattle()
        {
            if (currentEnemy == null) return;

            LogMessage("外部请求刷新战斗");

            // 停止当前战斗
            if (isBattleActive) StopBattle();

            // 启动刷新流程
            StartCoroutine(QuickBattleRefreshCoroutine());
        }

        /// <summary>
        ///     快速战斗刷新
        /// </summary>
        private IEnumerator QuickBattleRefreshCoroutine()
        {
            // 立即复活玩家
            RevivePlayer();

            // 敌人恢复满血
            if (currentEnemy != null)
            {
                currentEnemy.ResetHP();
                onEnemyHpChanged?.Invoke(currentEnemy);
            }

            yield return new WaitForSeconds(0.5f); // 短暂延迟

            // 重新开始战斗
            if (currentEnemy != null && CanStartBattle())
            {
                LogMessage("快速重启战斗！");
                OnBattleRestarted?.Invoke();
                StartBattle(currentEnemy);
            }
        }

        /// <summary>
        ///     设置自动复活开关
        /// </summary>
        public void SetAutoRevive(bool enabled)
        {
            autoReviveEnabled = enabled;
            LogMessage($"自动复活: {(enabled ? "开启" : "关闭")}");
        }

        /// <summary>
        ///     获取战斗刷新设置信息
        /// </summary>
        public string GetBattleRefreshInfo()
        {
            return $"自动复活: {(autoReviveEnabled ? "开启" : "关闭")} | " +
                   $"复活延迟: {reviveDelay}s | " +
                   $"重启延迟: {battleRestartDelay}s";
        }

        /// <summary>
        ///     获取当前战斗状态信息
        /// </summary>
        public string GetBattleStatusInfo()
        {
            if (!isBattleActive)
                return currentEnemy != null ? $"待战: {currentEnemy.enemyName}" : "无敌人";

            if (currentEnemy == null)
                return "战斗中 (无敌人数据)";

            var enemyHpPercent = currentEnemy.currentHP / currentEnemy.maxHP * 100f;
            return $"战斗中: {currentEnemy.enemyName} HP: {enemyHpPercent:F1}% | 用时: {battleTimer:F1}s";
        }

        /// <summary>
        ///     获取战斗统计信息
        /// </summary>
        public string GetBattleStatsInfo()
        {
            return $"连胜: {consecutiveWins} | 本次战斗: {totalBattlesThisSession} | 总用时: {totalBattleTime:F1}s";
        }

        /// <summary>
        ///     获取当前敌人信息
        /// </summary>
        public EnemyData GetCurrentEnemy()
        {
            return currentEnemy;
        }

        /// <summary>
        ///     设置当前敌人 (由SpireSystem调用)
        /// </summary>
        public void SetCurrentEnemy(EnemyData enemy)
        {
            if (isBattleActive)
            {
                LogMessage("战斗进行中，无法更换敌人");
                return;
            }

            currentEnemy = enemy;
            LogMessage($"设置新敌人: {enemy?.enemyName ?? "None"}");
        }

        /// <summary>
        ///     获取敌人血量百分比
        /// </summary>
        public float GetEnemyHPPercent()
        {
            return currentEnemy != null && currentEnemy.maxHP > 0
                ? currentEnemy.currentHP / currentEnemy.maxHP
                : 0f;
        }

        #endregion

        #region Utility Methods

        private void StopAllBattleCoroutines()
        {
            if (_battleCoroutine != null)
            {
                StopCoroutine(_battleCoroutine);
                _battleCoroutine = null;
            }
        }

        private void LogMessage(string message)
        {
            _logSystem?.LogMessage(message);
            // Debug.Log($"[BattleManager] {message}");
        }

        #endregion
    }
}