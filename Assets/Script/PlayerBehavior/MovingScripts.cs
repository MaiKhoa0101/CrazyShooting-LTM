using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;

public class MovingScripts : NetworkBehaviour
{
    public GameObject Thongbaochet;
    public Rigidbody2D myRigidbody;  // Đối tượng Rigidbody2D điều khiển vật lý của nhân vật
    private BoxCollider2D currentCollider; // Collider của đối tượng hiện đang va chạm
    public Collider2D[] secondGroundColliders; // Danh sách collider của "SecondGround"

    public float upwardForce;   // Lực nhảy khi nhấn phím W
    public float leftForce;     // Lực sang trái khi nhấn phím A
    public float rightForce;    // Lực sang phải khi nhấn phím D
    public float downForce;     // Lực đi xuống khi nhấn phím S
    public float speed;         // Tốc độ di chuyển của nhân vật
    public float maxHorizontalSpeed; // Tốc độ tối đa theo trục X

    [SerializeField]
    private int jumpCount;  // Số lần nhảy còn lại trước khi phải chạm đất
    private Vector2 originalSize;  // Kích thước ban đầu của BoxCollider2D
    [SerializeField]
    private float tocdoroi;  // Tốc độ rơi của nhân vật khi nhảy lên
    private Vector2 vecGravity;  // Vector đại diện cho trọng lực
    private bool facingRight;

    private float timeCountJump; // Đếm ngược thời gian nhảy
    private Animator animator;
    public NetworkVariable<int> life_count = new NetworkVariable<int>(3);

    // Chặn việc báo rơi nhiều khung hình liên tiếp cho cùng một lần rơi.
    // Chỉ owner dùng; được mở lại khi server xác nhận hồi sinh.
    private bool fallReported;

    /// <summary>Người chơi còn mạng hay không. MatchManager đọc trên server để tìm người thắng.</summary>
    public bool IsAlive => life_count.Value > 0;

    public override void OnNetworkSpawn()
    {
        animator = GetComponent<Animator>();
        animator.SetBool("JumpState", false);
        animator.SetBool("FallingState", false);
        animator.SetBool("WalkingState", false);
        animator.SetBool("StandingState", true);

        // Khởi tạo giá trị cho các biến
        upwardForce = 80f;
        leftForce = 30f;
        rightForce = 30f;
        downForce = 20f;
        speed = 3f;
        jumpCount = 2;
        tocdoroi = 3f;

        //Gắn rigidbody của nhân vật
        myRigidbody = GetComponent<Rigidbody2D>();
        // Lấy các Collider của đối tượng có tag "SecondGround"
        secondGroundColliders = GameObject.FindWithTag("SecondGround").GetComponents<Collider2D>();

        // Đặt vector trọng lực bằng giá trị trọng lực của Physics2D
        vecGravity = new Vector2(0, -Physics2D.gravity.y);

        // Đặt kích thước mặc định và trạng thái hướng của nhân vật
        transform.localScale = Vector2.one;
        facingRight = true;
        timeCountJump = 0;

        maxHorizontalSpeed = 40; // Giới hạn tốc độ di chuyển theo trục X|XXXXXXXXXXXXX( QUAN TRỌNG )XXXXXXXXXXXXXXXX

        // Báo trọng tài biết trận này có thêm một người chơi. Cần cho trường hợp
        // có người rời phòng giữa chừng: lúc đó đối tượng của họ đã bị despawn
        // nên không đếm lại được nữa.
        if (IsServer && MatchManager.Instance != null)
        {
            MatchManager.Instance.NotifyPlayerSpawned();
        }
    }


    void Update()
    {
        if (IsOwner)
        {
            // Hết mạng thì không điều khiển được nữa, cũng không báo rơi thêm lần nào.
            if (!IsAlive)
            {
                return;
            }

            CharacterMoving();
            CharacterJumping();
            CharacterFalling();
            SetStateAnimation();

            // Owner chỉ BÁO rơi, không tự quyết định mất mạng hay chết.
            // Server mới là trọng tài: nó trừ mạng rồi bảo lại phải làm gì.
            if (!fallReported && this.transform.position.y < -90)
            {
                fallReported = true;
                ReportFallServerRpc();
            }
        }

    }

