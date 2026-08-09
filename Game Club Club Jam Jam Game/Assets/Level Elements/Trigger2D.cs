using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Trigger2D : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers = -1;
    [SerializeField] private string[] targetTags;
    [SerializeField] private bool ignoreOtherTriggers;

    private HashSet<string> _targetTags = new HashSet<string>();

    public UnityEvent OnTriggerEnter;
    public UnityEvent OnTriggerExit;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (targetTags != null)
        {
            foreach(string tag in targetTags)
            {
                _targetTags.Add(tag);
            }
        }
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_targetTags.Contains(collision.tag) || (targetLayers.value & (1 << collision.gameObject.layer)) <= 0)
            return;
        if (ignoreOtherTriggers && collision.isTrigger)
            return;
        OnTriggerEnter?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!_targetTags.Contains(collision.tag) || (targetLayers.value & (1 << collision.gameObject.layer)) <= 0)
            return;
        if (ignoreOtherTriggers && collision.isTrigger)
            return;
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
