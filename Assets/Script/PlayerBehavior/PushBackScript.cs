using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushBackScript : MonoBehaviour
{
    public Rigidbody2D myRigidbody;  // Đối tượng Rigidbody2D điều khiển vật lý của nhân vật

    [SerializeField]
    public float ShotForce;//Trị số ảnh hưởng từ đạn
    public float BomForce;//Trị số ảnh hưởng từ đạn

    public float ShotForceTimeCounter;//Đếm ngược thời gian được di chuyển sau khi bị ăn đạn
    public float ShotForceTotalTime;//Tổng thời gian delay phải chịu khi ăn đạn
    public bool AnDanBenPhai;

    // Start is called before the first frame update
    void Start()
    {
        //Gắn rigidbody của nhân vật
        myRigidbody = GetComponent<Rigidbody2D>();

        //Khởi tạo giá trị ban đầu cho thuộc tính ăn đạn
        AnDanBenPhai = true;
        BomForce = 80f;
        ShotForce = 90f;
        ShotForceTimeCounter = 0;
        ShotForceTotalTime = 0.2f;
    }

    // Update is called once per frame
    void Update()
    {
        if (ShotForceTimeCounter >0)
        {
            PushBack();
            ShotForceTimeCounter -= Time.deltaTime;
        }
    }

    //Hàm bật lùi của người chơi khi ăn đạn
    public void PushBack()
    {
        if (AnDanBenPhai == true)
        {
            myRigidbody.linearVelocity = new Vector2(-BomForce, ShotForce);
        }
        if (AnDanBenPhai == false)
        {
            myRigidbody.linearVelocity = new Vector2(BomForce, ShotForce);
        }
    }

    //Hàm kiểm tra ăn loại đạn nào và sẽ bị j
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Kiểm tra va chạm với người chơi
        if (collision.gameObject.CompareTag("PistolBullet"))
        {
            //Kích hoạt hàm bật lùi của người chơi
            ShotForceTimeCounter = ShotForceTotalTime;
            if (collision.transform.position.x > transform.position.x)
            {
                AnDanBenPhai = true;
            }
            if (collision.transform.position.x <= transform.position.x)
            {
                AnDanBenPhai = false;
            }

        }
        // Kiểm tra va chạm với người chơi
        if (collision.gameObject.CompareTag("bomno"))
        {
            //Kích hoạt hàm bật lùi của người chơi
            ShotForceTimeCounter = ShotForceTotalTime;
            if (collision.transform.position.x > transform.position.x)
            {
                AnDanBenPhai = true;
            }
            if (collision.transform.position.x <= transform.position.x)
            {
                AnDanBenPhai = false;
            }

        }
        // Kiểm tra va chạm với người chơi
        if (collision.gameObject.CompareTag("SlashLine"))
        {
            //Kích hoạt hàm bật lùi của người chơi
            ShotForceTimeCounter = ShotForceTotalTime;
            if (collision.transform.position.x > transform.position.x)
            {
                AnDanBenPhai = true;
            }
            if (collision.transform.position.x <= transform.position.x)
            {
                AnDanBenPhai = false;
            }

        }
        // Đạn bắn tỉa: trước đây dùng chung tag "PistolBullet" nên không phân biệt được
        // với súng lục. Nay có tag riêng "SniperBullet"; lực đẩy tạm giữ nguyên như cũ.
        if (collision.gameObject.CompareTag("SniperBullet"))
        {
            //Kích hoạt hàm bật lùi của người chơi
            ShotForceTimeCounter = ShotForceTotalTime;
            if (collision.transform.position.x > transform.position.x)
            {
                AnDanBenPhai = true;
            }
            if (collision.transform.position.x <= transform.position.x)
            {
                AnDanBenPhai = false;
            }

        }
    }
}