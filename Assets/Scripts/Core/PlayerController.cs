using UnityEngine;

namespace IdleGame.Gameplay
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform coinRoutePos;
        [SerializeField] private Transform expRoutePos;
        [SerializeField] private Transform enemyRoutePos;

        /// <summary>
        ///     Temp method to move player among routes
        /// </summary>
        public void MoveToRoute(RouteType targetRoute)
        {
            transform.position = targetRoute switch
            {
                RouteType.Battle => enemyRoutePos.position,
                RouteType.Economy => coinRoutePos.position,
                RouteType.Experience => expRoutePos.position,
                _ => transform.position
            };
        }
    }
}