# EDITOR SETUP — CrazyShooting-LTM

**File này là gì:** checklist thao tác tay trong Unity Editor mà Human phải tự làm sau khi 5 mốc M1–M5 đã xong phần code. Code không tự gắn được component vào prefab và không tự kéo reference trong scene — phần đó nằm ở đây.

**Nguồn:** trích **nguyên văn** từ báo cáo handback M5 của agent `d6932a09-2fb6-4b6a-ade0-3f3f256eef42`, mục §6 (A) và §7 (B). Phụ lục ở cuối trích nguyên văn §2.1, §2.2, §2.3 và §4 của cùng báo cáo.

**Ngày trích:** 2026-10-31. Branch khi trích: `FinalGame`.

**Cách dùng:** làm §6 (A) trước, theo đúng thứ tự A0 → A8. Sau đó chạy §7 (B) từ T0 → T7. Phụ lục là ngữ cảnh để hiểu *tại sao*, và hai điểm còn chờ Human quyết.

---

## Mục lục

- [6. (A) THAO TÁC INSPECTOR BẮT BUỘC](#6-a-thao-tác-inspector-bắt-buộc)
  - [A0 — Chuẩn bị](#-a0--chuẩn-bị)
  - [A1 — Khôi phục tag](#-a1--khôi-phục-tag-kiểm-tra-lại-kết-quả-m1)
  - [A2 — Gỡ 11 component "Missing Script"](#-a2--gỡ-11-component-missing-script)
  - [A3 — Gắn `ClientNetworkTransform` vào 4 prefab nhân vật](#-a3--gắn-clientnetworktransform-vào-4-prefab-nhân-vật)
  - [A4 — Gắn `NetworkTransform` vào 4 prefab đạn](#-a4--gắn-networktransform-vào-4-prefab-đạn)
  - [A5 — Gắn `MatchManager` vào `Player.prefab`](#-a5--gắn-matchmanager-vào-playerprefab)
  - [A6 — Gán ô chữ kết quả trong scene `GameOver`](#-a6--gán-ô-chữ-kết-quả-trong-scene-gameover)
  - [A7 — Đổi nhãn nút `Quit`](#-a7--đổi-nhãn-nút-quit-khuyến-nghị-không-bắt-buộc)
  - [A8 — Thêm nút thoát trong trận](#-a8--thêm-nút-thoát-trong-trận-khuyến-nghị)
  - [A9 — Chờ quyết định, CHƯA LÀM](#-a9--chờ-quyết-định-chưa-làm)
- [7. (B) KỊCH BẢN TEST](#7-b-kịch-bản-test)
  - [T0 — Trước khi test](#t0--trước-khi-test)
  - [T1 — Nghiệm thu M1 (tag)](#t1--nghiệm-thu-m1-tag)
  - [T2 — Nghiệm thu M4 (kết nối)](#t2--nghiệm-thu-m4-kết-nối-làm-trước-vì-nó-chặn-mọi-thứ)
  - [T3 — Nghiệm thu M5 (đồng bộ vị trí và đạn)](#t3--nghiệm-thu-m5-đồng-bộ-vị-trí-và-đạn--quan-trọng-nhất)
  - [T4 — Nghiệm thu M2 (kết thúc trận và người thắng)](#t4--nghiệm-thu-m2-kết-thúc-trận-và-người-thắng)
  - [T5 — Nghiệm thu M3 (chơi lại nhiều vòng)](#t5--nghiệm-thu-m3-chơi-lại-nhiều-vòng--bài-test-quyết-định)
  - [T6 — Nghiệm thu M4 (mất kết nối)](#t6--nghiệm-thu-m4-mất-kết-nối)
  - [T7 — Kiểm tra cuối](#t7--kiểm-tra-cuối)
- [Phụ lục](#phụ-lục)

---
---

# 6. (A) THAO TÁC INSPECTOR BẮT BUỘC

> Làm **đúng thứ tự**. Mở Unity, đợi biên dịch xong, Console phải `0 error` trước khi bắt đầu.
> Sau mỗi prefab nhớ `Ctrl+S`.

---

### ☐ A0 — Chuẩn bị

1. Mở Unity, click vào cửa sổ Editor cho nó lấy focus và biên dịch.
2. `Window → General → Console`, bấm `Clear`.
3. Xác nhận **0 error**.

**Không làm thì hỏng gì:** mọi bước sau đều vô nghĩa nếu code chưa biên dịch; `Add Component` sẽ không tìm thấy script mới.

---

### ☐ A1 — Khôi phục tag (kiểm tra lại kết quả M1)

1. `Edit → Project Settings → Tags and Layers` → bung **Tags**.
2. Phải thấy đủ **9** tag: `Enemy`, `Ground`, `Object`, `SecondGround`, `PistolBullet`, `SniperBullet`, `SlashLine`, `bomno`, `Bomb`.
3. Kiểm tra ô **Tag** ở đầu Inspector của 5 prefab:

| Prefab | Tag phải thấy |
|---|---|
| `Assets/Sprite/SpawnableObject/Prefab/Bom.prefab` | `Bomb` |
| `Assets/Sprite/SpawnableObject/Prefab/SniperBullet.prefab` | `SniperBullet` |
| `Assets/Sprite/SpawnableObject/Prefab/PistolBullet.prefab` | `PistolBullet` |
| `Assets/Sprite/SpawnableObject/Prefab/Slashline.prefab` | `SlashLine` |
| `Assets/Sprite/SpawnableObject/Prefab/ExpAnimator 2.prefab` | `bomno` |

Nếu ô nào hiện `Untagged` hoặc `Undefined` → **dừng, báo lại**, đừng tự sửa.

**Không làm thì hỏng gì:** `CompareTag`/`FindWithTag` với tag chưa khai báo ném `UnityException`. `MovingScripts.cs:53` gọi `FindWithTag("SecondGround")` ngay trong `OnNetworkSpawn()` → nhân vật chết ngay lúc khởi tạo, không di chuyển được.

---

### ☐ A2 — Gỡ 11 component "Missing Script"

Đây là rác do gỡ package `com.unity.multiplayer.samples.coop` để lại. Gỡ **trước** khi gắn component mới, nếu không Inspector sẽ rất rối.

Cách gỡ một cái: click chuột phải lên thanh tiêu đề component ghi `Missing (Mono Script)` → `Remove Component`.

| # | Prefab | GameObject | Số component Missing |
|---|---|---|---|
| ☐ | `Assets/Sprite/Player/Prefab/Player.prefab` | `Player` (gốc) | **2** |
| ☐ | `Assets/Sprite/Player/Prefab/Assassin.prefab` | gốc | **2** |
| ☐ | `Assets/Sprite/Player/Prefab/Cowboy.prefab` | gốc | **2** |
| ☐ | `Assets/Sprite/Player/Prefab/Madman.prefab` | gốc | **2** |
| ☐ | `Assets/Sprite/Player/Prefab/Robot.prefab` | gốc | **2** |
| ☐ | `Assets/Sprite/Player/Prefab/Canvas.prefab` | `Canvas` (gốc) | **1** |

Tổng **11**. Sau khi gỡ xong, Console không được còn dòng nào kiểu *"The referenced script on this Behaviour is missing"*.

> Mẹo: Unity 6 có `GameObject → Remove Missing Scripts` khi đang chọn object — dùng được, nhưng kiểm lại bằng mắt cho chắc.

**Không làm thì hỏng gì:** không gây crash, nhưng mỗi lần spawn nhân vật Console sẽ spam lỗi, che mất lỗi thật. Và nếu không gỡ cái cũ mà gắn cái mới, prefab sẽ có hai component chồng nhau gây tranh chấp transform.

---

### ☐ A3 — Gắn `ClientNetworkTransform` vào 4 prefab nhân vật

Làm **lần lượt** cho `Assassin.prefab`, `Cowboy.prefab`, `Madman.prefab`, `Robot.prefab` trong `Assets/Sprite/Player/Prefab/`:

1. Double-click mở prefab, chọn GameObject **gốc**.
2. `Add Component` → gõ `ClientNetworkTransform` → Enter.
3. Set **đúng** các giá trị sau (đây là cấu hình thật khôi phục từ `Player.prefab:100-118`, không phải đoán):

| Trường | Giá trị |
|---|---|
| Use Unreliable Deltas | ☐ tắt |
| **Sync Position X** | ☑ **bật** |
| **Sync Position Y** | ☑ **bật** |
| Sync Position Z | ☐ tắt |
| Sync Rot Angle X | ☐ tắt |
| **Sync Rot Angle Y** | ☑ **bật** |
| Sync Rot Angle Z | ☐ tắt |
| Sync Scale X / Y / Z | ☐ tắt cả ba |
| Position Threshold | `0.001` |
| Rot Angle Threshold | `0.01` |
| Scale Threshold | `0.01` |
| Use Quaternion Synchronization | ☐ tắt |
| Use Quaternion Compression | ☐ tắt |
| Use Half Float Precision | ☐ tắt |
| In Local Space | ☐ tắt |
| **Interpolate** | ☑ **bật** |
| Slerp Position | ☐ tắt |

4. `Ctrl+S`.

> **`Sync Rot Angle Y` bắt buộc bật.** `MovingScripts.flip()` (`:255`) lật mặt nhân vật bằng `transform.Rotate(0f, 180f, 0f)`. Không đồng bộ trục Y thì hai máy thấy nhân vật quay mặt ngược nhau.

**Không làm thì hỏng gì:** không có component này thì **vị trí nhân vật hoàn toàn không đồng bộ** — mỗi máy thấy đối thủ đứng yên tại điểm spawn trong khi thực tế họ đang chạy khắp màn. Đây là lỗi nặng nhất còn lại.

---

### ☐ A4 — Gắn `NetworkTransform` vào 4 prefab đạn

**Lưu ý: dùng `NetworkTransform` (bản gốc của Unity), KHÔNG phải `ClientNetworkTransform`.** Đạn thuộc về server, không thuộc về người chơi nào.

Làm cho cả 4 file trong `Assets/Sprite/SpawnableObject/Prefab/`: `PistolBullet.prefab`, `SniperBullet.prefab`, `Bom.prefab`, `Slashline.prefab`.

1. Mở prefab, chọn GameObject gốc.
2. `Add Component` → gõ `Network Transform` → chọn **`NetworkTransform`**.
3. Set:

| Trường | Giá trị |
|---|---|
| **Sync Position X / Y** | ☑ bật cả hai |
| Sync Position Z | ☐ tắt |
| Sync Rot Angle X / Y | ☐ tắt |
| **Sync Rot Angle Z** | ☑ **bật** (hướng bay của đạn) |
| Sync Scale X / Y / Z | ☐ tắt cả ba |
| Position Threshold | `0.001` |
| **Interpolate** | ☑ bật |
| In Local Space | ☐ tắt |

4. `Ctrl+S`.

> Không cần đồng bộ Scale: các script đạn tự set scale trong `Start()` trên mọi máy — `BulletPistol.cs:22`, `BulletSniper.cs:24`, `SlashingLine.cs:22`.

**Không làm thì hỏng gì:** **đạn sẽ đứng im trên máy client.** Từ M5, server là nơi duy nhất sinh đạn và set vận tốc (`PlayerShooting.cs:89,94`). Client nhận được viên đạn qua `Spawn()` nhưng vận tốc là 0. Không có `NetworkTransform` thì không gì đẩy nó đi. **Bước này bắt buộc, không phải tuỳ chọn.**

---

### ☐ A5 — Gắn `MatchManager` vào `Player.prefab`

1. Mở `Assets/Sprite/Player/Prefab/Player.prefab`, chọn GameObject gốc tên `Player`.
2. `Add Component` → `MatchManager`.
3. Để nguyên mặc định: `End Match Delay = 2.5`, `Game Over Scene Name = GameOver`.
4. `Ctrl+S`.

> Chọn `Player.prefab` vì host spawn nó **đúng một lần** khi vào `MainGame` (`MainGameManager.cs:50`), nó đã có `NetworkObject` (`Player.prefab:47`) và đã đăng ký trong `Assets/DefaultNetworkPrefabs.asset:33`.

**Không làm thì hỏng gì:** **trận đấu không bao giờ kết thúc.** Hết mạng xong vẫn kẹt trong `MainGame`. Console sẽ in: *"Không tìm thấy MatchManager nên trận không thể kết thúc"*.

---

### ☐ A6 — Gán ô chữ kết quả trong scene `GameOver`

1. Mở `Assets/Scenes/GameOver.unity`.
2. Hierarchy → chọn object **`BackGround`** (đây mới là nơi gắn `GameOverManager`, không phải `GameOverCanvas`).
3. Inspector → component `GameOverManager` → ô mới **`Result Text`**.
4. Kéo object **`'GameOverText '`** từ Hierarchy vào ô đó.
5. `Ctrl+S`.

**Không làm thì hỏng gì:** vẫn sang được màn kết quả nhưng **không hiện tên người thắng**. Console in warning chỉ đúng việc cần làm.

---

### ☐ A7 — Đổi nhãn nút `Quit` (khuyến nghị, không bắt buộc)

1. Trong `GameOver.unity`, Hierarchy → `Quit` → con của nó (`Text (TMP)`).
2. Sửa chữ thành `VỀ MENU`.
3. `Ctrl+S`.

**Không làm thì hỏng gì:** nút vẫn chạy đúng (rời phiên mạng rồi về menu), chỉ là nhãn gây hiểu nhầm — người chơi tưởng nó thoát game.

---

### ☐ A8 — Thêm nút thoát trong trận (khuyến nghị)

Hiện `MainGame.unity` **không có nút nào** — không có đường ra giữa trận.

1. Mở `Assets/Scenes/MainGame.unity`.
2. Hierarchy → chuột phải object **`Canvas`** → `UI` → `Button - TextMeshPro`.
3. Đổi tên thành `ExitMatch`, kéo về góc trên bên phải, sửa chữ con thành `THOÁT`.
4. Chọn `ExitMatch` → Inspector → `Button` → `On Click ()` → bấm `+`.
5. Kéo chính object **`Canvas`** vào ô object (đây là nơi gắn `MainGame.cs`, xác nhận ở `MainGame.unity:1588`).
6. Dropdown chọn `MainGame` → `ReturnToMenu`.
7. `Ctrl+S`.

**Không làm thì hỏng gì:** đang đánh nhau thì không thoát được, phải tắt cả ứng dụng. (Khi mất kết nối thì `ConnectionUI` vẫn tự hiện nút **VỀ MENU**, nên vẫn có lưới an toàn.)

---

### ☐ A9 — Chờ quyết định, CHƯA LÀM

| Việc | Trạng thái |
|---|---|
| Tạo + gắn `ClientNetworkAnimator` (4 prefab nhân vật) | **Chờ bạn duyệt** — xem §2.1 |
| Component rỗng `7b3d0487…` trên `Player.prefab` | Đã gỡ ở A2, **không thay thế** — xem §2.2 |
| Transform sync cho `Canvas.prefab` | **Cố ý không gắn** — xem §2.3 |

---
---

# 7. (B) KỊCH BẢN TEST

> Cần **hai tiến trình**: một build `File → Build And Run` và một Editor bấm `Play`. Hoặc hai build. Một mình Editor không test được mạng.
>
> Mở Console ở cả hai phía. Trong build, bật log bằng cách chạy với cờ `-logFile` hoặc xem `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\Player.log`.

---

## T0 — Trước khi test

- ☐ Console Editor: **0 error**.
- ☐ Không còn dòng *"The referenced script … is missing"*.
- ☐ Đã làm xong A1–A6.

---

## T1 — Nghiệm thu M1 (tag)

| # | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Chạy host, vào `MainGame` | Nhân vật spawn và **di chuyển được** bằng `A`/`D` |
| 2 | Nhấn `W` hai lần | Nhảy đôi |
| 3 | Đứng trên `SecondGround`, nhấn `S` | Rơi xuyên qua sàn phụ |

**Nếu nhân vật đứng im hoặc trượt kỳ lạ:** `OnNetworkSpawn` đã chết giữa chừng ở `MovingScripts.cs:53`, nghĩa là tag `SecondGround` chưa thông. Quay lại A1.

---

## T2 — Nghiệm thu M4 (kết nối, làm trước vì nó chặn mọi thứ)

| # | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Ở `SelectCharacter`, gõ `abc` vào ô IP, bấm vào phòng | Lớp phủ đen: *"Địa chỉ IP không hợp lệ: "abc"…"* + nút **VỀ MENU**. **Không** chuyển scene |
| 2 | Bấm **VỀ MENU** | Về `MainMenu` |
| 3 | Vào lại, gõ `192.168.1.250` (máy không tồn tại) | *"Đang kết nối tới 192.168.1.250 …"* khoảng **10 giây**, rồi báo không kết nối được + gợi ý kiểm tra tường lửa |
| 4 | Bấm **VỀ MENU** | Về `MainMenu`, không kẹt |
| 5 | Máy A: bấm tạo phòng (StartHost) | *"Đang tạo phòng…"* chớp qua, vào phòng chờ, **IP của host hiện trên màn hình** |
| 6 | Máy B: gõ đúng IP đó, bấm vào phòng | *"Đang kết nối…"* chớp qua, vào phòng chờ |

**Nếu máy B kẹt ở màn chọn nhân vật quá 10 giây rồi báo lỗi dù IP đúng:** đây là rủi ro số 1 của M4 — báo lại ngay, đừng tự sửa.

---

## T3 — Nghiệm thu M5 (đồng bộ vị trí và đạn) — **quan trọng nhất**

| # | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Máy A bấm nút bắt đầu trận | **Cả hai máy** cùng sang `MainGame` |
| 2 | Máy B bấm nút của mình | Không làm gì, Console in *"Chỉ host mới bắt đầu được trận"* |
| 3 | Máy A di chuyển qua trái/phải | **Máy B thấy nhân vật A di chuyển theo, đúng vị trí** ← đây là bài test của A3 |
| 4 | Máy B di chuyển | Máy A thấy đúng |
| 5 | Máy A nhấn `A` rồi `D` (lật mặt) | Máy B thấy nhân vật A lật mặt **cùng hướng** ← bài test của `Sync Rot Angle Y` |
| 6 | Máy A nhấn `Space` bắn | **Cả hai máy thấy ĐÚNG MỘT viên đạn, ở cùng một chỗ** ← bài test của A4 |
| 7 | Máy B nhấn `Space` bắn | Tương tự |
| 8 | Bắn trúng đối thủ | Đối thủ bị hất văng trên **cả hai** máy |
| 9 | Đạn bay hết 4 giây không trúng ai | Đạn biến mất trên **cả hai** máy cùng lúc, Console **không** có lỗi kiểu *"Destroy a spawned NetworkObject"* |
| 10 | Chọn Robot (sniper), bắn | Đạn bay xuyên, không bị chặn |
| 11 | Chọn Madman, ném bom, đợi 3 giây | Bom nổ, ai đứng gần bị hất |
| 12 | Cowboy bắn vào quả bom đang bay | Đạn lục **không** còn tự huỷ (thay đổi có chủ đích từ M1) |

**Nếu đạn đứng im trên máy client:** chưa làm A4.
**Nếu thấy hai viên đạn cho một lần bắn:** `Instantiate` cũ còn sót — báo lại.
**Nếu đạn rung giật mạnh (đặc biệt sniper):** đây là rủi ro §4.2 đã biết, ghi lại rồi báo.

---

## T4 — Nghiệm thu M2 (kết thúc trận và người thắng)

| # | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Cho một người rơi xuống vực (`y < -90`) | Mất **đúng 1** mạng, hồi sinh ở trên cao |
| 2 | Lặp cho tới khi hết 3 mạng | **Cả hai máy** cùng hiện bảng đỏ *"&lt;tên&gt; đã thua"* ở giữa màn hình |
| 3 | Người đã thua thử bấm `A`/`D`/`Space` | **Không điều khiển được nữa** |
| 4 | Đợi ~2.5 giây | **Cả hai máy** cùng sang scene `GameOver` |
| 5 | Nhìn màn kết quả | Hiện *"&lt;tên người còn sống&gt; là người thắng!"* — **phải là tên thật**, không phải `Bom` hay `Player(Clone)` |
| 6 | Nhìn máy client | Có thêm dòng *"(Chờ host bấm Chơi lại)"* |
| 7 | Nhìn hai nút | `Replay` và `Quit` đều **nhìn thấy được**, Console không có `NullReferenceException` |

**Test hoà:** chơi một mình (chỉ host), rơi hết 3 mạng → `GameOver` hiện *"Hoà! Không ai thắng."*

---

## T5 — Nghiệm thu M3 (chơi lại nhiều vòng) — **bài test quyết định**

Đây là bài chứng minh lỗi nhân bản `NetworkManager` đã hết. **Phải làm đủ 4 vòng, đừng bỏ giữa chừng.**

**Chuẩn bị quan sát:** trong Editor, lúc đang `Play`, Hierarchy có mục **`DontDestroyOnLoad`** ở dưới cùng. Bung nó ra.

| # | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Sau T4, máy client bấm `Replay` | Hiện *"Chỉ host mới bắt đầu được ván mới. Đang chờ host..."*, không đổi scene |
| 2 | **Máy host** bấm `Replay` | **Cả hai máy** quay về `MainGame` |
| 3 | Đếm nhân vật trên màn | **Đúng 2** nhân vật, mỗi người một con. **Không được có 3, 4 con** |
| 4 | Kiểm tra số mạng | Mỗi người **đủ 3 mạng** lại từ đầu |
| 5 | Mở `DontDestroyOnLoad` trong Hierarchy | **Đúng MỘT** `NetworkManger`. Không được có `NetworkManger (1)` |
| 6 | Đánh hết ván 2, `Replay` lần nữa | — |
| 7 | **Lặp lại tới hết vòng 4** | Sau mỗi vòng: vẫn đúng 2 nhân vật, vẫn đúng 1 `NetworkManger` |
| 8 | Ở `GameOver` bấm `Quit` (hoặc `VỀ MENU`) | Về `MainMenu`. **Trước M3 nút này không phản ứng gì** |
| 9 | Từ `MainMenu` vào lại `SelectCharacter`, tạo phòng mới | Tạo được bình thường, không lỗi |
| 10 | Mở `DontDestroyOnLoad` lần nữa | Vẫn **đúng một** `NetworkManger` |

**Nếu số nhân vật tăng dần qua các vòng:** rò rỉ `sceneLoaded` chưa bịt hết — báo lại, đừng đoán.
**Nếu thấy `NetworkManger (1)`:** `Destroy(nm.gameObject)` ở `NetworkSession.cs:61` không chạy — báo lại.
**Nếu game đứng hình hoàn toàn sau khi đổi scene:** `Time.timeScale` còn kẹt 0 ở đâu đó — báo lại kèm scene đang ở.

---

## T6 — Nghiệm thu M4 (mất kết nối)

| # | Thao tác | Kết quả mong đợi |
|---|---|---|
| 1 | Đang đánh nhau, **tắt cửa sổ host** | Máy client hiện *"Mất kết nối với host…"* + nút **VỀ MENU** bấm được |
| 2 | Bấm **VỀ MENU** trên client | Về `MainMenu` sạch sẽ |
| 3 | Chơi lại ván mới, đang đánh thì **tắt cửa sổ client** | Trên máy host: trận **kết thúc và sang `GameOver`**, host là người thắng. **Không treo chờ mãi** |
| 4 | Nhìn kỹ máy host ở bước 3 | Host **KHÔNG** được hiện *"Mất kết nối với host"* |

**Nếu bước 3 treo:** `maxPlayersSeen` không hoạt động — báo lại.
**Nếu bước 4 host tự báo lỗi:** bộ lọc `ConnectionUI.cs:146-156` sai — báo lại.

---

## T7 — Kiểm tra cuối

- ☐ Console cả hai máy: không có `NullReferenceException`.
- ☐ Không có lỗi *"Destroy a spawned NetworkObject"*.
- ☐ Không có lỗi *"The referenced script is missing"*.
- ☐ Chữ tiếng Việt có dấu hiển thị đúng ở lớp phủ `ConnectionUI` và bảng *"đã thua"* (nếu thấy ô vuông → font `LiberationSans SDF` thiếu glyph, báo lại).

---

**Hết M5. Năm mốc M1–M5 đã xong phần code.** Game chưa được chạy thử lần nào trong phiên này, nên §6 và §7 mới là thứ biến công việc này thành một game chạy được. Có hai điểm chờ quyết định của bạn: `ClientNetworkAnimator` (§2.1) và việc chuyển knockback về server (§4.1).

---
---

# Phụ lục

> Phần dưới đây **không phải thao tác bắt buộc**. Đây là ngữ cảnh nguyên văn từ cùng báo cáo handback M5, giải thích các mục A2, A9 và các cảnh báo cần biết khi chạy §7. Đánh số §2.1/§2.2/§2.3/§4 giữ nguyên theo báo cáo gốc để các tham chiếu chéo ở trên vẫn đúng.

## §2.1 — `ad8deae8091111f4eb30a56c1f4e0d3e` = `ClientNetworkAnimator`. **Xác định được.**

Trường serialize của nó trên `Assassin.prefab`:
```
TransitionStateInfoList: []
m_Animator: {fileID: 4751238652397439623}
```

Đối chiếu source `NetworkAnimator.cs`: `[SerializeField] private Animator m_Animator;` (`:256`) và `[SerializeField] internal List<TransitionStateInfo> TransitionStateInfoList;` (`:272`). **Trùng khít cả hai.**

Nhưng GUID thật của `NetworkAnimator` là `e8d0727d5ae3244e3b569694d3912374`. Khác → đây là **lớp con** của `NetworkAnimator`, và `NetworkAnimator.cs:685` cũng có `protected virtual bool OnIsServerAuthoritative()`. Kết luận: `ClientNetworkAnimator` từ `com.unity.multiplayer.samples.coop` — cặp song sinh của `ClientNetworkTransform`.

**Có mặt trên:** 4 prefab nhân vật.
**Hệ quả hiện nay:** animation (chạy/nhảy/bắn) **không đồng bộ** giữa các máy. Mỗi máy tự chạy Animator cục bộ. Vì `PlayerShooting` đã phát animation bắn qua `PlayShootAnimationClientRpc` (`:140`) nên động tác bắn vẫn thấy được; nhưng trạng thái đi/nhảy/rơi của người khác thì mỗi máy tự đoán từ `SetStateAnimation()` chạy cục bộ — và với owner-auth transform, dữ liệu để đoán lại đến từ vị trí được đồng bộ, nên **phần lớn vẫn đúng**.

**Đề xuất (KHÔNG tự làm, chờ bạn quyết):**
Tạo `Assets/Script/Network/ClientNetworkAnimator.cs`, 5 dòng, y hệt `ClientNetworkTransform`:
```csharp
[DisallowMultipleComponent]
public class ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative() => false;
}
```
rồi gắn vào 4 prefab nhân vật và kéo `Animator` vào ô `m_Animator`.

**Khuyến nghị của tôi: làm, nhưng để sau khi nghiệm thu xong M1–M5.** Lý do: nó không chặn vòng đời game, và thêm một `NetworkBehaviour` nữa vào lúc này sẽ làm nhiễu việc truy lỗi nếu bài test mạng có vấn đề. Nếu bạn muốn làm luôn, nói một tiếng.

## §2.2 — `7b3d0487e64364742ac3debcbf073199` — **KHÔNG xác định được. Giữ ở trạng thái UNKNOWN.**

Chỉ có trên `Player.prefab`, và khối YAML của nó **rỗng hoàn toàn**:
```yaml
m_Script: {fileID: 11500000, guid: 7b3d0487e64364742ac3debcbf073199, type: 3}
m_Name: 
m_EditorClassIdentifier: 
```
Không một trường serialize nào. Tôi không có cơ sở nào để suy ra nó từng là gì — mọi phỏng đoán sẽ là bịa.

Điều tôi **biết chắc**: nó không mang dữ liệu nào, và không một dòng code nào còn sống trong `Assets/Script` tham chiếu tới nó.

**Đề xuất: gỡ bỏ, không thay thế.** Nếu sau khi gỡ mà có chức năng nào hụt đi, nó sẽ lộ ra trong test — và vì component này không có dữ liệu, gỡ nó không làm mất thông tin gì có thể khôi phục lại được.

## §2.3 — `Canvas.prefab`: **KHÔNG gắn transform sync. Chỉ gỡ.**

Ba căn cứ tôi tự đọc:

1. **Nó là con của nhân vật, không phải đối tượng độc lập.** `Canvas.prefab` (GUID `9db26002c07a0a84b9e4f3019e8adf3d`) được lồng trong cả 4 prefab nhân vật. Và nó **không có `NetworkObject`** riêng (đếm được 0). Vị trí của nó bám theo cha; cha đã được đồng bộ thì nó theo cùng. Đồng bộ lần nữa là lãng phí băng thông thuần tuý.

2. **Xoay của nó là việc RIÊNG của từng máy.** `FaceCamera.cs:10` chạy mỗi khung hình:
   ```csharp
   transform.rotation = Camera.main.transform.rotation;
   ```
   Hướng camera là chuyện cục bộ. Đồng bộ nó qua mạng là **sai về mặt thiết kế** — sẽ ép mọi client dùng hướng camera của host — và còn khiến `FaceCamera` và `NetworkTransform` giành nhau ghi `transform.rotation` mỗi khung hình.

3. **Người cấu hình ban đầu cũng đã né đúng chỗ đó.** Cấu hình cũ trên `Canvas.prefab` khác hẳn 4 nhân vật: `SyncRotAngleY: 0` (nhân vật là `1`) và `Interpolate: 0` (nhân vật là `1`). Tức họ đã cố tình tắt đồng bộ xoay. Điều đó củng cố cách đọc ở trên.

## §4 — UNKNOWN và rủi ro còn lại

1. **Rủi ro số 1 của M5 — knockback vẫn là cục bộ.** `PushBackScript.OnCollisionEnter2D` chạy trên từng máy. Tôi **cố ý giữ đạn được mô phỏng vật lý trên mọi máy** (không tắt Rigidbody2D ở client) để va chạm vẫn nổ ra cục bộ và knockback vẫn hoạt động y như trước. Đổi lại, vật lý cục bộ và `NetworkTransform` sẽ hơi giành nhau; `NetworkTransform` thắng nên **vị trí hội tụ** (đúng mục tiêu M5), nhưng có thể thấy đạn hơi rung. Cách sửa triệt để là chuyển knockback về server — **ngoài phạm vi 5 mốc**, cần bạn quyết riêng.
2. **`BulletSniper.cs:30`** ép `linearVelocity = initialVelocity` mỗi khung hình. Trên client `initialVelocity` được chụp lúc `Start()` khi vận tốc còn 0, nên nó ép về 0 liên tục. `NetworkTransform` ghi đè vị trí nên đạn vẫn bay đúng, nhưng đây là nguồn rung rõ nhất. Tôi **không sửa** vì chạm vào nó là chạm vào cân bằng vũ khí (xuyên thấu). Nếu test thấy sniper rung, báo tôi.
3. **Thứ tự gói tin.** `IgnoreShooterCollisionClientRpc` (`:100`) được gửi ngay sau `Spawn()`. Netcode đảm bảo thứ tự nên đối tượng phải tồn tại khi RPC tới; nếu không, `TryGetValue` thất bại im lặng và đạn sẽ va vào chính người bắn trên máy đó. Chưa gặp, nhưng là điểm cần soi nếu thấy người bắn tự hất mình.
4. **`JoinRoom` vẫn nạp cục bộ** (`JoinRoomManager`). Nợ kỹ thuật từ M3, chưa trả. `MainGame` và `GameOver` đã networked.
5. Tôi **chưa chạy game lần nào**. Mọi khẳng định về hành vi lúc chạy là suy luận từ việc đọc code. §7 là để kiểm chứng chúng.
