using UnityEngine;

namespace IdleGame.Gameplay
{
    /// <summary>
    ///     路线配置 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "RouteConfig", menuName = "IdleGame/Route Config")]
    public class RouteConfig : ScriptableObject
    {
        [Header("基础信息")]
        public string routeName = "路线";
        public string routeDescription = "路线描述";

        [Header("收益设置")]
        public float intervalTime = 1f; // 收益间隔时间(秒)
        public long coinReward; // 金币收益
        public long expReward; // 经验收益

        [Header("特殊设置")]
        public bool isActive = true; // 是否激活
        public float efficiencyMultiplier = 1f; // 效率倍率

        public void Initialize(string name, float interval, long coins, long exp)
        {
            routeName = name;
            intervalTime = interval;
            coinReward = coins;
            expReward = exp;
        }

        /// <summary>
        ///     获取实际金币收益
        /// </summary>
        public long GetActualCoinReward()
        {
            return (long)(coinReward * efficiencyMultiplier);
        }

        /// <summary>
        ///     获取实际经验收益
        /// </summary>
        public long GetActualExpReward()
        {
            return (long)(expReward * efficiencyMultiplier);
        }
    }
}