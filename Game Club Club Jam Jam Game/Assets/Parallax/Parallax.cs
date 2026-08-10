using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Tooltip("Single source sprite to tile. Copied 3 times (4 total), alternating normal/flipped/normal/flipped left to right.")]
    [SerializeField] private Sprite sourceSprite;
    [Tooltip("Sorting layer/order applied to all generated copies. Leave as Default/0 if you don't use custom sorting layers.")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder;
    [Range(0f, 1f)]
    [SerializeField] private float parallaxEffect = 0.5f;

    private float length;
    private float startPosition;
    private SpriteRenderer[] copies;

    private void Awake()
    {
        BuildTileStrip();
    }

    private void Start()
    {
        startPosition = transform.position.x;
    }

    private void Update()
    {
        float temp = Camera.main.transform.position.x * parallaxEffect;
        float distance = Camera.main.transform.position.x * (1f - parallaxEffect);
        transform.position = new Vector3(startPosition + distance, transform.position.y, transform.position.z);

        if (temp > startPosition + length)
        {
            startPosition += length;
        }
        else if (temp < startPosition - length)
        {
            startPosition -= length;
        }
    }

    private void BuildTileStrip()
    {
        if (sourceSprite == null)
        {
            Debug.LogWarning($"Parallax on {name}: no sourceSprite assigned.", this);
            return;
        }

        const int copyCount = 4;
        float spriteWidth = sourceSprite.bounds.size.x;

        copies = new SpriteRenderer[copyCount];

        for (int i = 0; i < copyCount; i++)
        {
            GameObject copyObject = i == 0 ? gameObject : new GameObject($"{name}_Tile{i}");
            if (i > 0)
            {
                copyObject.transform.SetParent(transform, false);
                copyObject.transform.localPosition = new Vector3(spriteWidth * i, 0f, 0f);
            }

            SpriteRenderer sr = copyObject.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = copyObject.AddComponent<SpriteRenderer>();

            sr.sprite = sourceSprite;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
            sr.flipX = (i % 2) == 1;

            copies[i] = sr;
        }

        length = spriteWidth * copyCount;
    }
}