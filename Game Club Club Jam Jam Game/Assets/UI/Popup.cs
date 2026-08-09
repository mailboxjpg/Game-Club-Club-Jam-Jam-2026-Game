using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Canvas))]
public class Popup : MonoBehaviour
{
    [SerializeField] private RectTransform popupBox;
    [Tooltip("Time in seconds for fade in/fade out.")]
    public float fadeTime;
    [Tooltip("Seconds the popup is active before getting destroyed.")]
    public float activeTime;
    [Tooltip("Alpha of the popup box lerps between min (x) and max (y).")]
    public Vector2 fadeAlphaRange;
    [Tooltip("Popup drifts with this velocity (world space only).")]
    public Vector3 driftVelocity;
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("If set, popup stays anchored at the target's world position+worldSpaceOffset. Otherwise stays at screen pos.")]
    public Transform worldSpaceTarget;
    [SerializeField] private Vector3 worldSpaceOffset;
    [SerializeField] private bool popupOnStart;
    [SerializeField] private bool destroyOnEnd = true;

    private bool _hadWorldSpaceTarget;
    private bool _wasEnabled = false;
    private Vector3 _worldSpacePosition;
    private AnimatedText _animatedText;
    private TextMeshProUGUI _textComp;

    public UnityEvent OnPopupStart;
    public UnityEvent OnPopupEnd;

    private void Start()
    {
        Init();
        if (popupOnStart)
            StartPopup();
    }

    public void Init()
    {
        _animatedText = GetComponentInChildren<AnimatedText>();
        _textComp = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void StartPopup()
    {
        if (_wasEnabled)
            return;
        StartCoroutine(PopupRoutine());
    }

    private IEnumerator PopupRoutine()
    {
        if (worldSpaceTarget != null)
        {
            _hadWorldSpaceTarget = true;
            _worldSpacePosition = worldSpaceTarget.position + worldSpaceOffset;
        }
        _wasEnabled = true;
        OnPopupStart?.Invoke();

        // Fade in
        float t = fadeTime;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(fadeAlphaRange.x, fadeAlphaRange.y, t);
            if (worldSpaceTarget != null)
            {
                _worldSpacePosition = worldSpaceTarget.position + worldSpaceOffset;
                popupBox.position = Camera.main.WorldToScreenPoint(_worldSpacePosition);
            }
            else if (_hadWorldSpaceTarget)
            {
                popupBox.position = Camera.main.WorldToScreenPoint(_worldSpacePosition);
            }
            _worldSpacePosition += driftVelocity * Time.deltaTime;
            yield return null;
        }
        
        // Active time
        t = activeTime;
        canvasGroup.alpha = fadeAlphaRange.y;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            if (worldSpaceTarget != null)
            {
                _worldSpacePosition = worldSpaceTarget.position + worldSpaceOffset;
                popupBox.position = Camera.main.WorldToScreenPoint(_worldSpacePosition);
            }
            else if (_hadWorldSpaceTarget)
            {
                popupBox.position = Camera.main.WorldToScreenPoint(_worldSpacePosition);
            }
            _worldSpacePosition += driftVelocity * Time.deltaTime;
            yield return null;
        }

        // Fade out
        t = fadeTime;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(fadeAlphaRange.x, fadeAlphaRange.y, t);
            if (worldSpaceTarget != null)
            {
                _worldSpacePosition = worldSpaceTarget.position + worldSpaceOffset;
                popupBox.position = Camera.main.WorldToScreenPoint(_worldSpacePosition);
            }
            else if (_hadWorldSpaceTarget)
            {
                popupBox.position = Camera.main.WorldToScreenPoint(_worldSpacePosition);
            }
            _worldSpacePosition += driftVelocity * Time.deltaTime;
            yield return null;
        }
        OnPopupEnd?.Invoke();
        _hadWorldSpaceTarget = false;
        _wasEnabled = false;
        if (destroyOnEnd)
            Destroy(gameObject);
    }

    public void SetText(string text)
    {
        if (_animatedText != null)
            _animatedText.SetFullText(text);
        else if (_textComp != null)
            _textComp.text = text;
    }
}
