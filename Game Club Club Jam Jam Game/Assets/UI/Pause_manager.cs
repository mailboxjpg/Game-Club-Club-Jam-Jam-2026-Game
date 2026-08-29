using UnityEngine;
using UnityEngine.UI;

public class Pause_manager : MonoBehaviour
{
    public static Pause_manager Instance { get; private set; }
    bool game_paused = false;
    [SerializeField] GameObject menu;
    [SerializeField] Slider volumeSlider;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            print("destroyed duplicate pause menu");
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        volumeSlider.onValueChanged.AddListener(ChangeVolume);
        volumeSlider.SetValueWithoutNotify(AudioListener.volume);
    }

    private void OnDestroy()
    {
        volumeSlider.onValueChanged.RemoveListener(ChangeVolume);
    }

    private void Update()
    {
        if (PlayerControl.Instance != null && PlayerControl.Instance.inputActions.UI.Pause.WasPressedThisFrame())
        {
            toggle();
        }
    }

    private void toggle()
    {
        if (game_paused)
        {
            unpause();
        }
        else
        {
            pause();
        }
    }
    
    public void pause()
    {
        Cursor.visible = true;
        menu.SetActive(true);
        game_paused = true;
        Time.timeScale = 0;
    }

    public void unpause()
    {
        Cursor.visible = false;
        menu?.SetActive(false);
        game_paused = false;
        Time.timeScale = 1;
    }

    public void go_to_menu()
    {
        unpause();
        SceneLoader.Instance.LoadScene("TitleScreen");
    }

    private void ChangeVolume(float value)
    {
        AudioListener.volume = value;
    }
}
