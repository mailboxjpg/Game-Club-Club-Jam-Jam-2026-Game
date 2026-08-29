using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerWithDifficulty : MonoBehaviour
{
    [SerializeField] private Difficulty[] targetDifficulties;

    public UnityEvent OnActivate;

    private void Start()
    {
        foreach (Difficulty difficulty in targetDifficulties)
        {
            if (SceneLoader.Instance.difficulty == difficulty)
            {
                Activate();
                return;
            }
        }
    }

    private void Activate()
    {
        OnActivate?.Invoke();
    }
}
