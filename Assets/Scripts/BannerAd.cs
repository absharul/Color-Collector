using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class BannerAd : MonoBehaviour
{
    [Header("AdMob Banner Settings")]
    [SerializeField] private string androidBannerAdUnitId = "ca-app-pub-2230128831363710~7982869470"; // Test ID
    [SerializeField] private string iOSBannerAdUnitId = ""; // Test ID
    [SerializeField] private bool useTestAds = true; // Use test ads for development
    
    [Header("Banner Configuration")]
    [SerializeField] private AdPosition bannerPosition = AdPosition.Bottom;
    [SerializeField] private AdSize bannerSize = AdSize.Banner; // 320x50
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private bool autoRetryOnFailure = true;
    [SerializeField] private float retryDelaySeconds = 30f;
    
    [Header("UI Layout Adjustment")]
    [SerializeField] private RectTransform mainUIContainer; // Optional: UI to adjust for banner
    [SerializeField] private GameObject bannerPlaceholder; // Optional: Placeholder UI element
    [SerializeField] private float bannerHeightPixels = 50f; // Standard banner height
    
    // Events
    public static event Action OnBannerLoaded;
    public static event Action<string> OnBannerLoadFailed;
    public static event Action OnBannerClicked;
    
    // Private fields
    private BannerView bannerView;
    private bool isInitialized = false;
    private bool isBannerRequested = false;
    
    // Singleton pattern (optional)
    public static BannerAd Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeAdMob();
    }
    
    private void Start()
    {
        if (showOnStart)
        {
            ShowBannerAd();
        }
    }
    
    /// <summary>
    /// Initialize Google Mobile Ads SDK
    /// </summary>
    private void InitializeAdMob()
    {
        Debug.Log("[BannerAd] Initializing AdMob SDK...");
        
        MobileAds.Initialize(initStatus =>
        {
            isInitialized = true;
            Debug.Log("[BannerAd] AdMob SDK initialized successfully");
            
            // If banner was requested before initialization, create it now
            if (isBannerRequested)
            {
                CreateBannerAd();
            }
        });
    }
    
    /// <summary>
    /// Show the banner ad (creates if doesn't exist)
    /// </summary>
    public void ShowBannerAd()
    {
        if (bannerView != null)
        {
            // Banner already exists, just show it
            bannerView.Show();
            Debug.Log("[BannerAd] Showing existing banner ad");
            AdjustUILayout(true);
            return;
        }
        
        // Mark that banner was requested
        isBannerRequested = true;
        
        if (isInitialized)
        {
            CreateBannerAd();
        }
        else
        {
            Debug.Log("[BannerAd] Banner requested but SDK not initialized yet. Will create after initialization.");
        }
    }
    
    /// <summary>
    /// Hide the banner ad
    /// </summary>
    public void HideBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
            Debug.Log("[BannerAd] Banner ad hidden");
        }
        
        AdjustUILayout(false);
        
        if (bannerPlaceholder != null)
        {
            bannerPlaceholder.SetActive(false);
        }
    }
    
    /// <summary>
    /// Destroy the banner ad completely
    /// </summary>
    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
            Debug.Log("[BannerAd] Banner ad destroyed");
        }
        
        isBannerRequested = false;
        AdjustUILayout(false);
        
        if (bannerPlaceholder != null)
        {
            bannerPlaceholder.SetActive(false);
        }
    }
    
    /// <summary>
    /// Create and load the banner ad
    /// </summary>
    private void CreateBannerAd()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[BannerAd] Cannot create banner - SDK not initialized");
            return;
        }
        
        // Destroy existing banner if any
        if (bannerView != null)
        {
            bannerView.Destroy();
        }
        
        // Get platform-specific ad unit ID
        string adUnitId = GetBannerAdUnitId();
        
        // Create banner view
        bannerView = new BannerView(adUnitId, bannerSize, bannerPosition);
        
        // Register for ad events
        RegisterBannerEvents();
        
        // Create ad request and load
        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);
        
        Debug.Log($"[BannerAd] Created and loading banner ad with ID: {adUnitId}");
    }
    
    /// <summary>
    /// Get the appropriate ad unit ID for current platform
    /// </summary>
    private string GetBannerAdUnitId()
    {
        if (useTestAds)
        {
#if UNITY_ANDROID
            return androidBannerAdUnitId;
#elif UNITY_IOS
            return iOSBannerAdUnitId;
#else
            return androidBannerAdUnitId; // Fallback for editor
#endif
        }
        else
        {
            // Replace these with your actual production ad unit IDs
#if UNITY_ANDROID
            return "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX"; // Your Android Banner Ad Unit ID
#elif UNITY_IOS
            return "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX"; // Your iOS Banner Ad Unit ID
#else
            return "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX"; // Fallback
#endif
        }
    }
    
    /// <summary>
    /// Register all banner ad event handlers
    /// </summary>
    private void RegisterBannerEvents()
    {
        if (bannerView == null) return;
        
        bannerView.OnBannerAdLoaded += OnBannerAdLoaded;
        bannerView.OnBannerAdLoadFailed += OnBannerAdLoadFailed;
        bannerView.OnAdClicked += OnBannerAdClicked;
        bannerView.OnAdFullScreenContentOpened += OnBannerAdFullScreenOpened;
        bannerView.OnAdFullScreenContentClosed += OnBannerAdFullScreenClosed;
        bannerView.OnAdPaid += OnBannerAdPaid;
    }
    
    /// <summary>
    /// Adjust UI layout to accommodate banner ad
    /// </summary>
    private void AdjustUILayout(bool showingBanner)
    {
        if (mainUIContainer != null)
        {
            if (showingBanner)
            {
                // Add padding to avoid banner overlap
                switch (bannerPosition)
                {
                    case AdPosition.Bottom:
                    case AdPosition.BottomLeft:
                    case AdPosition.BottomRight:
                        mainUIContainer.offsetMin = new Vector2(mainUIContainer.offsetMin.x, bannerHeightPixels);
                        break;
                    case AdPosition.Top:
                    case AdPosition.TopLeft:
                    case AdPosition.TopRight:
                        mainUIContainer.offsetMax = new Vector2(mainUIContainer.offsetMax.x, -bannerHeightPixels);
                        break;
                }
            }
            else
            {
                // Reset padding
                switch (bannerPosition)
                {
                    case AdPosition.Bottom:
                    case AdPosition.BottomLeft:
                    case AdPosition.BottomRight:
                        mainUIContainer.offsetMin = new Vector2(mainUIContainer.offsetMin.x, 0);
                        break;
                    case AdPosition.Top:
                    case AdPosition.TopLeft:
                    case AdPosition.TopRight:
                        mainUIContainer.offsetMax = new Vector2(mainUIContainer.offsetMax.x, 0);
                        break;
                }
            }
        }
        
        // Show/hide placeholder
        if (bannerPlaceholder != null)
        {
            bannerPlaceholder.SetActive(showingBanner);
        }
    }
    
    #region Banner Ad Event Handlers
    
    private void OnBannerAdLoaded()
    {
        Debug.Log("[BannerAd] Banner ad loaded successfully");
        AdjustUILayout(true);
        OnBannerLoaded?.Invoke();
    }
    
    private void OnBannerAdLoadFailed(LoadAdError error)
    {
        Debug.LogError($"[BannerAd] Banner ad failed to load: {error.GetMessage()}");
        AdjustUILayout(false);
        OnBannerLoadFailed?.Invoke(error.GetMessage());
        
        // Auto retry if enabled
        if (autoRetryOnFailure)
        {
            Invoke(nameof(RetryLoadBanner), retryDelaySeconds);
        }
    }
    
    private void OnBannerAdClicked()
    {
        Debug.Log("[BannerAd] Banner ad clicked");
        OnBannerClicked?.Invoke();
    }
    
    private void OnBannerAdFullScreenOpened()
    {
        Debug.Log("[BannerAd] Banner ad full screen content opened");
    }
    
    private void OnBannerAdFullScreenClosed()
    {
        Debug.Log("[BannerAd] Banner ad full screen content closed");
    }
    
    private void OnBannerAdPaid(AdValue adValue)
    {
        Debug.Log($"[BannerAd] Banner ad paid: {adValue.Value} {adValue.CurrencyCode}");
    }
    
    #endregion
    
    /// <summary>
    /// Retry loading banner ad after failure
    /// </summary>
    private void RetryLoadBanner()
    {
        Debug.Log("[BannerAd] Retrying to load banner ad...");
        CreateBannerAd();
    }
    
    /// <summary>
    /// Check if banner is currently loaded and showing
    /// </summary>
    public bool IsBannerShowing()
    {
        return bannerView != null;
    }
    
    /// <summary>
    /// Set new ad unit IDs (useful for switching between test and production)
    /// </summary>
    public void SetAdUnitIds(string androidId, string iOSId, bool isTestMode = true)
    {
        androidBannerAdUnitId = androidId;
        iOSBannerAdUnitId = iOSId;
        useTestAds = isTestMode;
        
        // Recreate banner with new IDs
        if (isBannerRequested)
        {
            DestroyBannerAd();
            ShowBannerAd();
        }
    }
    
    /// <summary>
    /// Change banner position
    /// </summary>
    public void SetBannerPosition(AdPosition newPosition)
    {
        if (bannerPosition != newPosition)
        {
            bannerPosition = newPosition;
            
            // Recreate banner with new position
            if (isBannerRequested)
            {
                DestroyBannerAd();
                ShowBannerAd();
            }
        }
    }
    
    #region Context Menu (Development Tools)
    
    [ContextMenu("Show Banner")]
    private void DebugShowBanner()
    {
        ShowBannerAd();
    }
    
    [ContextMenu("Hide Banner")]
    private void DebugHideBanner()
    {
        HideBannerAd();
    }
    
    [ContextMenu("Destroy Banner")]
    private void DebugDestroyBanner()
    {
        DestroyBannerAd();
    }
    
    [ContextMenu("Toggle Test/Production Ads")]
    private void DebugToggleTestAds()
    {
        useTestAds = !useTestAds;
        Debug.Log($"[BannerAd] Switched to {(useTestAds ? "Test" : "Production")} ads");
        
        // Recreate banner with new mode
        if (isBannerRequested)
        {
            DestroyBannerAd();
            ShowBannerAd();
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Clean up banner ad
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
        
        // Clear singleton reference
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // Handle app pause/resume for better ad performance
        if (pauseStatus)
        {
            Debug.Log("[BannerAd] App paused");
        }
        else
        {
            Debug.Log("[BannerAd] App resumed");
        }
    }
}