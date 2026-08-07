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

    private int _numShells;
    private HashSet<string> _collectTagsSet = new HashSet<string>();

    public HealthSystem healthSystem;
    public Action<Collectable> OnCollect;
    public Action OnMaxCollected;
    
    private void Start()
    {
        foreach(string collectTag in collectTags)
        {
            _collectTagsSet.Add(collectTag);
        }
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_numShells >= maxShells)
            return;
        if (_collectTagsSet.Contains(collision.tag) && collision.TryGetComponent<Collectable>(out var collectable))
        {
            int shellsCollected = collectable.Collect(this);
            if (shellsCollected < 0) // collection failed
                return;
            _numShells += shellsCollected;
            OnCollect?.Invoke(collectable);
            if (_numShells == maxShells)
                OnMaxCollected?.Invoke();
        }
    }

    public void SetMaxShells(int max)
    {
        maxShells = max;
    }

    public void ResetShells()
    {
        _numShells = 0;
    }
}
