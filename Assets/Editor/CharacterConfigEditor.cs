#if UNITY_EDITOR
using System.Collections.Generic;
using IdleGame.Character;
using UnityEditor;
using UnityEngine;

namespace IdleGame.Editor
{
    /// <summary>
    ///     Custom Inspector editor for CharacterConfig
    /// </summary>
    [CustomEditor(typeof(CharacterConfig))]
    public class CharacterConfigEditor : UnityEditor.Editor
    {
        private CharacterConfig config;
        private bool showPreviewStats = true;
        private int previewLevel = 1;

        private void OnEnable()
        {
            config = (CharacterConfig)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Preview section
            showPreviewStats = EditorGUILayout.Foldout(showPreviewStats, "Preview Stats", true);
            if (showPreviewStats) DrawPreviewSection();

            // Tool buttons
            EditorGUILayout.Space(5);
            DrawToolButtons();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///     Draw preview stats section
        /// </summary>
        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical("Box");

            // Level selector
            previewLevel = EditorGUILayout.IntSlider("Preview Level", previewLevel, 1, 50);

            EditorGUILayout.Space(5);

            // Attributes display
            if (config != null)
            {
                EditorGUILayout.LabelField("Level Stats", EditorStyles.boldLabel);

                var maxHP = config.CalculateMaxHP(previewLevel);
                var attack = config.CalculateAttack(previewLevel);
                var defense = config.CalculateDefense(previewLevel);
                var critRate = config.CalculateCriticalRate(previewLevel);
                var powerScore = config.CalculatePowerScore(previewLevel);
                var expRequired = config.CalculateExpRequired(previewLevel);

                EditorGUILayout.LabelField($"Max HP: {maxHP:F0}");
                EditorGUILayout.LabelField($"Attack: {attack:F0}");
                EditorGUILayout.LabelField($"Defense: {defense:F0}");
                EditorGUILayout.LabelField($"Crit Rate: {critRate:P1}");
                EditorGUILayout.LabelField($"Power Score: {powerScore:F0}");
                EditorGUILayout.LabelField($"EXP Required: {expRequired:N0}");
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        ///     Draw tool buttons
        /// </summary>
        private void DrawToolButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Create Test Character Data")) CreateTestCharacterData();

            if (GUILayout.Button("Validate Config")) ValidateConfig();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        ///     Create test character data
        /// </summary>
        private void CreateTestCharacterData()
        {
            if (config != null)
            {
                var testData = config.CreateCharacterData();
                Debug.Log($"Created test character: {testData.GetCharacterInfo()}\n" +
                          $"Base Stats - HP:{testData.GetMaxHP():F0} ATK:{testData.GetAttack():F0} DEF:{testData.GetDefense():F0}");
            }
        }

        /// <summary>
        ///     Validate config
        /// </summary>
        private void ValidateConfig()
        {
            var issues = new List<string>();

            if (config == null) return;

            // Validate basic info
            if (string.IsNullOrEmpty(config.characterID))
                issues.Add("Character ID is empty");

            if (string.IsNullOrEmpty(config.characterName))
                issues.Add("Character name is empty");

            // Validate values
            if (config.baseMaxHP <= 0)
                issues.Add("Base HP must be greater than 0");

            if (config.baseAttack <= 0)
                issues.Add("Base Attack must be greater than 0");

            if (config.baseExpRequired <= 0)
                issues.Add("Base EXP Required must be greater than 0");

            if (config.expGrowthFactor <= 1.0f)
                issues.Add("EXP Growth Factor should be greater than 1.0");

            // Output result
            if (issues.Count == 0)
                EditorUtility.DisplayDialog("Validation Result", "Config validated successfully, no issues found!", "OK");
            else
            {
                var message = "Found the following issues:\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Validation Result", message, "OK");
            }
        }
    }

    /// <summary>
    ///     Custom Inspector editor for CharacterDatabase
    /// </summary>
    [CustomEditor(typeof(CharacterDatabase))]
    public class CharacterDatabaseEditor : UnityEditor.Editor
    {
        private CharacterDatabase database;
        private bool showGachaPreview = true;

        private void OnEnable()
        {
            database = (CharacterDatabase)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Statistics
            DrawStatistics();

            // Gacha preview
            showGachaPreview = EditorGUILayout.Foldout(showGachaPreview, "Gacha Probability Preview", true);
            if (showGachaPreview) DrawGachaPreview();

            // Tool buttons
            EditorGUILayout.Space(5);
            DrawDatabaseTools();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///     Draw statistics
        /// </summary>
        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Database Statistics", EditorStyles.boldLabel);

            if (database != null)
            {
                var totalCount = database.allCharacters.Count;
                var commonCount = database.GetCharactersByRarity(CharacterRarity.Common).Count;
                var rareCount = database.GetCharactersByRarity(CharacterRarity.Rare).Count;
                var epicCount = database.GetCharactersByRarity(CharacterRarity.Epic).Count;
                var legendaryCount = database.GetCharactersByRarity(CharacterRarity.Legendary).Count;

                EditorGUILayout.LabelField($"Total Characters: {totalCount}");
                EditorGUILayout.LabelField($"Common: {commonCount}, Rare: {rareCount}, Epic: {epicCount}, Legendary: {legendaryCount}");
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        ///     Draw gacha preview
        /// </summary>
        private void DrawGachaPreview()
        {
            EditorGUILayout.BeginVertical("Box");

            if (database?.gachaSettings != null)
            {
                var gacha = database.gachaSettings;
                EditorGUILayout.LabelField($"Common: {gacha.GetRarityProbability(CharacterRarity.Common):F1}%");
                EditorGUILayout.LabelField($"Rare: {gacha.GetRarityProbability(CharacterRarity.Rare):F1}%");
                EditorGUILayout.LabelField($"Epic: {gacha.GetRarityProbability(CharacterRarity.Epic):F1}%");
                EditorGUILayout.LabelField($"Legendary: {gacha.GetRarityProbability(CharacterRarity.Legendary):F1}%");
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        ///     Draw database tools
        /// </summary>
        private void DrawDatabaseTools()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Validate Database")) database?.ValidateDatabase();

            if (GUILayout.Button("Test Random Gacha")) TestRandomGacha();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        ///     Test random gacha
        /// </summary>
        private void TestRandomGacha()
        {
            if (database != null)
            {
                var randomChar = database.GetRandomCharacter();
                if (randomChar != null)
                    Debug.Log($"Randomly pulled: {randomChar.characterName} ({randomChar.rarity})");
                else
                    Debug.LogWarning("Failed to pull character, please check database config");
            }
        }
    }
}
#endif