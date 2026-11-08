using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerShooting : NetworkBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab; // Prefab viên đạn

    // Các tham số có thể được thiết lập trong hàm Start
    private float bulletSpeed; // Tốc độ viên đạn
    private GameObject firePoint; // Vị trí nơi viên đạn được bắn ra
    private Rigidbody2D myRigidbody;
    private float TimeShoot;
    private float TimeCount;
    private Animator shootAnimator;

    private void Start()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        shootAnimator = GetComponent<Animator>();

        // Khởi tạo giá trị cho các biến
        bulletSpeed = 180f;  // Tốc độ viên đạn
        firePoint = transform.GetChild(0).gameObject;
        TimeCount = 0;
        TimeShoot = 0.5f;
    }

    private void Update()
    {
        if (!IsOwner) return; // Chỉ cho phép client sở hữu nhân vật mới thực hiện hành động bắn

        // Kiểm tra nếu nhấn phím bắn
        if (Input.GetKeyDown(KeyCode.Space) && TimeCount >= TimeShoot)
        {
            ShootServerRpc();
            TimeCount = 0;
        }
        TimeCount += Time.deltaTime;
    }

    [ServerRpc]
    private void ShootServerRpc()
    {
        // Animation là hiệu ứng hình ảnh -> phát trên mọi máy.
        PlayShootAnimationClientRpc();

        // Viên đạn là đối tượng mạng -> CHỈ server tạo, rồi Spawn() cho mọi máy.
        SpawnBulletOnServer();
    }

    /// <summary>
    /// Tạo đạn trên server rồi spawn qua mạng.
    ///
    /// Bản cũ gọi Instantiate() ngay trong ShootClientRpc, nghĩa là MỖI MÁY tự
    /// sinh một viên đạn riêng rồi tự mô phỏng vật lý. Hai máy không bao giờ
    /// thống nhất được viên đạn đang ở đâu.
    ///
    /// Bốn prefab đạn đều đã có NetworkObject và đã đăng ký trong
    /// Assets/DefaultNetworkPrefabs.asset nên Spawn() dùng được ngay.
    /// </summary>
    private void SpawnBulletOnServer()
    {
        if (!IsServer)
        {
            return;
        }

        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("[PlayerShooting] Chưa gán bulletPrefab hoặc không tìm thấy firePoint.");
            return;
        }

        GameObject newBullet = Instantiate(
            bulletPrefab, firePoint.transform.position, firePoint.transform.rotation);

        var bulletNetworkObject = newBullet.GetComponent<NetworkObject>();
        if (bulletNetworkObject == null)
        {
            Debug.LogError("[PlayerShooting] Prefab đạn '" + bulletPrefab.name +
                           "' không có NetworkObject. Huỷ viên đạn này.");
            Destroy(newBullet);
            return;
        }

        // Spawn trước, rồi mới set vận tốc, để trạng thái ban đầu kịp đi kèm gói spawn.
        bulletNetworkObject.Spawn(true);

        Rigidbody2D bulletRigidbody = newBullet.GetComponent<Rigidbody2D>();
        if (bulletRigidbody != null)
        {
            bulletRigidbody.linearVelocity = transform.right * bulletSpeed;
        }

        // Không cho đạn va vào chính người bắn. Phải làm trên mọi máy vì vật lý
        // được mô phỏng cục bộ ở từng máy, nên gửi kèm một ClientRpc.
        IgnoreShooterCollision(newBullet);
        IgnoreShooterCollisionClientRpc(bulletNetworkObject.NetworkObjectId);
    }

    [ClientRpc]
    private void IgnoreShooterCollisionClientRpc(ulong bulletNetworkObjectId)
    {
        if (IsServer)
        {
            return; // server đã làm rồi
        }

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null)
        {
            return;
        }

        if (nm.SpawnManager.SpawnedObjects.TryGetValue(bulletNetworkObjectId, out NetworkObject bullet))
        {
            IgnoreShooterCollision(bullet.gameObject);
        }
    }

    private void IgnoreShooterCollision(GameObject bullet)
    {
        if (bullet == null)
        {
            return;
        }

        Collider2D shooterCollider = GetComponent<Collider2D>();
        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();

        if (shooterCollider != null && bulletCollider != null)
        {
            Physics2D.IgnoreCollision(shooterCollider, bulletCollider, true);
        }
    }

    [ClientRpc]
    private void PlayShootAnimationClientRpc()
    {
        if (gameObject.name.Contains("Cowboy"))
        {
            Debug.Log("Cowboy tancong");
            StartCoroutine(PlayAnimationAndWait("ShootingPistol"));
        }
        else if (gameObject.name.Contains("Robot"))
        {
            Debug.Log("Sniper tancong");

            StartCoroutine(PlayAnimationAndWait("ShootingSniper"));
        }
        else if (gameObject.name.Contains("Madman"))
        {
            Debug.Log("Madman tancong");

            StartCoroutine(PlayAnimationAndWait("Throwing"));
        }
        else if (gameObject.name.Contains("Assassin"))
        {
            Debug.Log("Assassin tancong");

            StartCoroutine(PlayAnimationAndWait("Slash"));
        }

        // Việc sinh đạn ĐÃ CHUYỂN sang SpawnBulletOnServer().
        // Trước đây Instantiate() nằm ở đây, ngoài mọi nhánh if, nên mỗi máy
        // tự tạo một viên đạn cục bộ và vị trí không bao giờ khớp nhau.
    }

    private IEnumerator PlayAnimationAndWait(string animName)
    {
        Debug.Log("Animation attack is play");
        shootAnimator.Play(animName);

        yield return new WaitForEndOfFrame();

        while (shootAnimator.GetCurrentAnimatorStateInfo(0).IsName(animName) &&
               shootAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        SetStateAnimation();

        Debug.Log("Animation completed!");
    }

    void SetStateAnimation()
    {
        if (myRigidbody.linearVelocity.y > 1)
        {
            shootAnimator.SetBool("JumpState", true);
            shootAnimator.SetBool("FallingState", false);
            shootAnimator.SetBool("Walking", false);
            shootAnimator.SetBool("Standing", false);
        }
        else if (myRigidbody.linearVelocity.y < -1)
        {
            shootAnimator.SetBool("JumpState", false);
            shootAnimator.SetBool("FallingState", true);
            shootAnimator.SetBool("Walking", false);
            shootAnimator.SetBool("Standing", false);
        }
        else if (myRigidbody.linearVelocity.x != 0)
        {
            shootAnimator.SetBool("JumpState", false);
            shootAnimator.SetBool("FallingState", false);
            shootAnimator.SetBool("Walking", true);
            shootAnimator.SetBool("Standing", false);
        }
        else
        {
            shootAnimator.SetBool("JumpState", false);
            shootAnimator.SetBool("FallingState", false);
            shootAnimator.SetBool("Walking", false);
            shootAnimator.SetBool("Standing", true);
        }
    }
}
