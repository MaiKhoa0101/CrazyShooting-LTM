using System.Collections;
using UnityEngine;

public class BulletSniper : MonoBehaviour
{
    public Rigidbody2D bulletBody;
    public SpriteRenderer bulletRenderer;
    private float timeCount;
    private float timeLife;
    private Vector2 initialVelocity;

    void Start()
    {
        bulletRenderer = GetComponent<SpriteRenderer>();
        bulletRenderer.sortingOrder = 4;

        bulletBody = GetComponent<Rigidbody2D>();

        // Store the initial velocity to ensure it stays constant
        initialVelocity = bulletBody.linearVelocity;

        timeCount = 0;
        timeLife = 4f;
        transform.localScale = new Vector3(7, 2, 1);
    }

    void Update()
    {
        // Keep the bullet's velocity constant
        bulletBody.linearVelocity = initialVelocity;

        if (timeCount > timeLife)
        {
            // Đạn nay là NetworkObject do server spawn -> chỉ server được huỷ.
            NetworkSession.DespawnOrDestroy(gameObject);
        }
        timeCount += Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")||collision.gameObject.CompareTag("Ground"))
        {
            // Allow bullet to keep flying without being affected by the collision.
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
        }
        if (collision.gameObject.CompareTag("Player"))
        {
            // Ignore deceleration after hitting the player by resetting the velocity
            bulletBody.linearVelocity = initialVelocity;
        }
    }
}
