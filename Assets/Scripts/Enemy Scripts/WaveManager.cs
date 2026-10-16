using UnityEngine;

// Procedural enemy waves controller
public class WaveManager : MonoBehaviour
{
    // Lets the other scripts find this script
    public static WaveManager Instance { get; private set; }

    [Header("Wave")]
    [Tooltip("Enemies in wave 1")]
    public int startingEnemyCount = 5;

    [Tooltip("Remaining budget is divided by this to grow the next wave")]
    public int budgetDivisor = 10;

    [Tooltip("Seconds to wait after a wave before the next one starts")]
    public float delayBetweenWaves = 3f;

    [Header("References")]
    public EnemySpawner enemySpawner; // Enemy spawning script

    public int EnemiesThisWave { get; private set; }
    public int EnemiesKilled { get; private set; }
    public int WaveNumber { get; private set; }
    public bool IsSpawning { get; private set; }

    private bool waitingForNextWave;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        EnemiesThisWave = startingEnemyCount;
        EnemiesKilled = 0;
        WaveNumber = 1;
        IsSpawning = false;
    }

    private void Update()
    {
        if (!GenerationGrid.TerrainReady)
            return;

        if (!IsSpawning && !waitingForNextWave)
            StartWave();
    }

    public static void NotifyEnemyKilled()
    {
        if (Instance != null)
            Instance.OnEnemyKilled();
    }

    private void StartWave()
    {
        EnemiesKilled = 0;
        IsSpawning = true;
        waitingForNextWave = false;

        // Reset the enemy spawner for the new wave
        if (enemySpawner != null)
            enemySpawner.ResetForNewWave();

        Debug.Log($"Wave {WaveNumber} started — {EnemiesThisWave} enemies.");
    }

    private void OnEnemyKilled()
    {
        EnemiesKilled++;

        if (EnemiesKilled >= EnemiesThisWave)
            EndWave();
    }

    private void EndWave()
    {
        IsSpawning = false;

        // Save how much money the player has
        int remainingBudget = 0;
        if (ResourceManager.Instance != null)
            remainingBudget = ResourceManager.Instance.currentResource;

        // leftover budget / 10 = extra enemies
        int extraEnemies = remainingBudget / budgetDivisor;
        EnemiesThisWave += extraEnemies;
        WaveNumber++;

        Debug.Log($"Wave ended. Remaining budget {remainingBudget}. Next wave will spawn {EnemiesThisWave} enemies.");

        waitingForNextWave = true;
        Invoke(nameof(StartWave), delayBetweenWaves);
    }
}