using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RocketBehavior : MonoBehaviour
{
    [SerializeField]
    public GameObject[] SpriteGame;
    public Rigidbody2D rbbody;
    private Collider2D rocketCollider;
    private GameObject secondGroundObjects;
    [SerializeField]
    public GameObject BoomExplode;
    float timeCount;
    // Start is called before the first frame update
    void Start()
    {
        timeCount = 0;            
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        int i = Random.Range(0, SpriteGame.Length);
        spriteRenderer=SpriteGame[i].GetComponent<SpriteRenderer>();
        
    }

    // Update is called once per frame
    void Update()
    {
        Dropping();
        if (timeCount > 10)
        {
            Destroy(gameObject);
        }
        timeCount += Time.deltaTime;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainGame")
        {
            rocketCollider = GetComponent<Collider2D>();
            rbbody = GetComponent<Rigidbody2D>();
            secondGroundObjects = GameObject.FindGameObjectWithTag("SecondGround");
            Collider2D secondGroundCollider = secondGroundObjects.GetComponent<Collider2D>();
            Physics2D.IgnoreCollision(rocketCollider, secondGroundCollider);
        }
    }

    void Dropping()
    {
        rbbody.linearVelocity = new Vector2(rbbody.linearVelocity.x, -200);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground" || collision.gameObject.tag == "Player" )
        {
            Debug.Log("Collide with"+collision.gameObject.tag.ToString());
            GameObject BomNo = Instantiate(BoomExplode, this.transform.position, this.transform.rotation);
            Destroy(gameObject);
        }
    }

}
