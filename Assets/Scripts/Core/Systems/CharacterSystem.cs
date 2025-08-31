using System;
using System.Collections.Generic;
using System.Linq;
using IdleGame.Analytics;
using IdleGame.Character;
using UnityEngine;

namespace IdleGame.Gameplay
{
    /// <summary>
    ///     Manages character switching, upgrading, status update, and more
    /// </summary>
    public class CharacterSystem : MonoBehaviour
    {
        [Header("Library Settings")]
        public CharacterDatabase characterDb; // 默认角色配置

        [Header("Current Status")]
        public CharacterData currentCharacter; // 当前使用的角色

        private readonly Dictionary<string, CharacterData> _ownedCharacters = new();
        private IdleLogSystem _logSystem;

        // 事件
        public Action<CharacterData> OnCharacterSwitched;
        public Action<CharacterData, int> OnCharacterLevelUp;
        public Action<CharacterData, long> OnExperienceGained;
        public Action<CharacterData> OnCharacterDied;

        private void Start()
        {
            _logSystem = ServiceLocator.Get<IdleLogSystem>();
        }

        #region Initialization

        /// <summary>
        ///     初始化角色系统
        /// </summary>
        public void Initialize()
        {
            // // 加载玩家已拥有的角色数据
            // LoadOwnedCharacters();
            //
            // 如果没有角色, 创建默认角色
            if (_ownedCharacters.Count == 0) CreateDefaultCharacter();

            // 设置当前角色
            if (currentCharacter.IsNull)
            {
                var defaultCharacter = new CharacterData(characterDb.defaultCharacter);
                SetCurrentCharacter(defaultCharacter);
            }

            // LogMessage($"角色系统初始化完成, 当前角色: {currentCharacter?.GetCharacterInfo()}");
        }

        /// <summary>
        ///     创建默认角色
        /// </summary>
        private void CreateDefaultCharacter()
        {
            if (characterDb != null)
            {
                var defaultCharacter = new CharacterData(characterDb.defaultCharacter);
                AddCharacter(defaultCharacter);
                // LogMessage($"创建默认角色: {defaultCharacter.GetCharacterInfo()}");
            }
        }

        /// <summary>
        ///     加载已拥有的角色 (从PlayerData或存档)
        /// </summary>
        public void LoadOwnedCharacters(PlayerData playerData)
        {
            // 清空现有角色数据
            _ownedCharacters.Clear();

            // 从PlayerData加载角色
            foreach (var characterSave in playerData.ownedCharacters)
            {
                // 根据configID找到对应的配置
                var config = FindCharacterConfig(characterSave.configID);
                if (config != null)
                {
                    // 使用存档数据创建CharacterData
                    var characterData = new CharacterData(config, characterSave.level, characterSave.totalExperience);

                    // 恢复战斗统计数据
                    characterData.totalBattles = characterSave.totalBattles;
                    characterData.victoriesCount = characterSave.victoriesCount;
                    characterData.totalDamageDealt = characterSave.totalDamageDealt;
                    characterData.totalDamageTaken = characterSave.totalDamageTaken;

                    _ownedCharacters[characterSave.configID] = characterData;
                    // LogMessage($"加载角色: {characterData.GetCharacterInfo()}");
                }
                else
                    Debug.LogError($"警告: 未找到角色配置 {characterSave.configID}");
            }

            // 设置当前角色
            if (!string.IsNullOrEmpty(playerData.currentCharacterID))
            {
                if (_ownedCharacters.ContainsKey(playerData.currentCharacterID))
                    SetCurrentCharacter(_ownedCharacters[playerData.currentCharacterID]);
                else
                {
                    // Debug.LogWarning($"警告: 当前角色ID {playerData.currentCharacterID} 未找到, 使用默认角色");
                    SetDefaultCurrentCharacter();
                }
            }
            else
                SetDefaultCurrentCharacter();
        }

        /// <summary>
        ///     根据ID查找角色配置
        /// </summary>
        private CharacterConfig FindCharacterConfig(string configID)
        {
            // 尝试从CharacterDatabase查找
            var database = FindObjectOfType<CharacterDatabase>();
            if (database != null) return database.GetCharacterConfig(configID);

            return null;
        }

        /// <summary>
        ///     设置默认当前角色
        /// </summary>
        private void SetDefaultCurrentCharacter()
        {
            var firstCharacter = _ownedCharacters.Values.FirstOrDefault();
            if (firstCharacter != null) SetCurrentCharacter(firstCharacter);
        }

        #endregion

        #region Character Management

        /// <summary>
        ///     添加新角色到收藏
        /// </summary>
        public void AddCharacter(CharacterData character)
        {
            if (!character?.config) return;

            var characterID = character.config.characterID;

            if (_ownedCharacters.TryAdd(characterID, character))
                LogMessage($"New character obtained: {character.GetCharacterInfo()}");
            else
                LogMessage($"Already have the character: {character.GetCharacterInfo()}");
        }

        /// <summary>
        ///     通过配置添加角色
        /// </summary>
        public void AddCharacterByConfig(CharacterConfig config)
        {
            if (config == null) return;

            var newCharacter = new CharacterData(config);
            AddCharacter(newCharacter);
        }

