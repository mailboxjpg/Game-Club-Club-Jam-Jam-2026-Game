using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class GlobalLight : MonoBehaviour
{
    [SerializeField] private Color minColor = Color.white;
    [SerializeField] private Color maxColor = Color.white;
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float maxIntensity = 1f;
    [SerializeField] private float lerpSpeed;
    [SerializeField] private LerpType lerpType;

    private Light2D _light;
    private float _x;
    private float _perlinSeed;

    public enum LerpType
    {
        Sin,
        PerlinNoise,
        PingPong,
        Wrap
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _light = GetComponent<Light2D>();
        _perlinSeed = Random.Range(-999f, 999f);
    }

    // Update is called once per frame
    private void Update()
    {
        _x += Time.deltaTime * lerpSpeed;
        var t = lerpType switch
        {
            LerpType.Sin => Mathf.Sin(_x),
            LerpType.PerlinNoise => Mathf.PerlinNoise1D(_x + _perlinSeed),
            LerpType.PingPong => Mathf.PingPong(_x, 1f),
            LerpType.Wrap => _x % 1f,
            _ => 0f,
        };
        _light.color = Color.Lerp(minColor, maxColor, t);
        _light.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}
