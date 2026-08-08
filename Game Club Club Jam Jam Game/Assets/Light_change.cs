using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Light_change : MonoBehaviour
{
    Light2D player_light;
    [SerializeField] float intensity;
    [SerializeField] float falloff_strength;
    //[SerializeField] Color color;
    [SerializeField] float change_interval;
    float time;

    Light_change[] light_changers;

    private void Start()
    {
        player_light = PlayerControl.Instance.GetComponentInChildren<Light2D>();
        light_changers = GameObject.FindObjectsByType<Light_change>(FindObjectsSortMode.None);
    }

    public void change_light()
    {
        foreach (Light_change i in light_changers)
        {
            i.cancel_light_change();
        }
        time = change_interval;
    }

    public void cancel_light_change()
    {
        time = 0;
    }

    private void Update()
    {
        if (time > 0)
        {
            float progress = 1 - time / change_interval;
            player_light.intensity = Mathf.Lerp(player_light.intensity, intensity, progress);
            player_light.falloffIntensity = Mathf.Lerp(player_light.falloffIntensity, falloff_strength, progress);
            //player_light.color = Color.Lerp(player_light.color, color, progress);
            time -= Time.deltaTime;
        }
    }
}
