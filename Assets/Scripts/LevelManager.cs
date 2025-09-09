using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    private static LevelManager instance;
    public static LevelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<LevelManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("LevelManager");
                    instance = go.AddComponent<LevelManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("Level Settings")]
    public int totalLevels = 10; // Set this to your total number of levels
    
    private const string LEVEL_PROGRESS_KEY = "LevelProgress";
    private const string HIGHEST_UNLOCKED_KEY = "HighestUnlockedLevel";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Ensure level 1 is always unlocked
        if (!IsLevelUnlocked(1))
        {
            UnlockLevel(1);
        }
    }

    /// <summary>
    /// Check if a specific level is unlocked
    /// </summary>
    /// <param name="levelNumber">Level number (1-based)</param>
    /// <returns>True if level is unlocked</returns>
    public bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber == 1) return true; // Level 1 is always unlocked
        
        string key = LEVEL_PROGRESS_KEY + levelNumber;
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Check if a level is completed
    /// </summary>
    /// <param name="levelNumber">Level number (1-based)</param>
    /// <returns>True if level is completed</returns>
    public bool IsLevelCompleted(int levelNumber)
    {
        string key = "LevelCompleted" + levelNumber;
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Unlock a specific level
    /// </summary>
    /// <param name="levelNumber">Level number to unlock (1-based)</param>
    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber <= 0 || levelNumber > totalLevels) return;

        string key = LEVEL_PROGRESS_KEY + levelNumber;
        PlayerPrefs.SetInt(key, 1);
        
        // Update highest unlocked level
        int currentHighest = PlayerPrefs.GetInt(HIGHEST_UNLOCKED_KEY, 1);
        if (levelNumber > currentHighest)
        {
            PlayerPrefs.SetInt(HIGHEST_UNLOCKED_KEY, levelNumber);
        }
        
        PlayerPrefs.Save();
        Debug.Log($"Level {levelNumber} unlocked!");
    }

    /// <summary>
    /// Mark a level as completed and unlock the next level
    /// </summary>
    /// <param name="levelNumber">Level number that was completed (1-based)</param>
    public void CompleteLevel(int levelNumber)
    {
        if (levelNumber <= 0 || levelNumber > totalLevels) return;

        // Mark level as completed
        string completedKey = "LevelCompleted" + levelNumber;
        PlayerPrefs.SetInt(completedKey, 1);
        
        // Unlock next level
        int nextLevel = levelNumber + 1;
        if (nextLevel <= totalLevels)
        {
            UnlockLevel(nextLevel);
        }
        
        PlayerPrefs.Save();
        Debug.Log($"Level {levelNumber} completed! Next level {nextLevel} unlocked.");
    }

    /// <summary>
    /// Get the highest unlocked level
    /// </summary>
    /// <returns>Highest unlocked level number</returns>
    public int GetHighestUnlockedLevel()
    {
        return PlayerPrefs.GetInt(HIGHEST_UNLOCKED_KEY, 1);
    }

    /// <summary>
    /// Load a level by number (only if unlocked)
    /// </summary>
    /// <param name="levelNumber">Level number to load (1-based)</param>
    public void LoadLevel(int levelNumber)
    {
        if (!IsLevelUnlocked(levelNumber))
        {
            Debug.LogWarning($"Cannot load level {levelNumber} - it's still locked!");
            return;
        }

        string sceneName = "Level" + levelNumber;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Load a level by scene name (only if unlocked)
    /// </summary>
    /// <param name="sceneName">Scene name to load</param>
    public void LoadLevelByName(string sceneName)
    {
        // Extract level number from scene name
        int levelNumber = ExtractLevelNumberFromName(sceneName);
        
        if (levelNumber > 0 && !IsLevelUnlocked(levelNumber))
        {
            Debug.LogWarning($"Cannot load {sceneName} - it's still locked!");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Extract level number from scene name (assumes format like "Level1", "Level2", etc.)
    /// </summary>
    /// <param name="sceneName">Scene name</param>
    /// <returns>Level number or -1 if not found</returns>
    private int ExtractLevelNumberFromName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return -1;
        
        string numberPart = "";
        for (int i = 0; i < sceneName.Length; i++)
        {
            if (char.IsDigit(sceneName[i]))
            {
                numberPart += sceneName[i];
            }
        }
        
        if (int.TryParse(numberPart, out int levelNumber))
        {
            return levelNumber;
        }
        
        return -1;
    }

    /// <summary>
    /// Reset all level progress (for testing or reset functionality)
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        for (int i = 1; i <= totalLevels; i++)
        {
            PlayerPrefs.DeleteKey(LEVEL_PROGRESS_KEY + i);
            PlayerPrefs.DeleteKey("LevelCompleted" + i);
        }
        PlayerPrefs.DeleteKey(HIGHEST_UNLOCKED_KEY);
        
        // Ensure level 1 is unlocked
        UnlockLevel(1);
        
        Debug.Log("All level progress has been reset!");
    }

    /// <summary>
    /// Get completion percentage
    /// </summary>
    /// <returns>Percentage of levels completed (0-100)</returns>
    public float GetCompletionPercentage()
    {
        int completedLevels = 0;
        for (int i = 1; i <= totalLevels; i++)
        {
            if (IsLevelCompleted(i))
            {
                completedLevels++;
            }
        }
        
        return (float)completedLevels / totalLevels * 100f;
    }
}