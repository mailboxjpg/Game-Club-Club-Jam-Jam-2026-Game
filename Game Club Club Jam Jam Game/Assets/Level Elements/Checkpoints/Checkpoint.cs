using UnityEngine;
using UnityEngine.Events;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private string triggerTag;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private SpriteRenderer activeIndicator;
    [SerializeField] private Color inactiveColor;
    [SerializeField] private Color activeColor;
    [ColorUsage(true, true)]
    [SerializeField] private Color inactiveEmission;
    [ColorUsage(true, true)]
    [SerializeField] private Color activeEmission;
    [SerializeField] private bool activeOnStart;
    [SerializeField] private bool teleportOnStart;

    private Material _activeMaterial;

    public UnityEvent OnActivate;

    private void Start()
    {
        _activeMaterial = activeIndicator.material;
        if(activeOnStart)
            Activate();
        else
            Deactivate();
        if (teleportOnStart)
            PlayerControl.Instance.transform.position = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(PlayerControl.Instance == null)
            return;
        if (!collision.CompareTag(triggerTag))
            return;
        Activate();
    }

    public void Activate()
    {
        PlayerControl.Instance.SetCheckpoint(this);
        
        _activeMaterial.SetColor("_BaseColor", activeColor);
        _activeMaterial.SetColor("_EmissionColor", activeEmission);
        OnActivate?.Invoke();
    }

    public void Deactivate()
    {
        _activeMaterial.SetColor("_BaseColor", inactiveColor);
        _activeMaterial.SetColor("_EmissionColor", inactiveEmission);
    }
}
