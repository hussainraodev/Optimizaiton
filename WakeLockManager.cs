using UnityEngine;

/// <summary>
/// Centralized WakeLock management for Google Play Store compliance.
/// Ensures screen stays on only during active gameplay to save battery.
/// 
/// Google Play Policy: Apps should not hold WakeLocks unnecessarily.
/// This manager automatically releases WakeLock when:
/// - Game is paused
/// - App goes to background
/// - Player is in menus
/// </summary>
public class WakeLockManager : MonoBehaviour
{
    public static WakeLockManager Instance { get; private set; }

    [Header("WakeLock Settings")]
    [Tooltip("Enable WakeLock only during active gameplay")]
    [SerializeField] private bool enableOnlyDuringGameplay = true;
    
    [Tooltip("Auto-release WakeLock when app goes to background")]
    [SerializeField] private bool releaseOnBackground = true;

    // Track WakeLock state
    private bool _isWakeLockEnabled = false;
    private bool _isGameplayActive = false;
    private bool _isPaused = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Start with WakeLock disabled (safe default)
        ReleaseWakeLock();
    }

    private void OnDestroy()
    {
        // Always release WakeLock on destroy
        ReleaseWakeLock();
        
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Called when app goes to background/foreground
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // App going to background - MUST release WakeLock (Google Play policy)
            if (releaseOnBackground)
            {
                Debug.Log("[WakeLock] App paused - releasing WakeLock");
                ReleaseWakeLock();
            }
        }
        else
        {
            // App coming to foreground - restore WakeLock if gameplay was active
            if (_isGameplayActive && !_isPaused)
            {
                Debug.Log("[WakeLock] App resumed - restoring WakeLock");
                AcquireWakeLock();
            }
        }
    }

    /// <summary>
    /// Called when app is about to quit
    /// </summary>
    private void OnApplicationQuit()
    {
        ReleaseWakeLock();
    }

    /// <summary>
    /// Call this when gameplay starts (player is actively driving)
    /// </summary>
    public void OnGameplayStarted()
    {
        _isGameplayActive = true;
        _isPaused = false;
        
        if (enableOnlyDuringGameplay)
        {
            AcquireWakeLock();
        }
        
        Debug.Log("[WakeLock] Gameplay started - WakeLock acquired");
    }

    /// <summary>
    /// Call this when gameplay ends (level complete, game over, etc.)
    /// </summary>
    public void OnGameplayEnded()
    {
        _isGameplayActive = false;
        ReleaseWakeLock();
        
        Debug.Log("[WakeLock] Gameplay ended - WakeLock released");
    }

    /// <summary>
    /// Call this when game is paused (pause menu opened)
    /// </summary>
    public void OnGamePaused()
    {
        _isPaused = true;
        ReleaseWakeLock();
        
        Debug.Log("[WakeLock] Game paused - WakeLock released");
    }

    /// <summary>
    /// Call this when game is resumed from pause
    /// </summary>
    public void OnGameResumed()
    {
        _isPaused = false;
        
        if (_isGameplayActive)
        {
            AcquireWakeLock();
        }
        
        Debug.Log("[WakeLock] Game resumed - WakeLock restored");
    }

    /// <summary>
    /// Call this when in main menu or other non-gameplay screens
    /// </summary>
    public void OnMenuScreen()
    {
        _isGameplayActive = false;
        _isPaused = false;
        ReleaseWakeLock();
        
        Debug.Log("[WakeLock] Menu screen - WakeLock released");
    }

    /// <summary>
    /// Acquires WakeLock (keeps screen on)
    /// </summary>
    private void AcquireWakeLock()
    {
        if (!_isWakeLockEnabled)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            _isWakeLockEnabled = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[WakeLock] ACQUIRED - Screen will stay on");
#endif
        }
    }

    /// <summary>
    /// Releases WakeLock (allows screen to sleep normally)
    /// </summary>
    private void ReleaseWakeLock()
    {
        if (_isWakeLockEnabled || Screen.sleepTimeout == SleepTimeout.NeverSleep)
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            _isWakeLockEnabled = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[WakeLock] RELEASED - Screen can sleep normally");
#endif
        }
    }

    /// <summary>
    /// Force release WakeLock (for emergency/cleanup)
    /// </summary>
    public void ForceRelease()
    {
        _isGameplayActive = false;
        _isPaused = false;
        ReleaseWakeLock();
    }

    /// <summary>
    /// Check if WakeLock is currently held
    /// </summary>
    public bool IsWakeLockActive()
    {
        return _isWakeLockEnabled;
    }

    /// <summary>
    /// Get current WakeLock status for debugging
    /// </summary>
    public string GetStatus()
    {
        return $"WakeLock: {(_isWakeLockEnabled ? "ON" : "OFF")} | Gameplay: {_isGameplayActive} | Paused: {_isPaused}";
    }
}

