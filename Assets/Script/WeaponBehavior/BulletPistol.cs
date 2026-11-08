using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPistol : MonoBehaviour
{
    public Rigidbody2D bulletBody; // Rigidbody2D của viên đạn
    public SpriteRenderer bulletRenderer; // Renderer của viên đạn
    private float timeCount; // Thời gian tồn tại còn lại của viên đạn
    private float timeLife; // Thời gian tồn tại tối đa của viên đạn
    // Bắt đầu được gọi trước khi cập nhật khung đầu tiên
    void Start()
    {
        // Khởi tạo các giá trị ban đầu
        bulletRenderer = GetComponent<SpriteRenderer>();
        bulletRenderer.sortingOrder = 4; // Đặt thứ tự hiển thị

        bulletBody = GetComponent<Rigidbody2D>();

        timeCount = 0; // Khởi tạo thời gian
        timeLife = 4f;
        gameObject.transform.localScale = new Vector3(0.8f, 0.8f,1.4f);
    }

    // Update is called once per frame
    void Update()
    {
        // Kiểm tra thời gian sống của viên đạn
        if (timeCount > timeLife)
        {
            // Đạn nay là NetworkObject do server spawn -> chỉ server được huỷ.
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
            NetworkSession.DespawnOrDestroy(gameObject); // Hủy viên đạn sau khi va chạm
        }
    }

    //Đợi vài giây trước khi huỷ
    private IEnumerator WaitAndExecute()
    {
        yield return new WaitForSeconds(0.6f);
    }


}
