using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Trigger2D : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers;

    public UnityEvent onTriggerEnter;
    public UnityEvent onTriggerExit;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        onTriggerEnter?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        onTriggerExit?.Invoke();
    }

    public void LoadScene(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
    }
}
