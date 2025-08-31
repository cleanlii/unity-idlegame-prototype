using System;
using System.Collections;
using IdleGame.Analytics;
using IdleGame.Gameplay.Battle;
using UnityEngine;
using Random = UnityEngine.Random;

namespace IdleGame.Gameplay
{
    public enum RouteType
    {
        None,
        Battle, // 战斗线
        Economy, // 金币线
        Experience // 经验线
    }

    public class SpireSystem : MonoBehaviour
    {
        [Header("路线配置")]
        public RouteConfig battleRouteConfig;
        public RouteConfig economyRouteConfig;
        public RouteConfig experienceRouteConfig;

        [Header("敌人配置")]
        public EnemyConfig[] enemyConfigs; // 敌人配置列表

        [Header("运行时状态")]
        public RouteType currentRoute = RouteType.None;
        public EnemyData currentEnemy; // 当前待战敌人
        public float routeTimer; // 路线计时器

        [Header("调试信息")]
        public bool showDebugInfo = true;
        public float debugUpdateInterval = 1f;

        // 系统引用
        private GameManager gameManager;
        private CharacterSystem characterSystem;
        private IdleLogSystem logSystem;
        private BattleManager battleManager;

        // 协程引用
        private Coroutine routeCoroutine;
        private Coroutine battleCoroutine;
        private Coroutine debugCoroutine;

        // 事件
        public Action<RouteType> OnRouteChanged;
        public Action<EnemyData> OnEnemySpawned;
        public Action<bool, EnemyData> OnBattleCompleted; // victory, enemy

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        #region 系统初始化

        public void Initialize()
        {
            // 获取系统引用
            gameManager = GameManager.Instance;
            characterSystem = ServiceLocator.Get<CharacterSystem>();
            logSystem = ServiceLocator.Get<IdleLogSystem>();
            battleManager = ServiceLocator.Get<BattleManager>();

            currentRoute = RouteType.None;

            // 生成第一个敌人
            SpawnNextEnemy();

            LogMessage("[SpireSystem] 塔层系统初始化完成");
        }

        public void StartSpire()
        {
            // 根据当前路线启动对应协程
            SwitchToRoute(currentRoute);

            // 启动调试信息更新
            if (showDebugInfo)
                debugCoroutine = StartCoroutine(DebugInfoCoroutine());
        }

        #endregion

        #region 路线切换系统

        /// <summary>
        ///     切换到指定路线
        /// </summary>
        public void SwitchToRoute(RouteType newRoute)
        {
            if (currentRoute == newRoute) return;

            var oldRoute = currentRoute;
            currentRoute = newRoute;

            // 停止当前路线协程
            StopCurrentRoute();

            // 重置路线计时器
            routeTimer = 0f;

            // 如果从战斗线切换，停止战斗
            if (oldRoute == RouteType.Battle)
            {
                battleManager.RefreshBattle();
                LogMessage("战斗被中断，敌人重新生成");
            }

            // 启动新路线
            StartNewRoute();

            // 触发事件
            OnRouteChanged?.Invoke(newRoute);

            LogMessage($"路线切换：{GetRouteDisplayName(oldRoute)} → {GetRouteDisplayName(newRoute)}");
        }

        private void StopCurrentRoute()
        {
            if (routeCoroutine != null)
            {
                StopCoroutine(routeCoroutine);
                routeCoroutine = null;
            }

            if (battleCoroutine != null)
            {
                StopCoroutine(battleCoroutine);
                battleCoroutine = null;
            }
        }

        private void StartNewRoute()
        {
            switch (currentRoute)
            {
                case RouteType.Battle:
                    routeCoroutine = StartCoroutine(BattleRouteCoroutine());
                    break;
                case RouteType.Economy:
                    routeCoroutine = StartCoroutine(EconomyRouteCoroutine());
                    break;
                case RouteType.Experience:
                    routeCoroutine = StartCoroutine(ExperienceRouteCoroutine());
                    break;
            }
        }

