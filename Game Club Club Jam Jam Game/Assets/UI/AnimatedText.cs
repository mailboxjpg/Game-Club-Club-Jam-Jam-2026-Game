using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class AnimatedText : MonoBehaviour
{
    [Tooltip("Delay in seconds between adding characters.")]
    [SerializeField] private float characterRate = 0.1f;
    [SerializeField] private bool emptyOnAwake;
    [SerializeField] private bool setTargetTextOnAwake;
    [SerializeField] private bool playOnAwake = false;

    private TextMeshProUGUI _text;
    private Coroutine _animationRoutine;
    private string _originalFullText;
    private bool _skipped;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        if (setTargetTextOnAwake)
            _originalFullText = _text.text;
        if (emptyOnAwake)
            _text.text = "";
        if (playOnAwake)
            StartAnimation();
    }

    private void Update()
    {
        if (PlayerControl.Instance != null && PlayerControl.Instance.inputActions.UI.SkipPopup.WasPressedThisFrame())
        {
            SkipAnimation();
        }
    }

    public void StartAnimation()
    {
        if (_text == null)
        {
            _text = GetComponent<TextMeshProUGUI>();
        }
        if (_animationRoutine != null)
            StopCoroutine(_animationRoutine);
        _animationRoutine = StartCoroutine(Animation());
    }

    private IEnumerator Animation()
    {
        _text.text = "";
        for (int i = 0; i < _originalFullText.Length; i++)
        {
            _text.text += _originalFullText[i];
            yield return new WaitForSecondsRealtime(characterRate);
        }
        _animationRoutine = null;
    }

    private void SkipAnimation()
    {
        if (_animationRoutine == null)
            return;
        if (_skipped)
        {
            // Hide again on second skip
            _text.text = "";
            _skipped = false;
            return;
        }
        // Show full text on first skip
        StopCoroutine(_animationRoutine);
        _animationRoutine = null;
        _text.text = _originalFullText;
        _skipped = true;
    }

    public void SetFullText(string fullText)
    {
        _originalFullText = fullText;
    }
}
