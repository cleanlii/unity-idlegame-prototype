using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace IdleGame.Character
{
    /// <summary>
    ///     Character configuration database - manages all CharacterConfig ScriptableObjects
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "IdleGame/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        [Header("Character Config List")]
        public List<CharacterConfig> allCharacters = new();

        [Header("Default Character")]
        public CharacterConfig defaultCharacter;

        [Header("Gacha Weight Settings")]
        public GachaSettings gachaSettings = new();

        private Dictionary<string, CharacterConfig> characterLookup;

        private void OnEnable()
        {
            RefreshLookup();
        }

        /// <summary>
        ///     Refresh lookup dictionary
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
        ///     Get character config by ID
        /// </summary>
        public CharacterConfig GetCharacterConfig(string characterID)
        {
            if (characterLookup == null) RefreshLookup();
            characterLookup.TryGetValue(characterID, out var config);
            return config;
        }

        /// <summary>
        ///     Get character list by rarity
        /// </summary>
        public List<CharacterConfig> GetCharactersByRarity(CharacterRarity rarity)
        {
            return allCharacters.Where(c => c != null && c.rarity == rarity).ToList();
        }

        /// <summary>
        ///     Get default character config
        /// </summary>
        public CharacterConfig GetDefaultCharacter()
        {
            return defaultCharacter != null ? defaultCharacter : allCharacters.FirstOrDefault();
        }

        /// <summary>
        ///     Get a random character (based on rarity weights)
        /// </summary>
        public CharacterConfig GetRandomCharacter()
        {
            if (allCharacters.Count == 0) return null;

            // Random selection based on rarity weights
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

            // Random pick
            var randomValue = Random.Range(0f, totalWeight);
            return weightedCharacters.FirstOrDefault(wc => randomValue <= wc.cumulativeWeight)?.character;
        }

        /// <summary>
        ///     Validate database config
        /// </summary>
        [ContextMenu("Validate Config")]
        public void ValidateDatabase()
        {
            var issues = new List<string>();

            // Check null references
            for (var i = 0; i < allCharacters.Count; i++)
            {
                if (allCharacters[i] == null) issues.Add($"Character list index {i} is null");
            }

            // Check duplicate IDs
            var ids = new HashSet<string>();
            foreach (var character in allCharacters)
            {
                if (character != null)
                {
                    if (string.IsNullOrEmpty(character.characterID))
                        issues.Add($"Character {character.name} is missing ID");
                    else if (ids.Contains(character.characterID))
                        issues.Add($"Duplicate character ID: {character.characterID}");
                    else
                        ids.Add(character.characterID);
                }
            }

            // Check default character
            if (defaultCharacter == null)
                issues.Add("Default character not set");
            else if (!allCharacters.Contains(defaultCharacter)) issues.Add("Default character is not in the character list");

            // Output results
            if (issues.Count == 0)
                Debug.Log("[CharacterDatabase] Validation passed, no issues found");
            else
                Debug.LogWarning($"[CharacterDatabase] Found {issues.Count} issues:\n" + string.Join("\n", issues));
        }

        private void OnValidate()
        {
            // Automatically remove null references if needed
            // allCharacters.RemoveAll(c => c == null);

            // Refresh lookup table
            RefreshLookup();
        }

        /// <summary>
        ///     Weighted character structure
        /// </summary>
        private class WeightedCharacter
        {
            public CharacterConfig character;
            public float weight;
            public float cumulativeWeight;
        }
    }

    /// <summary>
    ///     Gacha settings
    /// </summary>
    [Serializable]
    public class GachaSettings
    {
        [Header("Rarity Weights")]
        [Range(0f, 100f)]
        public float commonWeight = 60f; // Common 60%

        [Range(0f, 100f)]
        public float rareWeight = 25f; // Rare 25%

        [Range(0f, 100f)]
        public float epicWeight = 12f; // Epic 12%

        [Range(0f, 100f)]
        public float legendaryWeight = 3f; // Legendary 3%

        /// <summary>
        ///     Get weight of a specific rarity
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
        ///     Get probability of a rarity (percentage)
        /// </summary>
        public float GetRarityProbability(CharacterRarity rarity)
        {
            var totalWeight = commonWeight + rareWeight + epicWeight + legendaryWeight;
            return totalWeight > 0 ? GetRarityWeight(rarity) / totalWeight * 100f : 0f;
        }
    }
}