        #endregion

        #region 路线协程实现

        /// <summary>
        ///     战斗线协程：只负责流程控制，具体战斗交给BattleManager
        /// </summary>
        private IEnumerator BattleRouteCoroutine()
        {
            LogMessage($"进入{battleRouteConfig.routeName}，准备战斗");

            while (currentRoute == RouteType.Battle)
            {
                // 检查是否有可用角色
                if (characterSystem.currentCharacter.IsNull)
                {
                    LogMessage("无可用角色，等待角色设置...");
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                // 如果没有敌人，生成新敌人
                if (currentEnemy == null)
                {
                    SpawnNextEnemy();
                    yield return new WaitForSeconds(0.5f); // 给敌人生成一点时间
                    continue;
                }

                // 检查BattleManager是否可以开始战斗
                if (battleManager)
                {
                    // 委托给BattleManager执行具体战斗
                    battleManager.StartBattle(currentEnemy);

                    // 等待战斗结束
                    while (battleManager.isBattleActive)
                    {
                        yield return null; // 等待BattleManager完成战斗
                    }

                    // 战斗结束后的处理在OnBattleEnded事件中进行
                }
                else
                {
                    // 如果无法开始战斗，等待一段时间后重试
                    yield return new WaitForSeconds(battleRouteConfig.intervalTime);
                }

                routeTimer += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        ///     金币线协程：按固定间隔获得金币
        /// </summary>
        private IEnumerator EconomyRouteCoroutine()
        {
            LogMessage($"进入{economyRouteConfig.routeName}，开始获得金币");

            while (currentRoute == RouteType.Economy)
            {
                routeTimer += Time.deltaTime;

                if (routeTimer >= economyRouteConfig.intervalTime)
                {
                    // 获得金币
                    var coinGain = economyRouteConfig.coinReward;
                    gameManager.AddCoins(coinGain);

                    LogMessage($"金币线收益：+{coinGain} 金币");

                    // 重置计时器
                    routeTimer = 0f;
                }

                yield return null;
            }
        }

        /// <summary>
        ///     经验线协程：按固定间隔获得经验
        /// </summary>
        private IEnumerator ExperienceRouteCoroutine()
        {
            LogMessage($"进入{experienceRouteConfig.routeName}，开始获得经验");

            while (currentRoute == RouteType.Experience)
            {
                routeTimer += Time.deltaTime;

                if (routeTimer >= experienceRouteConfig.intervalTime)
                {
                    // 获得经验
                    var expGain = experienceRouteConfig.expReward;
                    characterSystem.GainExperience(expGain);

                    LogMessage($"经验线收益：+{expGain} 经验");

                    // 重置计时器
                    routeTimer = 0f;
                }

                yield return null;
            }
        }

        #endregion

        #region 敌人管理

        /// <summary>
        ///     生成下一个敌人
        /// </summary>
        private void SpawnNextEnemy()
        {
            if (enemyConfigs == null || enemyConfigs.Length == 0) return;

            // 根据玩家等级选择合适的敌人配置
            var playerLevel = characterSystem.currentCharacter?.level ?? 1;
            var enemyConfig = SelectEnemyConfig(playerLevel);

            // 创建敌人实例
            currentEnemy = new EnemyData();
            ApplyEnemyConfig(currentEnemy, enemyConfig, playerLevel);

            // 通知BattleManager新敌人
            if (battleManager != null) battleManager.SetCurrentEnemy(currentEnemy);

            // 触发敌人生成事件
            OnEnemySpawned?.Invoke(currentEnemy);

            LogMessage($"生成敌人：{currentEnemy.enemyName} (Lv.{currentEnemy.recommendedLevel}, HP:{currentEnemy.maxHP:F0})");
        }

        /// <summary>
        ///     根据玩家等级选择敌人配置
        /// </summary>
        private EnemyConfig SelectEnemyConfig(int playerLevel)
        {
            // 简单的选择逻辑：随机选择一个配置
            var randomIndex = Random.Range(0, enemyConfigs.Length);
            return enemyConfigs[randomIndex];
        }

        /// <summary>
        ///     应用敌人配置到敌人数据
        /// </summary>
        private void ApplyEnemyConfig(EnemyData enemy, EnemyConfig config, int playerLevel)
        {
            enemy.enemyID = config.enemyID;
            enemy.enemyName = config.enemyName;
            enemy.enemyType = config.enemyType;

            // 根据玩家等级调整敌人等级
            var enemyLevel = Mathf.Max(1, playerLevel + Random.Range(-1, 2));
            enemy.GenerateStatsForLevel(enemyLevel);

            // 应用配置的特殊设置
            if (config.hasCustomStats)
            {
                enemy.maxHP = config.baseHP * (1 + enemyLevel * 0.1f);
                enemy.attackPower = config.baseAttack * (1 + enemyLevel * 0.08f);
                enemy.defense = config.baseDefense * (1 + enemyLevel * 0.05f);
                enemy.currentHP = enemy.maxHP;
            }
        }

        #endregion

        #region 公开方法

        /// <summary>
        ///     获取当前路线配置
        /// </summary>
        public RouteConfig GetCurrentRouteConfig()
        {
            return currentRoute switch
            {
                RouteType.Battle => battleRouteConfig,
                RouteType.Economy => economyRouteConfig,
                RouteType.Experience => experienceRouteConfig,
                _ => null
            };
        }

        /// <summary>
        ///     获取路线显示名称
        /// </summary>
        public string GetRouteDisplayName(RouteType route)
        {
            return route switch
            {
                RouteType.Battle => "战斗线",
                RouteType.Economy => "金币线",
                RouteType.Experience => "经验线",
                _ => "未知路线"
            };
        }

        /// <summary>
        ///     获取当前路线进度信息
        /// </summary>
        public string GetRouteProgressInfo()
        {
            var config = GetCurrentRouteConfig();
            if (config == null) return "无配置";

            var progress = config.intervalTime > 0 ? routeTimer / config.intervalTime * 100f : 100f;
            return $"{GetRouteDisplayName(currentRoute)} - 进度: {progress:F1}%";
        }

        /// <summary>
        ///     获取战斗状态信息
        /// </summary>
        public string GetBattleStatusInfo()
        {
            if (currentRoute != RouteType.Battle)
                return "非战斗线";

            if (currentEnemy == null)
                return "无敌人";

            if (!battleManager.isBattleActive)
                return $"待战敌人: {currentEnemy.enemyName} (Lv.{currentEnemy.recommendedLevel})";

            var enemyHpPercent = currentEnemy.currentHP / currentEnemy.maxHP * 100f;
            return $"战斗中: {currentEnemy.enemyName} HP: {enemyHpPercent:F1}%";
        }

        #endregion

        #region 调试和测试

        private IEnumerator DebugInfoCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(debugUpdateInterval);

                if (showDebugInfo) Debug.Log($"[SpireSystem Debug] {GetRouteProgressInfo()} | {GetBattleStatusInfo()}");
            }
        }

        [ContextMenu("强制生成新敌人")]
        public void TestSpawnEnemy()
        {
            SpawnNextEnemy();
        }

        [ContextMenu("切换到战斗线")]
        public void TestSwitchToBattle()
        {
            SwitchToRoute(RouteType.Battle);
        }

        [ContextMenu("切换到金币线")]
        public void TestSwitchToEconomy()
        {
            SwitchToRoute(RouteType.Economy);
        }

        [ContextMenu("切换到经验线")]
        public void TestSwitchToExperience()
        {
            SwitchToRoute(RouteType.Experience);
        }

        #endregion

        private void LogMessage(string message)
        {
            logSystem?.LogMessage(message);
            Debug.Log($"[SpireSystem] {message}");
        }
    }
}