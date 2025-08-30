using UnityEngine;

namespace IdleGame.Gameplay.Battle
{
    /// <summary>
    ///     敌人配置 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "IdleGame/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("基础信息")]
        public string enemyID;
        public string enemyName = "敌人";
        public EnemyType enemyType = EnemyType.Normal;

        [Header("自定义属性")]
        public bool hasCustomStats;
        public float baseHP = 100f;
        public float baseAttack = 20f;
        public float baseDefense = 10f;

        [Header("外观")]
        public Color enemyColor = Color.red;
        public float sizeScale = 1f;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(enemyID))
                enemyID = name.ToLower().Replace(" ", "_");
        }
    }
}