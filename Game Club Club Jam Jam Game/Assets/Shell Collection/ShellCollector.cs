using System;
using System.Collections.Generic;
using UnityEngine;

public class ShellCollector : MonoBehaviour
{
    [Tooltip("Maximum allowed number of collected shells.")]
    [SerializeField] private int maxAllowedShells;
    [Tooltip("Tags to try collection trigger check on.")]
    [SerializeField] private string[] collectTags;
    private int numShells;
    private HashSet<string> collectTagsSet = new HashSet<string>();

    public Action<Collectable> OnCollect;
    
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
        if (numShells >= maxAllowedShells)
            return;
        if (collectTagsSet.Contains(collision.tag) && collision.TryGetComponent<Collectable>(out var collectable))
        {
            numShells += collectable.Collect();
            OnCollect?.Invoke(collectable);
        }
    }
}
