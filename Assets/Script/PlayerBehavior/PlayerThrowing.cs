using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerThrowing : MonoBehaviour
{
    [Header("Throwing Settings")]
    public GameObject grenadePrefab; // Prefab lựu đạn

    public float grenadeThrowForce = 15f; // Lực ném lựu đạn, tăng lên từ 10f để ném xa hơn
    [SerializeField] private GameObject firePoint; // Vị trí nơi lựu đạn được ném ra

    private float TimeThrow = 1f; // Thời gian chờ giữa các lần ném
    private float TimeCount = 0;

    private void Start()
    {

        // Lấy vị trí ném từ con cái đầu tiên
        firePoint = transform.GetChild(0).gameObject;
    }

    private void Update()
    {
        // Kiểm tra nếu nhấn phím ném lựu đạn
        if (Input.GetKeyDown(KeyCode.G) && TimeCount >= TimeThrow)
        {
            ThrowGrenade();
            TimeCount = 0;
        }
        TimeCount += Time.deltaTime;
    }

    private void ThrowGrenade()
    {
        // Tạo lựu đạn tại vị trí firePoint
        GameObject newGrenade = Instantiate(grenadePrefab, firePoint.transform.position, firePoint.transform.rotation);

        // Thiết lập lực ném cho lựu đạn
        Rigidbody2D grenadeRigidbody = newGrenade.GetComponent<Rigidbody2D>();
        if (grenadeRigidbody != null)
        {// Hướng ném thẳng phía trước
            grenadeRigidbody.linearVelocity = transform.right * 100f + transform.up * 50f;

        }
    }
}
