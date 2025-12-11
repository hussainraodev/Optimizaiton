using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

/// <summary>
/// Automatic performance optimizer for low-end devices.
/// Detects device capabilities and adjusts quality settings accordingly.
/// Attach to a GameObject in your first scene (e.g., Splash Screen).
/// </summary>
public class PerformanceOptimizer : MonoBehaviour
{
    public static PerformanceOptimizer Instance { get; private set; }

    [Header("Performance Thresholds")]
    [Tooltip("RAM threshold in MB for low-end detection")]
    [SerializeField] private int lowEndRAMThreshold = 3000; // 3GB
    [Tooltip("Target FPS for the game")]
    [SerializeField] private int targetFPS = 60;
    [Tooltip("FPS threshold to trigger quality reduction")]
    [SerializeField] private int lowFPSThreshold = 45;
    
    [Header("Optimization Settings")]
    [SerializeField] private bool autoDetectOnStart = true;
    [SerializeField] private bool enableAdaptiveQuality = true;
    [SerializeField] private float adaptiveCheckInterval = 5f;
    
    [Header("Camera Settings")]
    [SerializeField] private float lowEndFarClipPlane = 200f;
    [SerializeField] private float normalFarClipPlane = 500f;
    
    // Performance tracking
    private float[] _fpsBuffer;
    private int _fpsBufferIndex;
    private const int FPS_BUFFER_SIZE = 30;
    private float _lastAdaptiveCheck;
    private int _currentQualityLevel;
    private bool _isLowEndDevice;
    
