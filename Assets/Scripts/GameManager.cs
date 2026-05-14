using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI — HUD")]
    public TMP_Text   scoreText;
    public TMP_Text   birdsLeftText;
    public TMP_Text   currentBirdText;

    [Header("UI — Win Screen")]
    public GameObject winPanel;
    public TMP_Text   winScoreText;

    [Header("UI — Lose Screen")]
    public GameObject losePanel;
    public TMP_Text   loseScoreText;

    [Header("Bird Queue")]
    public List<BirdEntry> birdQueue = new List<BirdEntry>();

    [Header("Pigs — assign all pig GameObjects in level")]
    public List<GameObject> pigObjects = new List<GameObject>();

    [Header("Settings")]
    public int scorePerPig = 500;
    public int scoreBird   = 1000;

    [Header("References")]
    public Slingshot slingshot;

    [System.Serializable]
    public class BirdEntry
    {
        public GameObject prefab;
        public int        count;
        public string     birdName;
    }

    private Dictionary<string, int> _birdCounts  = new Dictionary<string, int>();

    public Queue<GameObject> SpawnQueue { get; set; }
    public int               BirdsLeft  { get; private set; }

    private int  _score       = 0;
    private int  _pigsAlive   = 0; // tracked by counter — no FindObjects needed
    private bool _gameOver    = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildSpawnQueue();
        SetupPigs();
        if (winPanel)  winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        UpdateUI();
    }

    void BuildSpawnQueue()
    {
        SpawnQueue  = new Queue<GameObject>();
        _birdCounts = new Dictionary<string, int>();

        foreach (BirdEntry entry in birdQueue)
        {
            if (entry.prefab == null) continue;
            for (int i = 0; i < entry.count; i++)
                SpawnQueue.Enqueue(entry.prefab);

            string name = string.IsNullOrEmpty(entry.birdName)
                          ? entry.prefab.name : entry.birdName;

            if (_birdCounts.ContainsKey(name)) _birdCounts[name] += entry.count;
            else                               _birdCounts[name]  = entry.count;
        }

        BirdsLeft = SpawnQueue.Count;
    }

    void SetupPigs()
    {
        pigObjects.RemoveAll(p => p == null);
        // Counter starts at how many pigs are in the scene
        _pigsAlive = pigObjects.Count;
    }

    public void BirdLaunched()
    {
        if (_gameOver) return;
        BirdsLeft--;
        UpdateBirdCountForLaunch();
        UpdateUI();

        if (BirdsLeft <= 0)
        {
            if (slingshot != null) slingshot.Disable();
            Invoke(nameof(CheckEndCondition), 4f);
        }
    }

    void UpdateBirdCountForLaunch()
    {
        foreach (BirdEntry entry in birdQueue)
        {
            string name = string.IsNullOrEmpty(entry.birdName)
                          ? entry.prefab.name : entry.birdName;
            if (_birdCounts.ContainsKey(name) && _birdCounts[name] > 0)
            {
                _birdCounts[name]--;
                if (_birdCounts[name] <= 0) _birdCounts.Remove(name);
                break;
            }
        }
    }

    // ── Called by Pig.Die() ───────────────────────
    public void PigKilled()
    {
        if (_gameOver) return;

        _score     += scorePerPig;
        _pigsAlive  = Mathf.Max(0, _pigsAlive - 1);
        UpdateUI();

        // Win the instant counter hits zero
        // No FindObjects — counter is always accurate
        if (_pigsAlive == 0)
        {
            CancelInvoke();
            WinGame();
        }
    }

    void CheckEndCondition()
    {
        if (_gameOver) return;
        if (_pigsAlive == 0) WinGame();
        else                 LoseGame();
    }

    public void AddScore(int pts)
    {
        if (_gameOver) return;
        _score += pts;
        UpdateUI();
    }

    void WinGame()
    {
        if (_gameOver) return;
        _gameOver = true;
        _score   += BirdsLeft * scoreBird;
        UpdateUI();
        if (winScoreText) winScoreText.text = $"Score: {_score}";
        string key = $"Best_{SceneManager.GetActiveScene().buildIndex}";
        if (_score > PlayerPrefs.GetInt(key, 0))
            PlayerPrefs.SetInt(key, _score);
        PlayerPrefs.Save();
        if (winPanel) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void LoseGame()
    {
        if (_gameOver) return;
        _gameOver = true;
        if (loseScoreText) loseScoreText.text = $"Score: {_score}";
        if (losePanel)     losePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(
            next < SceneManager.sceneCountInBuildSettings ? next : 0);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    void UpdateUI()
    {
        if (scoreText)     scoreText.text    = $"Score: {_score}";
        if (birdsLeftText) birdsLeftText.text = $"Birds: {BirdsLeft}";

        if (currentBirdText != null)
        {
            if (_birdCounts.Count == 0)
            {
                currentBirdText.text = "No Birds Left";
            }
            else
            {
                var parts = new List<string>();
                foreach (var kvp in _birdCounts)
                    parts.Add($"{kvp.Key} x{kvp.Value}");
                currentBirdText.text = string.Join("  ", parts);
            }
        }
    }
}