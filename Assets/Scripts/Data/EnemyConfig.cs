using UnityEngine;

namespace IdleGame.Gameplay.Battle
{
    /// <summary>
    ///     Defines base attributes for different enemies
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "IdleGame/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Basic Information")]
        public string enemyID;
        public string enemyName = "NewEnemy";
        public EnemyType enemyType = EnemyType.Normal;

        [Header("Basic Statistics")]
        public bool hasCustomStats;
        public float baseHP = 100f;
        public float baseAttack = 20f;
        public float baseDefense = 10f;

        // [Header("Appearance (TODO)")]
        // public Color enemyColor = Color.red;
        // public float sizeScale = 1f;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(enemyID))
                enemyID = name.ToLower().Replace(" ", "_");
        }
    }
}