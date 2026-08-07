using UnityEngine;

public class Enemy1Controler : MonoBehaviour
{
    Transform player_transform = null;

    [SerializeField] Rigidbody2D rb;
    [SerializeField] float target_verticle_bound;
    [SerializeField] float verticle_speed;

    private void Start()
    {
        player_transform = GameObject.Find("PlayerController").GetComponent<Transform>();
    }
    private void Update()
    {
        Vector3 velocity = Vector3.zero;
        if(Mathf.Abs(player_transform.position.y - transform.position.y) > target_verticle_bound)
        {
            print("outside enemy bounds");
            velocity.y += Mathf.Clamp(player_transform.position.y - transform.position.y, -verticle_speed, verticle_speed);
        }

        rb.MovePosition(transform.position + velocity);
    }
}