    // Cached references
    private Camera _mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        _fpsBuffer = new float[FPS_BUFFER_SIZE];
        _currentQualityLevel = QualitySettings.GetQualityLevel();
    }

    private void Start()
    {
        // Set target framerate
        Application.targetFrameRate = targetFPS;
        
        if (autoDetectOnStart)
        {
            DetectAndOptimize();
        }
    }

    private void Update()
    {
        // Track FPS
        UpdateFPSBuffer();
        
        // Adaptive quality check
        if (enableAdaptiveQuality && Time.time - _lastAdaptiveCheck > adaptiveCheckInterval)
        {
            _lastAdaptiveCheck = Time.time;
            AdaptiveQualityCheck();
        }
    }

    /// <summary>
    /// Detect device capabilities and apply appropriate optimizations.
    /// </summary>
    public void DetectAndOptimize()
    {
        int systemRAM = SystemInfo.systemMemorySize;
        int graphicsRAM = SystemInfo.graphicsMemorySize;
        string gpuName = SystemInfo.graphicsDeviceName.ToLower();
        int processorCount = SystemInfo.processorCount;
        
        Debug.Log($"[PerformanceOptimizer] Device Info:");
        Debug.Log($"  RAM: {systemRAM}MB, VRAM: {graphicsRAM}MB");
        Debug.Log($"  GPU: {gpuName}, Cores: {processorCount}");
        
        // Determine device tier
        DeviceTier tier = GetDeviceTier(systemRAM, graphicsRAM, gpuName, processorCount);
        
        // Apply optimizations based on tier
        ApplyOptimizationsForTier(tier);
        
        Debug.Log($"[PerformanceOptimizer] Device Tier: {tier}, Quality Level: {QualitySettings.GetQualityLevel()}");
    }

    private enum DeviceTier
    {
        UltraLow,   // 2GB RAM, very old GPU
        Low,        // 3GB RAM, old GPU
        Medium,     // 4GB RAM, decent GPU
        High        // 6GB+ RAM, good GPU
    }

    private DeviceTier GetDeviceTier(int ram, int vram, string gpu, int cores)
    {
        // Check for known low-end GPUs
        bool isLowEndGPU = gpu.Contains("mali-4") || gpu.Contains("mali-t") || 
                          gpu.Contains("adreno 3") || gpu.Contains("adreno 4") ||
                          gpu.Contains("powervr sgx") || gpu.Contains("tegra 3") ||
                          gpu.Contains("tegra 4") || vram < 512;
        
        // Ultra Low: Very old devices
        if (ram < 2500 || isLowEndGPU || cores <= 2)
        {
            _isLowEndDevice = true;
            return DeviceTier.UltraLow;
        }
        
        // Low: Budget devices
        if (ram < 4000 || vram < 1024 || cores <= 4)
        {
            _isLowEndDevice = true;
            return DeviceTier.Low;
        }
        
        // Medium: Standard devices
        if (ram < 6000 || vram < 2048)
        {
            _isLowEndDevice = false;
            return DeviceTier.Medium;
        }
        
        // High: Flagship devices
        _isLowEndDevice = false;
        return DeviceTier.High;
    }

    private void ApplyOptimizationsForTier(DeviceTier tier)
    {
        switch (tier)
        {
            case DeviceTier.UltraLow:
                ApplyUltraLowSettings();
                break;
            case DeviceTier.Low:
                ApplyLowSettings();
                break;
            case DeviceTier.Medium:
                ApplyMediumSettings();
                break;
            case DeviceTier.High:
                ApplyHighSettings();
                break;
        }
    }

    private void ApplyUltraLowSettings()
    {
        // Quality level 0 = Ultra Low
        QualitySettings.SetQualityLevel(0, true);
        _currentQualityLevel = 0;
        
        Application.targetFrameRate = 30; // Lower target for stability
        
        // Reduce draw distance
        SetCameraFarClipPlane(150f);
        
        // Disable expensive features
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowDistance = 0;
        QualitySettings.antiAliasing = 0;
        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.billboardsFaceCameraPosition = false;
        QualitySettings.skinWeights = SkinWeights.OneBone;
        QualitySettings.lodBias = 0.2f;
        QualitySettings.maximumLODLevel = 2; // Force lowest LOD
        QualitySettings.particleRaycastBudget = 4;
        
        // Reduce texture quality
        QualitySettings.globalTextureMipmapLimit = 2; // Quarter resolution
        
        // Disable vsync for max FPS
        QualitySettings.vSyncCount = 0;
        
        // Reduce physics
        Physics.defaultSolverIterations = 3;
        Physics.defaultSolverVelocityIterations = 1;
        
        Debug.Log("[PerformanceOptimizer] Applied ULTRA LOW settings");
    }

    private void ApplyLowSettings()
    {
        // Quality level 1 = Very Low
        QualitySettings.SetQualityLevel(1, true);
        _currentQualityLevel = 1;
        
        Application.targetFrameRate = 45;
        
        SetCameraFarClipPlane(lowEndFarClipPlane);
        
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.antiAliasing = 0;
        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.skinWeights = SkinWeights.TwoBones;
        QualitySettings.lodBias = 0.3f;
        QualitySettings.maximumLODLevel = 1;
        QualitySettings.particleRaycastBudget = 16;
        QualitySettings.globalTextureMipmapLimit = 1; // Half resolution
        QualitySettings.vSyncCount = 0;
        
        Physics.defaultSolverIterations = 4;
        
        Debug.Log("[PerformanceOptimizer] Applied LOW settings");
    }

    private void ApplyMediumSettings()
    {
        // Quality level 2 = Low (which is actually medium for mobile)
        QualitySettings.SetQualityLevel(2, true);
        _currentQualityLevel = 2;
        
        Application.targetFrameRate = 60;
        
        SetCameraFarClipPlane(300f);
        
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.antiAliasing = 0;
        QualitySettings.lodBias = 0.5f;
        QualitySettings.maximumLODLevel = 0;
        QualitySettings.globalTextureMipmapLimit = 0; // Full resolution
        QualitySettings.vSyncCount = 0;
        
        Debug.Log("[PerformanceOptimizer] Applied MEDIUM settings");
    }

    private void ApplyHighSettings()
    {
        // Quality level 3 = Medium
        QualitySettings.SetQualityLevel(3, true);
        _currentQualityLevel = 3;
        
        Application.targetFrameRate = 60;
        
        SetCameraFarClipPlane(normalFarClipPlane);
        
        QualitySettings.lodBias = 0.7f;
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.vSyncCount = 0;
        
        Debug.Log("[PerformanceOptimizer] Applied HIGH settings");
    }

    private void SetCameraFarClipPlane(float distance)
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;
        
        if (_mainCamera != null)
            _mainCamera.farClipPlane = distance;
        
        // Also find RCC camera if exists
        Camera[] allCameras = FindObjectsOfType<Camera>();
        foreach (var cam in allCameras)
        {
            if (cam.gameObject.name.Contains("RCC") || cam.CompareTag("MainCamera"))
            {
                cam.farClipPlane = distance;
            }
        }
    }

    private void UpdateFPSBuffer()
    {
        _fpsBuffer[_fpsBufferIndex] = 1f / Time.unscaledDeltaTime;
        _fpsBufferIndex = (_fpsBufferIndex + 1) % FPS_BUFFER_SIZE;
    }

    private float GetAverageFPS()
    {
        float sum = 0f;
        for (int i = 0; i < FPS_BUFFER_SIZE; i++)
        {
            sum += _fpsBuffer[i];
        }
        return sum / FPS_BUFFER_SIZE;
    }

    private void AdaptiveQualityCheck()
    {
        float avgFPS = GetAverageFPS();
        
        // If FPS is too low, reduce quality
        if (avgFPS < lowFPSThreshold && _currentQualityLevel > 0)
        {
            _currentQualityLevel--;
            QualitySettings.SetQualityLevel(_currentQualityLevel, true);
            Debug.Log($"[PerformanceOptimizer] Reduced quality to {_currentQualityLevel} (FPS: {avgFPS:F1})");
            
            // Apply additional optimizations
            if (_currentQualityLevel <= 1)
            {
                SetCameraFarClipPlane(lowEndFarClipPlane);
            }
        }
        // If FPS is stable and good, we could increase quality (optional)
        else if (avgFPS > 55 && _currentQualityLevel < 2 && !_isLowEndDevice)
        {
            // Don't auto-increase on low-end devices
            // Uncomment below to enable auto quality increase
            // _currentQualityLevel++;
            // QualitySettings.SetQualityLevel(_currentQualityLevel, true);
        }
    }

    /// <summary>
    /// Manually set quality level (0 = Ultra Low, 1 = Very Low, 2 = Low, etc.)
    /// </summary>
    public void SetQualityLevel(int level)
    {
        _currentQualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(_currentQualityLevel, true);
        Debug.Log($"[PerformanceOptimizer] Quality set to: {QualitySettings.names[_currentQualityLevel]}");
    }

    /// <summary>
    /// Get current device tier for UI display
    /// </summary>
    public string GetDeviceTierName()
    {
        return _isLowEndDevice ? "Low-End" : "Standard";
    }

    /// <summary>
    /// Force garbage collection (call sparingly, e.g., on scene transitions)
    /// </summary>
    public void ForceCleanup()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

