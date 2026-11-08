using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Bom : NetworkBehaviour
{
    private SpriteRenderer grenadeRenderer;
    private Rigidbody2D bombRigidbody;

    public GameObject explosionEffectPrefab;
    public float explosionRadius = 5f;
    public float knockbackForce = 5f;
    public float bounceForce = 2f;
    public float initialThrowForce = 10f;
    public float throwAngle = 50f;
    public float gravityScale = 9f;
    public float explosionTime = 2f;

    private bool hasExploded = false;
    private bool isReadyToBounce = true;

    private void Awake()
    {

        Vector3 currentPosition = transform.position;
        currentPosition.z = -Mathf.Abs(currentPosition.z);
        transform.position = currentPosition;
    }


    private void Start()
    {
        Invoke(nameof(Explode), explosionTime);
        grenadeRenderer = GetComponent<SpriteRenderer>();
        grenadeRenderer.sortingOrder = 2;
        bombRigidbody = GetComponent<Rigidbody2D>();
        bombRigidbody.isKinematic = false;
        bombRigidbody.gravityScale = 9; // Ensure gravity affects the bomb  
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
            Rigidbody2D playerRigidbody = collision.gameObject.GetComponent<Rigidbody2D>();
            playerRigidbody.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }

        if (collision.gameObject.CompareTag("Ground") && !hasExploded && isReadyToBounce)
        {
            bombRigidbody.linearVelocity = new Vector2(bombRigidbody.linearVelocity.x * 0.5f, bounceForce);
            StartCoroutine(BounceCooldown());
        }
    }

    private IEnumerator BounceCooldown()
    {
        isReadyToBounce = false;
        yield return new WaitForSeconds(0.2f);
        isReadyToBounce = true;
    }

    private void Explode(){
        if (!hasExploded)
        {
            hasExploded = true;

            if (explosionEffectPrefab != null)
            {
                // Hiệu ứng nổ cố ý để CỤC BỘ trên từng máy (không Spawn qua mạng),
                // vì đúng cái va chạm cục bộ đó mới kích hoạt được PushBackScript
                // của người chơi trên chính máy họ. Nó chưa từng được Spawn nên
                // Destroy thẳng là hợp lệ.
                GameObject explosionEffect = Instantiate(explosionEffectPrefab, this.transform.position, Quaternion.identity);
                Destroy(explosionEffect, 0.2f);
            }

            // Quả bom thì là NetworkObject do server spawn -> chỉ server được huỷ.
            NetworkSession.DespawnOrDestroy(gameObject);
        }
    }

    public void TriggerExplosion()
    {
        Explode();
    }
}
