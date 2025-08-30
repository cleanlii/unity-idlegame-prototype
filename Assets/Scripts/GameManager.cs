using IdleGame.Analytics;
using IdleGame.Gameplay;
using IdleGame.Gameplay.Battle;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    [Header("Game Data")]
    public PlayerData playerData;

    [Header("Managers")]
    public BattleManager battleManager;
    public UIManager uiManager;

    [Header("Systems")]
    public EcoSystem ecoSystem;
    public IdleLogSystem logSystem;
    public SpireSystem spireSystem;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null) Debug.LogError("No GameManager found in the scene.");
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
            Destroy(gameObject);
    }

    private void InitializeGame()
    {
    }
}