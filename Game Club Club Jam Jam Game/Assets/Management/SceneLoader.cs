using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance {get; private set;}

    [SerializeField] private CanvasGroup fadeScreenGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool _loadingScene;
    private int _currentSceneIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log($"[{name}: SceneLoader] An instance already exists. Destroying this instance's gameObject.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadNextScene()
    {
        _currentSceneIndex++;
        if (_currentSceneIndex >= SceneManager.sceneCountInBuildSettings)
            _currentSceneIndex = 0;
        LoadScene(_currentSceneIndex);
    }

    public void LoadScene(string sceneName)
    {
        if (_loadingScene)
            return;
        StartCoroutine(TransitionRoutine(sceneName));
    }
    
    public void LoadScene(int buildIndex)
    {
        if (_loadingScene)
            return;
        StartCoroutine(TransitionRoutine(SceneManager.GetSceneByBuildIndex(buildIndex).name));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        _loadingScene = true;
        if (PlayerControl.Instance != null)
            PlayerControl.Instance.enabled = false;

        // 2. Fade to black
        yield return StartCoroutine(Fade(1f));

        // 3. Load scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (PlayerControl.Instance != null)
            PlayerControl.Instance.enabled = true;
        _loadingScene = false;
        if (sceneName.Equals("TitleScreen") || sceneName.Equals("EndScreen"))
        {
            Cursor.visible = true;
            PlayerControl.Instance.Delete();
            CameraControl.Instance.Delete();
        }
        else
        {
            Cursor.visible = false;
        }

        yield return StartCoroutine(Fade(0f));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeScreenGroup == null) yield break;

        float startAlpha = fadeScreenGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeScreenGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeScreenGroup.alpha = targetAlpha;
    }
}
