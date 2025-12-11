using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Optimized scene loading with smooth progress and deferred initialization.
/// Attach this to your loading screen or use as a utility.
/// </summary>
public class SceneLoadingOptimizer : MonoBehaviour
{
    public static SceneLoadingOptimizer Instance { get; private set; }

    [Header("Loading UI (Optional - Auto-finds if not set)")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Image progressBar;
    [SerializeField] private Text progressText;
    
    [Header("Loading Settings")]
    [SerializeField] private float minimumLoadingTime = 0.5f; // Minimum time to show loading (prevents flashing)
    [SerializeField] private float smoothProgressSpeed = 3f;
    
    // Internal state
    private float _displayProgress;
    private float _targetProgress;
    private bool _isLoading;
    private AsyncOperation _asyncOp;
    
    // Cached WaitForEndOfFrame for smooth updates
    private static readonly WaitForEndOfFrame _waitFrame = new WaitForEndOfFrame();
    private static readonly WaitForSecondsRealtime _wait01 = new WaitForSecondsRealtime(0.1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Smooth progress bar animation
        if (_isLoading && progressBar != null)
        {
            _displayProgress = Mathf.MoveTowards(_displayProgress, _targetProgress, smoothProgressSpeed * Time.unscaledDeltaTime);
            progressBar.fillAmount = _displayProgress;
            
            if (progressText != null)
            {
                progressText.text = Mathf.RoundToInt(_displayProgress * 100f) + "%";
            }
        }
    }

    /// <summary>
    /// Load scene asynchronously with optimized loading and smooth progress
    /// </summary>
    public static void LoadScene(string sceneName, GameObject loadingScreenOverride = null, Image progressBarOverride = null)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.LoadSceneOptimized(sceneName, loadingScreenOverride, progressBarOverride));
        }
        else
        {
            // Fallback if no instance
            SceneManager.LoadSceneAsync(sceneName);
        }
    }

    /// <summary>
    /// Load scene by index
    /// </summary>
    public static void LoadScene(int sceneIndex, GameObject loadingScreenOverride = null, Image progressBarOverride = null)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.LoadSceneOptimizedByIndex(sceneIndex, loadingScreenOverride, progressBarOverride));
        }
        else
        {
            SceneManager.LoadSceneAsync(sceneIndex);
        }
    }

    private IEnumerator LoadSceneOptimized(string sceneName, GameObject loadingScreenOverride, Image progressBarOverride)
    {
        yield return LoadSceneInternal(sceneName, -1, loadingScreenOverride, progressBarOverride);
    }

    private IEnumerator LoadSceneOptimizedByIndex(int sceneIndex, GameObject loadingScreenOverride, Image progressBarOverride)
    {
        yield return LoadSceneInternal(null, sceneIndex, loadingScreenOverride, progressBarOverride);
    }

    private IEnumerator LoadSceneInternal(string sceneName, int sceneIndex, GameObject loadingScreenOverride, Image progressBarOverride)
    {
        _isLoading = true;
        _displayProgress = 0f;
        _targetProgress = 0f;
        
        // Use overrides or defaults
        GameObject activeLoadingScreen = loadingScreenOverride ?? loadingScreen;
        Image activeProgressBar = progressBarOverride ?? progressBar;
        
        // Show loading screen
        if (activeLoadingScreen != null)
        {
            activeLoadingScreen.SetActive(true);
        }
        
        if (activeProgressBar != null)
        {
            activeProgressBar.fillAmount = 0f;
            progressBar = activeProgressBar; // Set for Update smoothing
        }
        
        float startTime = Time.realtimeSinceStartup;
        
        // Pre-cleanup before loading new scene
        yield return StartCoroutine(PreLoadCleanup());
        _targetProgress = 0.1f;
        
        // Start async scene loading
        if (!string.IsNullOrEmpty(sceneName))
        {
            _asyncOp = SceneManager.LoadSceneAsync(sceneName);
        }
        else
        {
            _asyncOp = SceneManager.LoadSceneAsync(sceneIndex);
        }
        
        _asyncOp.allowSceneActivation = false;
        
        // Update progress while loading
        while (_asyncOp.progress < 0.9f)
        {
            // Map 0-0.9 progress to 0.1-0.9 display progress
            _targetProgress = 0.1f + (_asyncOp.progress / 0.9f) * 0.8f;
            yield return _wait01;
        }
        
        _targetProgress = 0.95f;
        
        // Ensure minimum loading time for smooth UX
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSecondsRealtime(minimumLoadingTime - elapsedTime);
        }
        
        // Finish progress
        _targetProgress = 1f;
        
        // Wait for progress bar to finish animating
        while (_displayProgress < 0.99f)
        {
            yield return null;
        }
        
        // Activate the scene
        _asyncOp.allowSceneActivation = true;
        
        _isLoading = false;
        _asyncOp = null;
    }

    /// <summary>
    /// Pre-load cleanup to free memory before loading new scene
    /// </summary>
    private IEnumerator PreLoadCleanup()
    {
        // Clear caches
        try
        {
            GameManager.Instance?.ClearCache();
            GSF_AdsManager.ClearCache();
        }
        catch (System.Exception) { /* Ignore */ }
        
        // Request async unload of unused assets (non-blocking)
        AsyncOperation unloadOp = Resources.UnloadUnusedAssets();
        
        // Don't wait for full unload, just start it
        yield return null;
    }

    private void OnDestroy()
    {
        _asyncOp = null;
        StopAllCoroutines();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

