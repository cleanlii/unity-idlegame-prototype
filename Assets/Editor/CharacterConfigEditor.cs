#if UNITY_EDITOR
using System.Collections.Generic;
using IdleGame.Character;
using UnityEditor;
using UnityEngine;

namespace IdleGame.Editor
{
    /// <summary>
    ///     角色配置的自定义Inspector编辑器
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

            // 绘制默认Inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // 预览区域
            showPreviewStats = EditorGUILayout.Foldout(showPreviewStats, "属性预览", true);
            if (showPreviewStats) DrawPreviewSection();

            // 工具按钮
            EditorGUILayout.Space(5);
            DrawToolButtons();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///     绘制属性预览区域
        /// </summary>
        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical("Box");

            // 等级选择器
            previewLevel = EditorGUILayout.IntSlider("预览等级", previewLevel, 1, 50);

            EditorGUILayout.Space(5);

            // 属性显示
            if (config != null)
            {
                EditorGUILayout.LabelField("等级属性", EditorStyles.boldLabel);

                var maxHP = config.CalculateMaxHP(previewLevel);
                var attack = config.CalculateAttack(previewLevel);
                var defense = config.CalculateDefense(previewLevel);
                var critRate = config.CalculateCriticalRate(previewLevel);
                var powerScore = config.CalculatePowerScore(previewLevel);
                var expRequired = config.CalculateExpRequired(previewLevel);

                EditorGUILayout.LabelField($"最大HP: {maxHP:F0}");
                EditorGUILayout.LabelField($"攻击力: {attack:F0}");
                EditorGUILayout.LabelField($"防御力: {defense:F0}");
                EditorGUILayout.LabelField($"暴击率: {critRate:P1}");
                EditorGUILayout.LabelField($"战力评分: {powerScore:F0}");
                EditorGUILayout.LabelField($"升级经验: {expRequired:N0}");
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        ///     绘制工具按钮
        /// </summary>
        private void DrawToolButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("创建测试角色数据")) CreateTestCharacterData();

            if (GUILayout.Button("验证配置")) ValidateConfig();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        ///     创建测试角色数据
        /// </summary>
        private void CreateTestCharacterData()
        {
            if (config != null)
            {
                var testData = config.CreateCharacterData();
                Debug.Log($"创建测试角色: {testData.GetCharacterInfo()}\n" +
                          $"基础属性 - HP:{testData.GetMaxHP():F0} ATK:{testData.GetAttack():F0} DEF:{testData.GetDefense():F0}");
            }
        }

        /// <summary>
        ///     验证配置
        /// </summary>
        private void ValidateConfig()
        {
            var issues = new List<string>();

            if (config == null) return;

            // 验证基本信息
            if (string.IsNullOrEmpty(config.characterID))
                issues.Add("角色ID为空");

            if (string.IsNullOrEmpty(config.characterName))
                issues.Add("角色名称为空");

            // 验证数值
            if (config.baseMaxHP <= 0)
                issues.Add("基础HP必须大于0");

            if (config.baseAttack <= 0)
                issues.Add("基础攻击力必须大于0");

            if (config.baseExpRequired <= 0)
                issues.Add("基础经验要求必须大于0");

            if (config.expGrowthFactor <= 1.0f)
                issues.Add("经验增长系数应该大于1.0");

            // 输出结果
            if (issues.Count == 0)
                EditorUtility.DisplayDialog("验证结果", "配置验证通过，无问题发现！", "确定");
            else
            {
                var message = "发现以下问题:\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("验证结果", message, "确定");
            }
        }
    }

    /// <summary>
    ///     角色数据库的自定义Inspector编辑器
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

            // 绘制默认Inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // 统计信息
            DrawStatistics();

            // 抽卡预览
            showGachaPreview = EditorGUILayout.Foldout(showGachaPreview, "抽卡概率预览", true);
            if (showGachaPreview) DrawGachaPreview();

            // 工具按钮
            EditorGUILayout.Space(5);
            DrawDatabaseTools();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///     绘制统计信息
        /// </summary>
        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("数据库统计", EditorStyles.boldLabel);

            if (database != null)
            {
                var totalCount = database.allCharacters.Count;
                var commonCount = database.GetCharactersByRarity(CharacterRarity.Common).Count;
                var rareCount = database.GetCharactersByRarity(CharacterRarity.Rare).Count;
                var epicCount = database.GetCharactersByRarity(CharacterRarity.Epic).Count;
                var legendaryCount = database.GetCharactersByRarity(CharacterRarity.Legendary).Count;

                EditorGUILayout.LabelField($"总计角色: {totalCount}");
                EditorGUILayout.LabelField($"普通: {commonCount}, 稀有: {rareCount}, 史诗: {epicCount}, 传说: {legendaryCount}");
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        ///     绘制抽卡预览
        /// </summary>
        private void DrawGachaPreview()
        {
            EditorGUILayout.BeginVertical("Box");

            if (database?.gachaSettings != null)
            {
                var gacha = database.gachaSettings;
                EditorGUILayout.LabelField($"普通: {gacha.GetRarityProbability(CharacterRarity.Common):F1}%");
                EditorGUILayout.LabelField($"稀有: {gacha.GetRarityProbability(CharacterRarity.Rare):F1}%");
                EditorGUILayout.LabelField($"史诗: {gacha.GetRarityProbability(CharacterRarity.Epic):F1}%");
                EditorGUILayout.LabelField($"传说: {gacha.GetRarityProbability(CharacterRarity.Legendary):F1}%");
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        ///     绘制数据库工具
        /// </summary>
        private void DrawDatabaseTools()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("验证数据库")) database?.ValidateDatabase();

            if (GUILayout.Button("测试随机抽取")) TestRandomGacha();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        ///     测试随机抽取
        /// </summary>
        private void TestRandomGacha()
        {
            if (database != null)
            {
                var randomChar = database.GetRandomCharacter();
                if (randomChar != null)
                    Debug.Log($"随机抽取到: {randomChar.characterName} ({randomChar.rarity})");
                else
                    Debug.LogWarning("未能抽取到角色，请检查数据库配置");
            }
        }
    }
}
#endif