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

        private readonly Dictionary<string, CharacterData> ownedCharacters = new();
        private IdleLogSystem logSystem;

        // 事件
        public Action<CharacterData> OnCharacterSwitched;
        public Action<CharacterData, int> OnCharacterLevelUp;
        public Action<CharacterData, long> OnExperienceGained;
        public Action<CharacterData> OnCharacterDied;

        private void Start()
        {
            logSystem = ServiceLocator.Get<IdleLogSystem>();
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
            // 如果没有角色，创建默认角色
            if (ownedCharacters.Count == 0) CreateDefaultCharacter();

            // 设置当前角色
            if (currentCharacter.IsNull)
            {
                var defaultCharacter = new CharacterData(characterDb.defaultCharacter);
                SetCurrentCharacter(defaultCharacter);
            }

            LogMessage($"角色系统初始化完成，当前角色：{currentCharacter?.GetCharacterInfo()}");
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
                LogMessage($"创建默认角色：{defaultCharacter.GetCharacterInfo()}");
            }
        }

        /// <summary>
        ///     加载已拥有的角色 (从PlayerData或存档)
        /// </summary>
        public void LoadOwnedCharacters(PlayerData playerData)
        {
            // 清空现有角色数据
            ownedCharacters.Clear();

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

                    ownedCharacters[characterSave.configID] = characterData;
                    LogMessage($"加载角色：{characterData.GetCharacterInfo()}");
                }
                else
                    LogMessage($"警告：未找到角色配置 {characterSave.configID}");
            }

            // 设置当前角色
            if (!string.IsNullOrEmpty(playerData.currentCharacterID))
            {
                if (ownedCharacters.ContainsKey(playerData.currentCharacterID))
                    SetCurrentCharacter(ownedCharacters[playerData.currentCharacterID]);
                else
                {
                    LogMessage($"警告：当前角色ID {playerData.currentCharacterID} 未找到，使用默认角色");
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
            var firstCharacter = ownedCharacters.Values.FirstOrDefault();
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

            if (ownedCharacters.TryAdd(characterID, character))
                LogMessage($"获得新角色：{character.GetCharacterInfo()}");
            else
                LogMessage($"角色已拥有：{character.config.characterName}");
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
            if (ownedCharacters.TryGetValue(characterID, out var character))
            {
                SetCurrentCharacter(character);
                return true;
            }

            LogMessage($"未找到角色：{characterID}");
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
            LogMessage($"切换角色：{character.GetCharacterInfo()}，HP：{character.currentHP:F0}/{character.GetMaxHP():F0}");
        }

        /// <summary>
        ///     获取所有拥有的角色
        /// </summary>
        public List<CharacterData> GetOwnedCharacters()
        {
            return ownedCharacters.Values.ToList();
        }

        /// <summary>
        ///     根据ID获取角色
        /// </summary>
        public CharacterData GetCharacter(string characterID)
        {
            ownedCharacters.TryGetValue(characterID, out var character);
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
            LogMessage($"获得经验：{expAmount}，当前等级：{currentCharacter.level}");

            if (leveledUp)
            {
                var newLevel = currentCharacter.level;
                OnCharacterLevelUp?.Invoke(currentCharacter, newLevel);
                LogMessage($"角色升级！{currentCharacter.config.characterName} {oldLevel} → {newLevel}");
                LogMessage(
                    $"属性提升：HP {currentCharacter.GetMaxHP():F0}, ATK {currentCharacter.GetAttack():F0}, DEF {currentCharacter.GetDefense():F0}");
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
                LogMessage($"花费 {coinCost} 金币购买 {expAmount} 经验");
            }
            else
                LogMessage($"金币不足，需要 {coinCost} 金币");
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
            LogMessage($"受到伤害：{actualDamage:F1}，剩余HP：{currentCharacter.currentHP:F0}/{currentCharacter.GetMaxHP():F0}");

            if (currentCharacter.IsDead())
            {
                OnCharacterDied?.Invoke(currentCharacter);
                LogMessage($"{currentCharacter.config.characterName} 被击败！");
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
            LogMessage($"造成伤害：{damage:F1}");
            return damage;
        }

        /// <summary>
        ///     恢复当前角色HP
        /// </summary>
        public void RestoreHP()
        {
            if (currentCharacter.IsNull) return;

            currentCharacter.RestoreFullHP();
            LogMessage($"{currentCharacter.config.characterName} HP完全恢复：{currentCharacter.currentHP:F0}");
        }

        /// <summary>
        ///     记录战斗结果
        /// </summary>
        public void RecordBattleResult(bool victory, long damageDealt)
        {
            if (currentCharacter.IsNull) return;

            currentCharacter.RecordBattleResult(victory, damageDealt);

            var result = victory ? "胜利" : "失败";
            LogMessage($"战斗{result}！造成伤害：{damageDealt}，胜率：{currentCharacter.GetWinRate():P1}");
        }

        #endregion

        #region UI Support

        /// <summary>
        ///     获取当前角色信息 (UI显示用)
        /// </summary>
        public string GetCurrentCharacterDisplayInfo()
        {
            if (currentCharacter.IsNull) return "无角色";

            return $"{currentCharacter.config.characterName}\n" +
                   $"等级：{currentCharacter.level}\n" +
                   $"HP：{currentCharacter.currentHP:F0}/{currentCharacter.GetMaxHP():F0}\n" +
                   $"经验：{currentCharacter.GetExpProgress():P1}\n" +
                   $"战力：{currentCharacter.GetPowerScore():F0}";
        }

        /// <summary>
        ///     获取角色属性详情
        /// </summary>
        public string GetCharacterStats()
        {
            if (currentCharacter.IsNull) return "";

            return $"攻击力：{currentCharacter.GetAttack():F0}\n" +
                   $"防御力：{currentCharacter.GetDefense():F0}\n" +
                   $"暴击率：{currentCharacter.GetCriticalRate():P1}\n" +
                   $"攻击速度：{currentCharacter.GetAttackSpeed():F1}";
        }

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
            logSystem?.LogMessage(message);
        }

        #endregion
    }
}