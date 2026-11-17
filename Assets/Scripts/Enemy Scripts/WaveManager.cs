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

    [Tooltip("Safety cap so a big leftover budget can never create an unbeatable wave")]
    public int maxEnemiesPerWave = 40;

    [Header("End Goal")]
    [Tooltip("How many normal waves the player must survive before the boss round")]
    public int totalWaves = 10;

    [Tooltip("Regular enemies that spawn alongside the boss")]
    public int bossEscortCount = 4;

    [Header("References")]
    public EnemySpawner enemySpawner; // Enemy spawning script

    public int EnemiesThisWave { get; private set; }
    public int EnemiesKilled { get; private set; }
    public int WaveNumber { get; private set; }
    public bool IsSpawning { get; private set; }
    public bool IsBossRound { get; private set; }
    public bool GameWon { get; private set; }

    private bool waitingForNextWave;
    private int enemiesToKill; // how many enemies this wave actually spawned

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
        IsBossRound = false;
        GameWon = false;
    }

    private void Update()
    {
        if (!GenerationGrid.TerrainReady)
            return;

        if (!IsSpawning && !waitingForNextWave)
            StartWave();
    }

    // Enemy.cs calls this when an enemy dies
    public static void NotifyEnemyKilled(bool wasBoss = false)
    {
        if (Instance != null)
            Instance.OnEnemyKilled(wasBoss);
    }

    private void StartWave()
    {
        if (GameWon)
            return;

        EnemiesKilled = 0;
        IsSpawning = true;
        waitingForNextWave = false;

        // All the normal waves are survived - time for the boss
        if (WaveNumber > totalWaves)
        {
            StartBossRound();
            return;
        }

        // The whole wave spawns at the same time
        int spawned = 0;
        if (enemySpawner != null)
            spawned = enemySpawner.SpawnWave(EnemiesThisWave);

        enemiesToKill = spawned;

        if (spawned <= 0)
        {
            RetryStartLater();
            return;
        }

        Debug.Log($"Wave {WaveNumber} started — {spawned} enemies.");
    }

    private void StartBossRound()
    {
        IsBossRound = true;

        bool bossSpawned = false;
        if (enemySpawner != null)
            bossSpawned = enemySpawner.SpawnBoss(bossEscortCount);

        if (!bossSpawned)
        {
            RetryStartLater();
            return;
        }

        Debug.Log("BOSS ROUND started!");
    }

    // Nothing could spawn (e.g. the NavMesh isn't ready)
    private void RetryStartLater()
    {
        IsSpawning = false;
        waitingForNextWave = true;
        Invoke(nameof(StartWave), delayBetweenWaves);
    }

    private void OnEnemyKilled(bool wasBoss)
    {
        if (GameWon)
            return;

        EnemiesKilled++;

        // In the boss round only the boss matters - kill it and the game is won
        if (IsBossRound)
        {
            if (wasBoss)
                WinGame();
            
            return;
        }

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
        int extraEnemies = remainingBudget / Mathf.Max(1, budgetDivisor);
        EnemiesThisWave = Mathf.Min(EnemiesThisWave + extraEnemies, maxEnemiesPerWave);
        WaveNumber++;

        if (WaveNumber > totalWaves)
            Debug.Log($"Wave {totalWaves} cleared. Remaining budget {remainingBudget}. The BOSS is next!");
        else
            Debug.Log($"Wave ended. Remaining budget {remainingBudget}. Next wave will spawn {EnemiesThisWave} enemies.");

        waitingForNextWave = true;
        Invoke(nameof(StartWave), delayBetweenWaves);
    }

    private void WinGame()
    {
        GameWon = true;
        IsSpawning = false;
        CancelInvoke();

        Debug.Log("Boss defeated - YOU WIN!");

        if (UIManager.Instance != null)
            UIManager.Instance.ShowVictory();
    }

}