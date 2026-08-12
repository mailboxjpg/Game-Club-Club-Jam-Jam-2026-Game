using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance {get; private set;}

    [SerializeField] private CanvasGroup fadeScreenGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private Image difficultySliderHandle;
    [SerializeField] private Image difficultySliderFill;
    [SerializeField] private bool takeOverInstance;
    public Difficulty difficulty;

    private bool _loadingScene;
    private int _currentSceneIndex;

    private void Awake()
    {
        if (!takeOverInstance && Instance != null && Instance != this)
        {
            Debug.Log($"[{name}: SceneLoader] An instance already exists. Destroying this instance's gameObject.");
            Destroy(gameObject);
            return;
        }
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
        }
        if (PlayerControl.Instance != null)
            PlayerControl.Instance.Delete();
        if (CameraControl.Instance != null)
            CameraControl.Instance.Delete();
        Cursor.visible = true;
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (difficultySlider != null)
        {
            difficultySlider.onValueChanged.AddListener(DifficultySliderChanged);
            difficultySlider.value = 1;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this && difficultySlider != null)
            difficultySlider.onValueChanged.RemoveListener(DifficultySliderChanged);
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
            PlayerControl.Instance.shellCollector.numShells = 0;
            float sceneLoadHealth = difficulty switch
            {
                Difficulty.Easy => PlayerControl.Instance.healthSystem.GetMaxHealth(),
                Difficulty.Normal => PlayerControl.Instance.healthSystem.GetMaxHealth() * 0.5f,
                Difficulty.Hard => 0f,
                _ => 0f,
            };
            PlayerControl.Instance.healthSystem.AddHealth(sceneLoadHealth);
            Cursor.visible = false;
        }

        yield return StartCoroutine(Fade(0f));
    }

    public void FadeScreen(float alpha)
    {
        StartCoroutine(Fade(alpha));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeScreenGroup == null)
            yield break;

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

    private void DifficultySliderChanged(float value)
    {
        if (value <= 0)
        {
            // Easy
            difficulty = Difficulty.Easy;
            difficultySliderHandle.color = Color.green;
            difficultySliderFill.color = Color.green;
            difficultyText.text = "Difficulty: <color=green>Easy</color>";
        }
        else if (value <= 1)
        {
            // Normal
            difficulty = Difficulty.Normal;
            difficultySliderHandle.color = Color.yellow;
            difficultySliderFill.color = Color.yellow;
            difficultyText.text = "Difficulty: <color=yellow>Normal</color>";
        }
        else
        {
            // Hard
            difficulty = Difficulty.Hard;
            difficultySliderHandle.color = Color.red;
            difficultySliderFill.color = Color.red;
            difficultyText.text = "Difficulty: <color=red>Hard</color>";
        }
    }
}