    // ---------------------------------------------------------------------
    // Vòng đời mất mạng / hồi sinh / chết — server là nơi quyết định.
    // ---------------------------------------------------------------------

    [ServerRpc(RequireOwnership = false)]
    void ReportFallServerRpc()
    {
        // Đã hết mạng từ trước thì bỏ qua, tránh trừ âm.
        if (life_count.Value <= 0)
        {
            return;
        }

        life_count.Value -= 1;

        if (life_count.Value > 0)
        {
            RespawnClientRpc();
            return;
        }

        // Hết mạng: báo cho mọi máy rồi hỏi trọng tài xem trận đã xong chưa.
        AnnounceDeathClientRpc(ResolveDisplayName());

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnPlayerDied();
        }
        else
        {
            Debug.LogError("[MovingScripts] Không tìm thấy MatchManager nên trận không thể kết thúc. " +
                           "Hãy gắn component MatchManager lên prefab " +
                           "Assets/Sprite/Player/Prefab/Player.prefab.");
        }
    }

    [ClientRpc]
    void RespawnClientRpc()
    {
        // Mở lại chốt để lần rơi sau còn báo được.
        fallReported = false;

        if (IsOwner)
        {
            this.transform.position = new Vector3(0, 180, 0);
            if (myRigidbody != null)
            {
                myRigidbody.linearVelocity = Vector2.zero;
            }
        }
    }

    [ClientRpc]
    void AnnounceDeathClientRpc(string deadName)
    {
        ShowDeathBanner(deadName);
    }

    /// <summary>
    /// Hiện bảng "đã thua" ở GIỮA MÀN HÌNH trên MỌI máy.
    /// Bản cũ đặt bảng ở hai toạ độ khác nhau cho server và client, và còn
    /// gọi Destroy(gameObject) lên một NetworkObject phía client — việc mà
    /// Netcode không cho phép và làm hai máy lệch trạng thái.
    /// </summary>
    private void ShowDeathBanner(string deadName)
    {
        if (Thongbaochet == null)
        {
            return;
        }

        // Canvas của prefab này ở chế độ World Space nên vị trí có ý nghĩa.
        // Đặt ngay trước camera để mọi máy đều thấy ở giữa màn hình.
        Vector3 bannerPosition = Vector3.zero;
        Camera cam = Camera.main;
        if (cam != null)
        {
            bannerPosition = cam.transform.position + cam.transform.forward * 10f;
        }

        GameObject banner = Instantiate(Thongbaochet, bannerPosition, Quaternion.identity);

        TMP_Text text = banner.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = deadName + " đã thua";
        }
    }

    /// <summary>Tên hiển thị của người chơi này, ưu tiên tên đã đồng bộ qua mạng.</summary>
    private string ResolveDisplayName()
    {
        var setName = GetComponent<SetName>();
        if (setName != null && !string.IsNullOrWhiteSpace(setName.PlayerName))
        {
            return setName.PlayerName;
        }
        return this.name;
    }


    void SetStateAnimation()
    {

        if (myRigidbody.linearVelocity.y > 1)
        {

            animator.SetBool("JumpState", true);
            animator.SetBool("FallingState", false);
            animator.SetBool("Walking", false);
            animator.SetBool("Standing", false);
        }
        else if (myRigidbody.linearVelocity.y < -1)
        {

            animator.SetBool("JumpState", false);
            animator.SetBool("FallingState", true);
            animator.SetBool("Walking", false);
            animator.SetBool("Standing", false);
        }
        else if (myRigidbody.linearVelocity.x>20 || myRigidbody.linearVelocity.x<-20 )
        {

            animator.SetBool("JumpState", false);
            animator.SetBool("FallingState", false);
            animator.SetBool("Walking", true);
            animator.SetBool("Standing", false);
        }
        else
        {
            animator.SetBool("JumpState", false);
            animator.SetBool("FallingState", false);
            animator.SetBool("Walking", false);
            animator.SetBool("Standing", true);
        }

    }

    //Hàm nhảy
    private void CharacterJumping()
    {
        // Kiểm tra nếu nhấn phím W và nhân vật còn số lần nhảy
        if (Input.GetKeyDown(KeyCode.W) && jumpCount > 0 && myRigidbody.linearVelocity.y > -40)
        {
            // Tạo vận tốc nhảy lên
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, upwardForce);
            jumpCount--; // Giảm số lần nhảy sau khi thực hiện
            timeCountJump = 0.2f;
        }
        timeCountJump-=Time.deltaTime;
    }

    //Hàm rớt
    private void CharacterFalling()
    {
        // Kiểm tra nếu nhấn phím S để làm nhân vật rơi nhanh hơn
        if (Input.GetKeyDown(KeyCode.S))
        {
            // Tăng tốc độ rơi xuống và bỏ qua va chạm với "SecondGround"
            myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, -upwardForce);
            foreach (var collider in secondGroundColliders)
            {
                Physics2D.IgnoreCollision(collider, this.GetComponent<Collider2D>(), true);
                
            }
            WaitFall();
        }

        // Tăng tốc độ rơi khi vận tốc y lớn hơn -50
        if (myRigidbody.linearVelocity.y < -50)
        {
            myRigidbody.linearVelocity -= vecGravity * tocdoroi * Time.deltaTime;
        }
    }

    void WaitFall()
    {
        float i = 0;
        while (i < 0.3f)
        {
            i += Time.deltaTime;
        }
        foreach (var collider in secondGroundColliders)
        {
            Physics2D.IgnoreCollision(collider, this.GetComponent<Collider2D>(), false);
        }
    }

    private void CharacterMoving()
    {
        float horizontalVelocity = myRigidbody.linearVelocity.x;
        if (Input.GetKey(KeyCode.A)|| Input.GetKey(KeyCode.D)){
            

        
        // Di chuyển nhân vật khi nhấn phím A hoặc D
        if (Input.GetKey(KeyCode.A))
        {
            if (facingRight)  // Nếu nhân vật đang đối mặt phải, lật sang trái
            {
                flip();
            }
            // Di chuyển về bên trái
            horizontalVelocity = -leftForce * speed;
            if (horizontalVelocity > -30)
            {
                horizontalVelocity = 0;
            }
        }
        else if (Input.GetKey(KeyCode.D))
        {
            if (!facingRight)  // Nếu nhân vật đang đối mặt trái, lật sang phải
            {
                flip();
            }
            // Di chuyển về bên phải
            horizontalVelocity = rightForce * speed;
        }
            // Giới hạn tốc độ theo trục X khi nhấn nhiều phím
            horizontalVelocity = Mathf.Clamp(horizontalVelocity, -maxHorizontalSpeed, maxHorizontalSpeed);

            // Gán vận tốc mới cho Rigidbody
            myRigidbody.linearVelocity = new Vector2(horizontalVelocity, myRigidbody.linearVelocity.y);
        }


    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Kiểm tra nếu va chạm với mặt đất "BaseGround" hoặc "SecondGround"
        if (collision.gameObject.name == "BaseGround" || collision.gameObject.name == "SecondGround")
        {
            if (myRigidbody.linearVelocity.y < 1 && myRigidbody.linearVelocity.y > -1)
            {
                // Reset số lần nhảy khi chạm đất
                jumpCount = 2;
            }
        }
    }

    void flip()
    {
        // Đảo ngược trạng thái hướng và lật nhân vật mà không xoay hình
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);  // Xoay đối tượng 180 độ quanh trục y
    }

    // Hàm coroutine để chờ một khoảng thời gian trước khi reset kích thước của collider
    private IEnumerator ResetColliderSize(float waitTime)
    {
        yield return new WaitForSeconds(waitTime); // Chờ thời gian chỉ định

        // Trở lại kích thước ban đầu nếu currentCollider khác null
        if (currentCollider != null)
        {
            currentCollider.size = originalSize;
        }
    }
}  