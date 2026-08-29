using UnityEngine;

public class GameActions : MonoBehaviour
{
    private GameObject _prevSpawned;
    
    public void SpawnHere(GameObject target)
    {
        _prevSpawned = Instantiate(target, transform.position, transform.rotation);
    }

    public void SpawnAsChild(GameObject target)
    {
        _prevSpawned = Instantiate(target, transform);
    }

    public void DestroySpawnedWithDelay(float delay)
    {
        if (_prevSpawned != null)
            Destroy(_prevSpawned, delay);
    }

    public void DestroyTarget(GameObject target)
    {
        Destroy(target);
    }

    public void LoadScene(string sceneName)
    {
        SceneLoader.Instance.LoadScene(sceneName);
    }
}
