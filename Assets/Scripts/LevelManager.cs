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
    public int totalLevels = 10; // This will be auto-adjusted based on actual scenes
    
    [Header("Fallback Settings")]
    public string menuSceneName = "GameMenu"; // Fallback scene name
    
    private const string LEVEL_PROGRESS_KEY = "LevelProgress";
    private const string HIGHEST_UNLOCKED_KEY = "HighestUnlockedLevel";
    private int actualTotalLevels; // Actual number of level scenes found

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
            return;
        }
        
        // Validate and adjust total levels
        ValidateLevelScenes();
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
    /// Validate which level scenes actually exist and update totalLevels accordingly
    /// </summary>
    private void ValidateLevelScenes()
    {
        actualTotalLevels = 0;
        
        for (int i = 1; i <= totalLevels; i++)
        {
            string sceneName = "Level" + i;
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                actualTotalLevels = i;
            }
            else
            {
                Debug.LogWarning($"Scene '{sceneName}' not found in Build Settings. Stopping level validation at Level {actualTotalLevels}");
                break;
            }
        }
        
        if (actualTotalLevels < totalLevels)
        {
            Debug.LogWarning($"Only {actualTotalLevels} level scenes found in Build Settings. Adjusting totalLevels from {totalLevels} to {actualTotalLevels}");
            totalLevels = actualTotalLevels;
        }
        
        Debug.Log($"Level validation complete. Total playable levels: {totalLevels}");
    }

    /// <summary>
    /// Check if a specific level is unlocked
    /// </summary>
    /// <param name="levelNumber">Level number (1-based)</param>
    /// <returns>True if level is unlocked</returns>
    public bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber == 1) return true; // Level 1 is always unlocked
        if (levelNumber > totalLevels) return false; // Can't unlock levels that don't exist
        
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
        if (levelNumber > totalLevels) return false;
        
        string key = "LevelCompleted" + levelNumber;
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Unlock a specific level
    /// </summary>
    /// <param name="levelNumber">Level number to unlock (1-based)</param>
    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber <= 0 || levelNumber > totalLevels) 
        {
            Debug.LogWarning($"Cannot unlock level {levelNumber}. Valid range is 1-{totalLevels}");
            return;
        }

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
        if (levelNumber <= 0 || levelNumber > totalLevels) 
        {
            Debug.LogWarning($"Cannot complete level {levelNumber}. Valid range is 1-{totalLevels}");
            return;
        }

        // Mark level as completed
        string completedKey = "LevelCompleted" + levelNumber;
        PlayerPrefs.SetInt(completedKey, 1);
        
        // Unlock next level if it exists
        int nextLevel = levelNumber + 1;
        if (nextLevel <= totalLevels)
        {
            UnlockLevel(nextLevel);
            Debug.Log($"Level {levelNumber} completed! Next level {nextLevel} unlocked.");
        }
        else
        {
            Debug.Log($"Level {levelNumber} completed! This was the final level - Congratulations!");
        }
        
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Get the highest unlocked level (clamped to actual available levels)
    /// </summary>
    /// <returns>Highest unlocked level number</returns>
    public int GetHighestUnlockedLevel()
    {
        int highest = PlayerPrefs.GetInt(HIGHEST_UNLOCKED_KEY, 1);
        return Mathf.Min(highest, totalLevels); // Don't return higher than available levels
    }

    /// <summary>
    /// Load a level by number with validation
    /// </summary>
    /// <param name="levelNumber">Level number to load (1-based)</param>
    public void LoadLevel(int levelNumber)
    {
        // Validate level exists
        if (levelNumber <= 0 || levelNumber > totalLevels)
        {
            Debug.LogError($"Cannot load level {levelNumber}. Valid range is 1-{totalLevels}");
            LoadMenuScene();
            return;
        }

        // Check if level is unlocked
        if (!IsLevelUnlocked(levelNumber))
        {
            Debug.LogWarning($"Cannot load level {levelNumber} - it's still locked! Loading highest unlocked level instead.");
            LoadLevel(GetHighestUnlockedLevel());
            return;
        }

        string sceneName = "Level" + levelNumber;
        
        // Double-check scene exists before loading
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' not found! Loading menu instead.");
            LoadMenuScene();
            return;
        }

        Debug.Log($"Loading level {levelNumber} ({sceneName})");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Load the menu scene as fallback
    /// </summary>
    private void LoadMenuScene()
    {
        if (!string.IsNullOrEmpty(menuSceneName) && Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.Log($"Loading menu scene: {menuSceneName}");
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogError("Menu scene not found! Loading scene index 0");
            SceneManager.LoadScene(0);
        }
    }

    /// <summary>
    /// Load a level by scene name (with validation)
    /// </summary>
    /// <param name="sceneName">Scene name to load</param>
    public void LoadLevelByName(string sceneName)
    {
        // Check if scene exists
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' not found in Build Settings!");
            LoadMenuScene();
            return;
        }

        // Extract level number from scene name
        int levelNumber = ExtractLevelNumberFromName(sceneName);
        
        if (levelNumber > 0)
        {
            // Use the regular LoadLevel method for validation
            LoadLevel(levelNumber);
        }
        else
        {
            // Not a level scene, load directly (like menu scenes)
            Debug.Log($"Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
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
        
        return totalLevels > 0 ? (float)completedLevels / totalLevels * 100f : 0f;
    }

    /// <summary>
    /// Debug method to show current status
    /// </summary>
    [ContextMenu("Show Level Status")]
    private void ShowLevelStatus()
    {
        Debug.Log($"=== LEVEL MANAGER STATUS ===");
        Debug.Log($"Total Levels: {totalLevels}");
        Debug.Log($"Highest Unlocked: {GetHighestUnlockedLevel()}");
        Debug.Log($"Completion: {GetCompletionPercentage():F1}%");
        
        for (int i = 1; i <= totalLevels; i++)
        {
            string status = IsLevelCompleted(i) ? "Completed" : (IsLevelUnlocked(i) ? "Unlocked" : "Locked");
            Debug.Log($"Level {i}: {status}");
        }
        Debug.Log($"===========================");
    }
}