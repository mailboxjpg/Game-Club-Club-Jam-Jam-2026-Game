using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CameraBounds : MonoBehaviour
{

    [Tooltip("Uses the trigger collider's bounds for min and max when true.")]
    [SerializeField] private bool boundsMatchCollider = true;
    [Tooltip("Set CameraControl's bounds from trigger events with this tag.")]
    [SerializeField] private string triggerTag;
    
    private Collider2D _triggerCollider;

    public Vector2 min;
    public Vector2 max;

    private void Start()
    {
        _triggerCollider = GetComponent<Collider2D>();
        if (boundsMatchCollider)
        {
            min = _triggerCollider.bounds.min;
            max = _triggerCollider.bounds.max;
        }
    }

    private void Update()
    {
        
    }

    private void OnValidate()
    {
        if (boundsMatchCollider)
        {
            _triggerCollider = GetComponent<Collider2D>();
            min = _triggerCollider.bounds.min;
            max = _triggerCollider.bounds.max;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag(triggerTag) && !collision.isTrigger)
        {
            // Override the camera's bounds
            CameraControl.Instance.SetBounds(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform.CompareTag(triggerTag) && !collision.isTrigger)
        {
            // Unbound the camera when leaving this bound's trigger and the camera hasn't entered another CameraBounds
            if (CameraControl.Instance.GetBounds() == this)
            {
                CameraControl.Instance.SetBounds(null);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(center, size);
    }
}
