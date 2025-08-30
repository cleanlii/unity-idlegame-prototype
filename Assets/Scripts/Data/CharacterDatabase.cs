using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace IdleGame.Character
{
    /// <summary>
    ///     角色配置数据库 - 管理所有角色配置的ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "IdleGame/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        [Header("角色配置列表")]
        public List<CharacterConfig> allCharacters = new();

        [Header("默认角色")]
        public CharacterConfig defaultCharacter;

        [Header("抽卡权重设置")]
        public GachaSettings gachaSettings = new();

        private Dictionary<string, CharacterConfig> characterLookup;

        private void OnEnable()
        {
            RefreshLookup();
        }

        /// <summary>
        ///     刷新查找字典
        /// </summary>
        private void RefreshLookup()
        {
            characterLookup = new Dictionary<string, CharacterConfig>();
            foreach (var character in allCharacters)
            {
                if (character != null && !string.IsNullOrEmpty(character.characterID)) characterLookup[character.characterID] = character;
            }
        }

        /// <summary>
        ///     根据ID获取角色配置
        /// </summary>
        public CharacterConfig GetCharacterConfig(string characterID)
        {
            if (characterLookup == null) RefreshLookup();
            characterLookup.TryGetValue(characterID, out var config);
            return config;
        }

        /// <summary>
        ///     获取指定稀有度的角色列表
        /// </summary>
        public List<CharacterConfig> GetCharactersByRarity(CharacterRarity rarity)
        {
            return allCharacters.Where(c => c != null && c.rarity == rarity).ToList();
        }

        /// <summary>
        ///     获取默认角色配置
        /// </summary>
        public CharacterConfig GetDefaultCharacter()
        {
            return defaultCharacter != null ? defaultCharacter : allCharacters.FirstOrDefault();
        }

        /// <summary>
        ///     随机抽取一个角色 (基于稀有度权重)
        /// </summary>
        public CharacterConfig GetRandomCharacter()
        {
            if (allCharacters.Count == 0) return null;

            // 根据稀有度权重随机选择
            var totalWeight = 0f;
            var weightedCharacters = new List<WeightedCharacter>();

            foreach (var character in allCharacters)
            {
                if (character == null) continue;

                var weight = gachaSettings.GetRarityWeight(character.rarity);
                totalWeight += weight;

                weightedCharacters.Add(new WeightedCharacter
                {
                    character = character,
                    weight = weight,
                    cumulativeWeight = totalWeight
                });
            }

            // 随机选择
            var randomValue = Random.Range(0f, totalWeight);
            return weightedCharacters.FirstOrDefault(wc => randomValue <= wc.cumulativeWeight)?.character;
        }

        /// <summary>
        ///     验证数据库配置
        /// </summary>
        [ContextMenu("验证配置")]
        public void ValidateDatabase()
        {
            var issues = new List<string>();

            // 检查空引用
            for (var i = 0; i < allCharacters.Count; i++)
            {
                if (allCharacters[i] == null) issues.Add($"角色列表索引 {i} 为空");
            }

            // 检查重复ID
            var ids = new HashSet<string>();
            foreach (var character in allCharacters)
            {
                if (character != null)
                {
                    if (string.IsNullOrEmpty(character.characterID))
                        issues.Add($"角色 {character.name} 缺少ID");
                    else if (ids.Contains(character.characterID))
                        issues.Add($"重复的角色ID: {character.characterID}");
                    else
                        ids.Add(character.characterID);
                }
            }

            // 检查默认角色
            if (defaultCharacter == null)
                issues.Add("未设置默认角色");
            else if (!allCharacters.Contains(defaultCharacter)) issues.Add("默认角色不在角色列表中");

            // 输出结果
            if (issues.Count == 0)
                Debug.Log("[CharacterDatabase] 验证通过，无问题发现");
            else
                Debug.LogWarning($"[CharacterDatabase] 发现 {issues.Count} 个问题:\n" + string.Join("\n", issues));
        }

        private void OnValidate()
        {
            // 自动移除空引用
            // allCharacters.RemoveAll(c => c == null);

            // 刷新查找表
            RefreshLookup();
        }

        /// <summary>
        ///     权重角色结构
        /// </summary>
        private class WeightedCharacter
        {
            public CharacterConfig character;
            public float weight;
            public float cumulativeWeight;
        }
    }

    /// <summary>
    ///     抽卡设置
    /// </summary>
    [Serializable]
    public class GachaSettings
    {
        [Header("稀有度权重")]
        [Range(0f, 100f)]
        public float commonWeight = 60f; // 普通 60%

        [Range(0f, 100f)]
        public float rareWeight = 25f; // 稀有 25%

        [Range(0f, 100f)]
        public float epicWeight = 12f; // 史诗 12%

        [Range(0f, 100f)]
        public float legendaryWeight = 3f; // 传说 3%

        /// <summary>
        ///     获取指定稀有度的权重
        /// </summary>
        public float GetRarityWeight(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.Common: return commonWeight;
                case CharacterRarity.Rare: return rareWeight;
                case CharacterRarity.Epic: return epicWeight;
                case CharacterRarity.Legendary: return legendaryWeight;
                default: return commonWeight;
            }
        }

        /// <summary>
        ///     获取稀有度概率 (百分比)
        /// </summary>
        public float GetRarityProbability(CharacterRarity rarity)
        {
            var totalWeight = commonWeight + rareWeight + epicWeight + legendaryWeight;
            return totalWeight > 0 ? GetRarityWeight(rarity) / totalWeight * 100f : 0f;
        }
    }
}