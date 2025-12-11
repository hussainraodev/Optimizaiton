using UnityEngine;

/// <summary>
/// Initializes all optimization systems on game start.
/// Place this script on a GameObject in your first scene (Splash Screen).
/// This ensures PerformanceOptimizer, MemoryCleanupManager, and SceneLoadingOptimizer
/// are created and persist across all scenes.
/// </summary>
public class GameOptimizationBootstrap : MonoBehaviour
{
    [Header("Optimization Prefabs (Optional)")]
    [Tooltip("If set, these prefabs will be instantiated. Otherwise, scripts will auto-create.")]
    [SerializeField] private GameObject performanceOptimizerPrefab;
    [SerializeField] private GameObject memoryCleanupManagerPrefab;
    [SerializeField] private GameObject sceneLoadingOptimizerPrefab;
    
    [Header("Settings")]
    [SerializeField] private bool enablePerformanceOptimizer = true;
    [SerializeField] private bool enableMemoryCleanup = true;
    [SerializeField] private bool enableSceneLoadingOptimizer = true;
    [SerializeField] private bool logInitialization = true;

    private void Awake()
    {
        InitializeOptimizationSystems();
    }

    private void InitializeOptimizationSystems()
    {
        // Initialize Performance Optimizer
        if (enablePerformanceOptimizer && PerformanceOptimizer.Instance == null)
        {
            if (performanceOptimizerPrefab != null)
            {
                Instantiate(performanceOptimizerPrefab);
            }
            else
            {
                CreatePerformanceOptimizer();
            }
            
            if (logInitialization)
            {
                Debug.Log("[GameOptimizationBootstrap] PerformanceOptimizer initialized");
            }
        }

        // Initialize Memory Cleanup Manager
        if (enableMemoryCleanup && MemoryCleanupManager.Instance == null)
        {
            if (memoryCleanupManagerPrefab != null)
            {
                Instantiate(memoryCleanupManagerPrefab);
            }
            else
            {
                CreateMemoryCleanupManager();
            }
            
            if (logInitialization)
            {
                Debug.Log("[GameOptimizationBootstrap] MemoryCleanupManager initialized");
            }
        }

        // Initialize Scene Loading Optimizer
        if (enableSceneLoadingOptimizer && SceneLoadingOptimizer.Instance == null)
        {
            if (sceneLoadingOptimizerPrefab != null)
            {
                Instantiate(sceneLoadingOptimizerPrefab);
            }
            else
            {
                CreateSceneLoadingOptimizer();
            }
            
            if (logInitialization)
            {
                Debug.Log("[GameOptimizationBootstrap] SceneLoadingOptimizer initialized");
            }
        }

        if (logInitialization)
        {
            Debug.Log("[GameOptimizationBootstrap] All optimization systems initialized");
        }
    }

    private void CreatePerformanceOptimizer()
    {
        GameObject go = new GameObject("PerformanceOptimizer");
        go.AddComponent<PerformanceOptimizer>();
    }

    private void CreateMemoryCleanupManager()
    {
        GameObject go = new GameObject("MemoryCleanupManager");
        go.AddComponent<MemoryCleanupManager>();
    }

    private void CreateSceneLoadingOptimizer()
    {
        GameObject go = new GameObject("SceneLoadingOptimizer");
        go.AddComponent<SceneLoadingOptimizer>();
    }

    /// <summary>
    /// Static method to manually trigger optimization initialization
    /// Call this from any script if bootstrap wasn't placed in first scene
    /// </summary>
    public static void EnsureInitialized()
    {
        // Performance Optimizer
        if (PerformanceOptimizer.Instance == null)
        {
            GameObject po = new GameObject("PerformanceOptimizer");
            po.AddComponent<PerformanceOptimizer>();
        }

        // Memory Cleanup Manager
        if (MemoryCleanupManager.Instance == null)
        {
            GameObject mcm = new GameObject("MemoryCleanupManager");
            mcm.AddComponent<MemoryCleanupManager>();
        }

        // Scene Loading Optimizer
        if (SceneLoadingOptimizer.Instance == null)
        {
            GameObject slo = new GameObject("SceneLoadingOptimizer");
            slo.AddComponent<SceneLoadingOptimizer>();
        }
    }
}