        /// <summary>
        ///     切换当前角色
        /// </summary>
        public bool SwitchCharacter(string characterID)
        {
            if (_ownedCharacters.TryGetValue(characterID, out var character))
            {
                SetCurrentCharacter(character);
                return true;
            }

            // Debug.LogError($"未找到角色: {characterID}");
            return false;
        }

        /// <summary>
        ///     设置当前角色
        /// </summary>
        private void SetCurrentCharacter(CharacterData character)
        {
            if (character == null) return;

            currentCharacter = character;

            // 刷新角色的运行时状态
            character.RefreshRuntimeState();

            OnCharacterSwitched?.Invoke(character);
            LogMessage($"Switch To Character: {character.GetCharacterInfo()}, HP: {character.currentHP:F0}/{character.GetMaxHP():F0}");
        }

        /// <summary>
        ///     获取所有拥有的角色
        /// </summary>
        public List<CharacterData> GetOwnedCharacters()
        {
            return _ownedCharacters.Values.ToList();
        }

        /// <summary>
        ///     根据ID获取角色
        /// </summary>
        public CharacterData GetCharacter(string characterID)
        {
            _ownedCharacters.TryGetValue(characterID, out var character);
            return character;
        }

        #endregion

        #region Upgrading

        /// <summary>
        ///     给当前角色添加经验
        /// </summary>
        public void GainExperience(long expAmount)
        {
            if (currentCharacter.IsNull || expAmount <= 0) return;

            var oldLevel = currentCharacter.level;
            var leveledUp = currentCharacter.GainExperience(expAmount);

            OnExperienceGained?.Invoke(currentCharacter, expAmount);
            LogMessage($"{IdleGameConst.LOG_PLAYER_NAME} gained EXP: {expAmount}, Current Lv.: {currentCharacter.level}");

            if (leveledUp)
            {
                var newLevel = currentCharacter.level;
                OnCharacterLevelUp?.Invoke(currentCharacter, newLevel);
                LogMessage($"{IdleGameConst.LOG_PLAYER_NAME} Upgraded! {currentCharacter.config.characterName} {oldLevel} → {newLevel}");
                LogMessage(
                    $"Growth: HP {currentCharacter.GetMaxHP():F0}, ATK {currentCharacter.GetAttack():F0}, DEF {currentCharacter.GetDefense():F0}");
            }
        }

        /// <summary>
        ///     购买经验（使用金币）
        /// </summary>
        public void BuyExperience(long coinCost, long expAmount)
        {
            var gameManager = GameManager.Instance;
            if (gameManager?.playerData == null) return;

            if (gameManager.playerData.SpendCoins(coinCost))
            {
                GainExperience(expAmount);
                LogMessage($"{IdleGameConst.LOG_PLAYER_NAME} spent {coinCost} coins to purchase {expAmount} EXP");
            }
            else
                LogMessage($"Not enough coins, requires {coinCost} coins");
        }

        #endregion

        #region Battle

        /// <summary>
        ///     当前角色受到伤害
        /// </summary>
        public float TakeDamage(float damage)
        {
            if (currentCharacter.IsNull) return 0f;

            var actualDamage = currentCharacter.TakeDamage(damage);
            // LogMessage($"受到伤害: {actualDamage:F1}, 剩余HP: {currentCharacter.currentHP:F0}/{currentCharacter.GetMaxHP():F0}");

            if (currentCharacter.IsDead())
            {
                OnCharacterDied?.Invoke(currentCharacter);
                // LogMessage($"{currentCharacter.config.characterName} 被击败!");
            }

            return actualDamage;
        }

        /// <summary>
        ///     当前角色攻击
        /// </summary>
        public float DealDamage()
        {
            if (currentCharacter.IsNull) return 0f;

            var damage = currentCharacter.GetAttackDamage();
            // LogMessage($"造成伤害: {damage:F1}");
            return damage;
        }

        /// <summary>
        ///     恢复当前角色HP
        /// </summary>
        public void RestoreHP()
        {
            if (currentCharacter.IsNull) return;

            currentCharacter.RestoreFullHP();
            // LogMessage($"{currentCharacter.config.characterName} HP完全恢复: {currentCharacter.currentHP:F0}");
        }

        /// <summary>
        ///     记录战斗结果
        /// </summary>
        public void RecordBattleResult(bool victory, long damageDealt)
        {
            if (currentCharacter.IsNull) return;

            currentCharacter.RecordBattleResult(victory, damageDealt);

            var result = victory ? "胜利" : "失败";
            // LogMessage($"战斗{result}!造成伤害: {damageDealt}, 胜率: {currentCharacter.GetWinRate():P1}");
        }

        #endregion

        #region UI Support

        /// <summary>
        ///     检查是否可以升级
        /// </summary>
        public bool CanLevelUp()
        {
            if (currentCharacter.IsNull) return false;

            var expNeeded = currentCharacter.GetExpToNextLevel();
            return expNeeded > 0 && currentCharacter.totalExperience >= expNeeded;
        }

        #endregion

        #region Utility Methods

        private void LogMessage(string message)
        {
            _logSystem?.LogMessage(message);
        }

        #endregion
    }
}