using UnityEngine;

public class Pause_manager : MonoBehaviour
{
    public static Pause_manager Instance { get; private set; }
    bool game_paused = false;
    [SerializeField] GameObject menu;

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
    }
    public void toggle()
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
        menu.SetActive(true);
        game_paused = true;
        Time.timeScale = 0;
    }

    public void unpause()
    {
        menu?.SetActive(false);
        game_paused = false;
        Time.timeScale = 1;
    }
}
