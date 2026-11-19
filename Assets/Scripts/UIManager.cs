using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [Tooltip("Players current resource total displayed in the UI")]
    public TMP_Text resourceText;

    [Tooltip("Towers current health displayed in the UI")]
    public TMP_Text towerHealthText;

    [Tooltip("Current wave and kill progress")]
    public TMP_Text waveText;

    [Header("Game Over")]
    [Tooltip("The Game Over UI panel")]
    public GameObject gameOverPanel;

    [Tooltip("Shown when the boss is defeated (duplicate the Game Over panel and change its text to something like YOU WIN)")]
    public GameObject victoryPanel;

    [Header("Pause")]
    [Tooltip("Panel shown while the game is paused")]
    public GameObject pausePanel;

    private Tower tower;
    private bool isPaused = false;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Tower.OnTowerSpawned += HandleTowerSpawned;

        if (tower == null)
            tower = FindAnyObjectByType<Tower>();
    }

    private void OnDisable()
    {
        Tower.OnTowerSpawned -= HandleTowerSpawned;
    }

    private void HandleTowerSpawned(Tower spawnedTower)
    {
        tower = spawnedTower;
    }

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (resourceText != null && ResourceManager.Instance != null)
        {
            resourceText.text = "Resources: " + ResourceManager.Instance.currentResource;
        }

        if (towerHealthText != null && tower != null)
        {
            towerHealthText.text = "TOWER HEALTH: " + tower.CurrentHealth + "/" + tower.MaxHealth;
        }

        if (waveText != null && WaveManager.Instance != null)
        {
            if (WaveManager.Instance.IsBossRound)
            {
                waveText.text = "BOSS ROUND";

                if (!string.IsNullOrEmpty(EnemyMutator.BossName))
                {
                    waveText.text = "BOSS ROUND - " + EnemyMutator.BossName;
                }
            }
            else
            {
                waveText.text = "Wave " + WaveManager.Instance.WaveNumber
                    + "/" + WaveManager.Instance.totalWaves
                    + "  " + WaveManager.Instance.EnemiesKilled
                    + "/" + WaveManager.Instance.EnemiesInCurrentWave;
            }
        }

    }

    //Called when towers health reaches 0
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f; // Pause the game
    }

    // Called by WaveManager when the boss is defeated
    public void ShowVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        Time.timeScale = 0f; // Pause the game
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }

        Time.timeScale = isPaused ? 0f : 1f; // Pause or resume the game
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

        public void LoadMainMenu(string mainMenuSceneName)
    {
        Time.timeScale = 1f; // Always reset time scale before leaving!
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}
