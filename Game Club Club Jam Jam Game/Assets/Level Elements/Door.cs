using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform openedState;
    [SerializeField] private Transform closedState;
    [Tooltip("Transform of the model to apply the lerp to.")]
    [SerializeField] private Transform model;
    [SerializeField] private float lerpSpeed = 1f;

    private bool open = false;
    private float timer = 0f;

    private void Start()
    {
        model.SetPositionAndRotation(closedState.position, closedState.rotation);
        PlayerControl.Instance.shellCollector.OnMaxCollected += Open;
    }

    private void OnDestroy()
    {
        PlayerControl.Instance.shellCollector.OnMaxCollected -= Open;
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

    private void Open()
    {
        open = true;
    }
}
