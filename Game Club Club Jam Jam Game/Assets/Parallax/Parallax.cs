using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Range(0f, 100f)]
    [SerializeField] private float parallaxEffect;
    [SerializeField] private float lengthMultiplier = 1f;
    
    private float length;
    private float startPosition;

    // Start is called before the first frame update
    private void Start()
    {
        startPosition = transform.position.x;
        length = transform.localScale.x * lengthMultiplier;
    }

    // Update is called once per frame
    private void Update()
    {
        float temp = Camera.main.transform.position.x * parallaxEffect;
        float distance = Camera.main.transform.position.x * (1f - parallaxEffect);
        transform.position = new Vector3(startPosition + distance, transform.position.y, transform.position.z);

        if(temp > startPosition + length)
        {
            startPosition += length;
        }
        else if(temp < startPosition - length)
        {
            startPosition -= length;
        }
    }
}