using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Trigger2D : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers;

    public UnityEvent OnTriggerEnter;
    public UnityEvent OnTriggerExit;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        OnTriggerEnter?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        OnTriggerExit?.Invoke();
    }

    public void LoadScene(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
    }

    public void SpawnPrefab(GameObject prefab)
    {
        Instantiate(prefab, transform.position, transform.rotation);
    }
}
