using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// Memory Cleanup Manager - Handles automatic memory cleanup on scene transitions
/// Attach to a DontDestroyOnLoad object or call static methods directly
/// </summary>
public class MemoryCleanupManager : MonoBehaviour
{
    public static MemoryCleanupManager Instance { get; private set; }
    
    [Header("Settings")]
    [Tooltip("Automatically clean up memory on scene changes")]
    [SerializeField] private bool autoCleanupOnSceneChange = true;
    
    [Tooltip("Force GC collection after unloading (use sparingly)")]
    [SerializeField] private bool forceGCOnCleanup = false;
    
    [Tooltip("Delay before cleanup after scene load")]
    [SerializeField] private float cleanupDelay = 0.5f;

    private static readonly WaitForSecondsRealtime _cleanupWait = new WaitForSecondsRealtime(0.5f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to scene events
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
            Instance = null;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (autoCleanupOnSceneChange)
        {
            // Clear GameManager cache immediately
            try { GameManager.Instance?.ClearCache(); }
            catch (Exception) { /* Ignore */ }
            
            // Clear GSF_AdsManager cache
            try { GSF_AdsManager.ClearCache(); }
            catch (Exception) { /* Ignore */ }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (autoCleanupOnSceneChange)
        {
            StartCoroutine(DelayedCleanup());
        }
    }

    private IEnumerator DelayedCleanup()
    {
        yield return _cleanupWait;
        CleanupMemory();
    }

    /// <summary>
    /// Call this to manually trigger memory cleanup
    /// </summary>
    public static void CleanupMemory()
    {
        // Unload unused assets
        Resources.UnloadUnusedAssets();
        
        // Force GC if enabled (use sparingly - can cause frame spike)
        if (Instance != null && Instance.forceGCOnCleanup)
        {
            GC.Collect();
        }
        
        Debug.Log("Memory cleanup completed");
    }

    /// <summary>
    /// Call this for aggressive cleanup (use only when necessary, e.g., after heavy scenes)
    /// </summary>
    public static void AggressiveCleanup()
    {
        // Clear all caches
        try { GameManager.Instance?.ClearCache(); }
        catch (Exception) { /* Ignore */ }
        
        try { GSF_AdsManager.ClearCache(); }
        catch (Exception) { /* Ignore */ }

        // Unload unused assets
        Resources.UnloadUnusedAssets();
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        Debug.Log("[MemoryCleanupManager] Aggressive memory cleanup completed");
    }

    /// <summary>
    /// Get current memory usage for debugging
    /// </summary>
    public static long GetTotalMemoryMB()
    {
        return GC.GetTotalMemory(false) / (1024 * 1024);
    }

    /// <summary>
    /// Log current memory stats
    /// </summary>
    public static void LogMemoryStats()
    {
        long totalMemory = GC.GetTotalMemory(false);
        Debug.Log($"[Memory] Managed Heap: {totalMemory / (1024 * 1024)} MB | " +
                  $"Texture Memory: {Texture.currentTextureMemory / (1024 * 1024)} MB | " +
                  $"Total Allocated: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)} MB");
    }
}

