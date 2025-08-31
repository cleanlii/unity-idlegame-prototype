using System;
using System.IO;
using IdleGame.Analytics;
using UnityEngine;

namespace IdleGame.Core
{
    /// <summary>
    ///     存档系统 - 处理PlayerData的保存和加载
    /// </summary>
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(IdleGameConst.SAVE_PATH, IdleGameConst.SAVE_FILE_NAME);
        private static string BackupPath => Path.Combine(IdleGameConst.BACKUP_PATH, IdleGameConst.BACKUP_FILE_NAME);

        public static event Action<PlayerData> OnDataLoaded;
        public static event Action<PlayerData> OnDataSaved;
        public static event Action<string> OnSaveError;

        /// <summary>
        ///     保存玩家数据
        /// </summary>
        public static bool SavePlayerData(PlayerData playerData)
        {
            if (playerData == null)
            {
                Debug.LogError("[SaveSystem] PlayerData is null, cannot save!");
                OnSaveError?.Invoke("玩家数据为空");
                return false;
            }

            try
            {
                // 更新保存时间
                playerData.UpdateSaveTime();

                // 序列化数据
                // var jsonData = JsonUtility.ToJson(playerData, true);

                // 如果已存在存档，先备份
                if (File.Exists(SavePath)) File.Copy(SavePath, BackupPath, true);

                // 保存新数据
                // File.WriteAllText(SavePath, jsonData);

                // TODO: 加密保存
                JsonUtils.SaveEncryptedJson(SavePath, playerData, true);

                Debug.Log($"[SaveSystem] Data saved successfully at: {SavePath}");
                OnDataSaved?.Invoke(playerData);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save data: {e.Message}");
                OnSaveError?.Invoke($"保存失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        ///     加载玩家数据
        /// </summary>
        public static PlayerData LoadPlayerData()
        {
            PlayerData playerData = null;

            // 尝试加载主存档
            if (File.Exists(SavePath))
            {
                playerData = LoadFromFile(SavePath);
                if (playerData != null)
                {
                    Debug.Log("[SaveSystem] Data loaded successfully from main save");
                    OnDataLoaded?.Invoke(playerData);
                    return playerData;
                }

                Debug.LogWarning("[SaveSystem] Main save corrupted, trying backup...");
            }

            // 尝试加载备份存档
            if (File.Exists(BackupPath))
            {
                playerData = LoadFromFile(BackupPath);
                if (playerData != null)
                {
                    Debug.Log("[SaveSystem] Data loaded from backup save");
                    OnDataLoaded?.Invoke(playerData);
                    return playerData;
                }

                Debug.LogWarning("[SaveSystem] Backup save also corrupted");
            }

            // 创建新的存档
            Debug.Log("[SaveSystem] No valid save found, creating new player data");
            playerData = new PlayerData();
            OnDataLoaded?.Invoke(playerData);
            return playerData;
        }

        /// <summary>
        ///     从文件加载数据
        /// </summary>
        private static PlayerData LoadFromFile(string filePath)
        {
            try
            {
                var jsonData = File.ReadAllText(filePath);
                // var playerData = JsonUtility.FromJson<PlayerData>(jsonData);
                var playerData = JsonUtils.LoadEncryptedJson<PlayerData>(SavePath, true);

                // 验证数据完整性
                if (ValidatePlayerData(playerData)) return playerData;

                Debug.LogWarning($"[SaveSystem] Data validation failed for: {filePath}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load from {filePath}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        ///     验证PlayerData的完整性
        /// </summary>
        private static bool ValidatePlayerData(PlayerData data)
        {
            if (data == null) return false;
            if (string.IsNullOrEmpty(data.playerID)) return false;
            if (data.playerLevel < 1) return false;
            if (data.coins < 0) return false;
            // 可以添加更多验证规则
            return true;
        }

        /// <summary>
        ///     删除存档
        /// </summary>
        public static bool DeleteSaveData()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);

                Debug.Log("[SaveSystem] Save data deleted successfully");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to delete save data: {e.Message}");
                return false;
            }
        }

        /// <summary>
        ///     检查是否存在存档
        /// </summary>
        public static bool HasSaveData()
        {
            return File.Exists(SavePath) || File.Exists(BackupPath);
        }

        /// <summary>
        ///     获取存档文件信息
        /// </summary>
        public static string GetSaveFileInfo()
        {
            if (!File.Exists(SavePath)) return "无存档文件";

            try
            {
                var fileInfo = new FileInfo(SavePath);
                return $"存档大小: {fileInfo.Length / 1024f:F1}KB\n" +
                       $"修改时间: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
            }
            catch
            {
                return "存档信息获取失败";
            }
        }

        /// <summary>
        ///     自动保存 (定期调用)
        /// </summary>
        public static void AutoSave(PlayerData playerData)
        {
            if (playerData != null) SavePlayerData(playerData);
        }
    }
}