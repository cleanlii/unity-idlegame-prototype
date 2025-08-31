using UnityEngine;

namespace IdleGame
{
    public static class IdleGameConst
    {
        // Save & Load Path
        public const string SAVE_FILE_NAME = "playerdata.json";
        public const string BACKUP_FILE_NAME = "playerdata_backup.json";
        public static readonly string SAVE_PATH = Application.persistentDataPath;
        public static readonly string BACKUP_PATH = "Assets/Test/BackupSaves";

        // Log info
        public const string LOG_PLAYER_NAME = "You";
        public const string LOG_ENEMY_NAME = "Enemy";
    }
}