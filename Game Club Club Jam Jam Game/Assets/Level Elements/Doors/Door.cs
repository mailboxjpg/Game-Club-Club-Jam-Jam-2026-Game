using UnityEngine;
using UnityEngine.Events;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform openedState;
    [SerializeField] private Transform closedState;
    [Tooltip("Transform of the model to apply the lerp to.")]
    [SerializeField] private Transform model;
    [SerializeField] private float lerpSpeed = 1f;
    [Tooltip("Min number of shells needed to be collected before opening.")]
    [SerializeField] private int minShells = 1;

    private bool open = false;
    private float timer = 0f;

    public UnityEvent OnOpen;

    private void Start()
    {
        model.SetPositionAndRotation(closedState.position, closedState.rotation);
        PlayerControl.Instance.shellCollector.OnCollect += TryOpen;
    }

    private void OnDestroy()
    {
        PlayerControl.Instance.shellCollector.OnCollect -= TryOpen;
    }

    private void Update()
    {
        if (open)
        {
            if (timer < 1f)
                timer += Time.deltaTime * lerpSpeed;
        }
        else
        {
            if (timer > 0f)
                timer -= Time.deltaTime * lerpSpeed;
        }
        timer = Mathf.Clamp01(timer);
        model.SetPositionAndRotation(
            Vector3.Lerp(closedState.position, openedState.position, timer),
            Quaternion.Slerp(closedState.rotation, openedState.rotation, timer));
    }

    private void TryOpen(Collectible _)
    {
        if (PlayerControl.Instance.shellCollector.numShells >= minShells)
        {
            open = true;
            OnOpen?.Invoke();
        }
    }

    public void SetMinShells(int shells)
    {
        minShells = shells;
    }
}
