using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    #region Fields
    [SerializeField] private bool logTransitions = true;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private string startupSceneName = string.Empty;
    [SerializeField] private bool loadStartupSceneOnStart = false;
    #endregion

    #region Private Members
    private static SceneController instance;
    private AsyncOperation activeOperation;
    #endregion

    #region Getters
    public static SceneController Instance => instance;
    public bool IsLoading => activeOperation != null && !activeOperation.isDone;
    public string ActiveSceneName => SceneManager.GetActiveScene().name;
    #endregion

    #region Unity Message Methods
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (!loadStartupSceneOnStart)
        {
            return;
        }

        LoadScene(startupSceneName);
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        instance = null;
    }
    #endregion

    #region Public Methods
    public bool LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (!CanLoad(sceneName))
        {
            return false;
        }

        activeOperation = null;
        SceneManager.LoadScene(sceneName, mode);
        LogTransition(sceneName, mode, false);

        return true;
    }

    public bool LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, Action<AsyncOperation> onStarted = null)
    {
        if (!CanLoad(sceneName))
        {
            return false;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);

        if (operation == null)
        {
            return false;
        }

        RegisterOperation(operation);
        LogTransition(sceneName, mode, true);

        if (onStarted != null)
        {
            onStarted.Invoke(operation);
        }

        return true;
    }

    public bool ReloadActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(sceneName))
        {
            return false;
        }

        return LoadScene(sceneName);
    }

    public bool LoadNextScene()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            return false;
        }

        string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);

        if (string.IsNullOrEmpty(nextScenePath))
        {
            return false;
        }

        string nextSceneName = Path.GetFileNameWithoutExtension(nextScenePath);

        if (string.IsNullOrEmpty(nextSceneName))
        {
            return false;
        }

        return LoadScene(nextSceneName);
    }
    #endregion

    #region Private Methods
    private bool CanLoad(string sceneName)
    {
        if (IsLoading)
        {
            return false;
        }

        return !string.IsNullOrEmpty(sceneName);
    }

    private void RegisterOperation(AsyncOperation operation)
    {
        activeOperation = operation;
        activeOperation.completed += HandleOperationCompleted;
    }

    private void HandleOperationCompleted(AsyncOperation operation)
    {
        operation.completed -= HandleOperationCompleted;
        activeOperation = null;
    }

    private void LogTransition(string sceneName, LoadSceneMode mode, bool isAsync)
    {
        if (!logTransitions)
        {
            return;
        }

        string asyncLabel = isAsync ? "async" : "sync";
        Debug.Log($"[SceneController] {asyncLabel} load: {sceneName} ({mode})");
    }
    #endregion
}
