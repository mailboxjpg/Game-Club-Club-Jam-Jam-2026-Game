using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShellCollector : MonoBehaviour
{
    [Tooltip("Maximum number of shells to collect.")]
    [SerializeField] private int maxShells;
    [Tooltip("Tags to try collection trigger check on.")]
    [SerializeField] private string[] collectTags;
    private int numShells;
    private HashSet<string> collectTagsSet = new HashSet<string>();

    public Action<Collectable> OnCollect;
    public Action OnMaxCollected;
    
    private void Start()
    {
        foreach(string collectTag in collectTags)
        {
            collectTagsSet.Add(collectTag);
        }
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (numShells >= maxShells)
            return;
        if (collectTagsSet.Contains(collision.tag) && collision.TryGetComponent<Collectable>(out var collectable))
        {
            numShells += collectable.Collect();
            OnCollect?.Invoke(collectable);
            if (numShells == maxShells)
                OnMaxCollected?.Invoke();
        }
    }

    public void SetMaxShells(int max)
    {
        maxShells = max;
    }

    public void ResetShells()
    {
        numShells = 0;
    }
}
