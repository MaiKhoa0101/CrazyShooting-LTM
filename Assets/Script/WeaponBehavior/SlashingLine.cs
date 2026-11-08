using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashingLine : MonoBehaviour
{
    public Rigidbody2D slashlineBody; // Rigidbody2D của viên đạn
    public SpriteRenderer slashlineRenderer; // Renderer của viên đạn
    private float timeCount; // Thời gian tồn tại còn lại của viên đạn
    private float timeLife; // Thời gian tồn tại tối đa của viên đạn
    // Start is called before the first frame update
    public GameObject firepos;
    void Start()
    {
        // Khởi tạo các giá trị ban đầu
        slashlineRenderer = GetComponent<SpriteRenderer>();
        slashlineRenderer.sortingOrder = 4; // Đặt thứ tự hiển thị

        slashlineBody = GetComponent<Rigidbody2D>();
        timeCount = 0; // Khởi tạo thời gian
        timeLife =0.1f;
        gameObject.transform.localScale = new Vector3(2.2f, 2.2f,2.2f);
    }

    // Update is called once per frame
    void Update()
    {
        // Kiểm tra thời gian sống của viên đạn
        if (timeCount > timeLife)
        {
            // Vệt chém nay là NetworkObject do server spawn -> chỉ server được huỷ.
            NetworkSession.DespawnOrDestroy(gameObject);
        }
        timeCount += Time.deltaTime; // Cập nhật thời gian sống

    }

    //Kiểm tra va chạm
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Kiểm tra va chạm với người chơi
        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(WaitAndExecute());
            //Destroy(gameObject); // Hủy viên đạn sau khi va chạm
        }
    }

    //Đợi vài giây trước khi huỷ
    private IEnumerator WaitAndExecute()
    {
        yield return new WaitForSeconds(0.6f);
    }


}
