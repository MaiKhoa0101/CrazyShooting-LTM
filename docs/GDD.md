# CrazyShooting — Game Design Document (GDD)

- Repo: `CrazyShooting-LTM`
- Branch tham chiếu: `FinalGame` (xác nhận: `.git/HEAD` → `ref: refs/heads/FinalGame`)
- Ngày lập: 2026-10-29
- Engine: Unity, 2D, Netcode for GameObjects `1.11.0` (`Packages/manifest.json:8`)
- Nền tảng mục tiêu đợt này: PC Windows standalone (quyết định của Human)

## 0. Quy ước đọc tài liệu

Mỗi khẳng định trong tài liệu này được gắn một trong ba nhãn:

| Nhãn | Nghĩa |
|---|---|
| **[HIỆN TRẠNG]** | Đã đọc trực tiếp trong source/asset trên branch `FinalGame`, có dẫn `file:line`. |
| **[ĐỀ XUẤT]** | Thiết kế đề nghị, **chưa có** trong code. Không được đọc như mô tả sản phẩm hiện tại. |
| **[UNKNOWN]** | Không xác minh được bằng cách đọc bytes; cần mở Unity Editor hoặc chạy build mới kết luận được. |

Tài liệu này **không** sửa code. Mọi mục "nên làm" nằm ở nhãn ĐỀ XUẤT.

---

## 1. Tóm tắt game

**[HIỆN TRẠNG]** CrazyShooting là game đối kháng 2D nhiều người chơi theo mô hình *host/client* trong mạng LAN. Người chơi chọn 1 trong 4 nhân vật, vào cùng một phòng, rồi đánh nhau trên một màn platform. Cơ chế sát thương **không phải là máu** mà là **đẩy lùi (knockback)**: trúng đòn thì bị bắn văng, rơi khỏi màn thì mất 1 mạng.

- Số mạng mặc định: `3` — `Assets/Script/PlayerBehavior/MovingScripts.cs:32`
  (`public NetworkVariable<int> life_count = new NetworkVariable<int>(3);`)
- Ngưỡng rơi chết: `y < -90` — `MovingScripts.cs:77`
- Vị trí hồi sinh: `(0, 180, 0)` — `MovingScripts.cs:85`

**[HIỆN TRẠNG]** Không có hệ thống điểm, không có vòng đấu (round), không có logic xác định người thắng: tìm toàn bộ `Assets/Script` với từ khoá `winner|score|round` chỉ ra các trùng khớp ngẫu nhiên trong comment về `SecondGround`, không có biến điểm nào. Chuỗi `"is the winner"` chỉ tồn tại ở `Assets/Script/Scene/EndSceneCode.cs:24`, và script đó hiện không nằm trong scene nào (xem §6, §8.2).

---

## 2. Vòng đời người chơi (player lifecycle)

### 2.1 Chuỗi trạng thái — [HIỆN TRẠNG]

```
[Khởi động game]
      |
      v
(1) MainMenu  ───────────────────────────────────────────────┐
      |  nút "Mainmenu" → MainMenu.Room()                     │
      |  MainMenu.cs:11  LoadScene("SelectCharacter")         │
      v                                                      │
(2) SelectCharacter                                          │
      |  - gõ tên vào "InputField (TMP) Name"                 │
      |  - gõ IP host vào "InputField (TMP) Room"             │
      |  - Next/Back đổi nhân vật                             │
      |                                                      │
      |── nút "StartHost" ──> JoinRoomManager.cs:31           │
      |      StartHost() rồi LoadSceneAsync("JoinRoom")       │
      |                                                      │
      |── nút "StartClient" ─> JoinRoomManager.cs:34-41       │
      |      ConnectToServer(ip) → LoadSceneAsync("JoinRoom") │
      v                                                      │
(3) Joinroom  (phòng chờ / lobby)                            │
      |  - StartGame.cs:16-27 : host thấy nút "StartHost",    │
      |    client thấy nút "StartClient"                      │
      |  - nút "Exit" → StartGame.ReturnToMenu()              │
      |      StartGame.cs:37 LoadScene("SelectCharacter") ────┘
      |
      |  host bấm nút → StartGame.cs:32 LoadScene("MainGame")
      v
(4) MainGame  (trận đấu)
      |  - vòng lặp: di chuyển / bắn / bị đẩy / rơi
      |  - rơi y<-90 → trừ mạng → hồi sinh (0,180,0)
      |  - hết mạng → hiện chữ "<tên> đã thua"
      |  - nút ReturnToMenu → MainGame.cs:37 LoadScene("SelectCharacter")
      v
   (không có lối ra tự động — xem §2.3)

(5) GameOver  ── KHÔNG CÓ ĐƯỜNG VÀO TỪ RUNTIME (xem §3.2, §4.5)
```

### 2.2 Vòng đời trong trận — [HIỆN TRẠNG]

| Giai đoạn | Cái gì xảy ra | Nguồn |
|---|---|---|
| Spawn | Host load scene `MainGame` → spawn `Player.prefab` cho chính host | `Assets/Script/Network/MainGameManager.cs:43-54` |
| Chọn hình hài | Mỗi client gửi tên nhân vật đã chọn lên server, server bảo host instantiate prefab tương ứng và gán quyền sở hữu | `Assets/Script/PlayerBehavior/PlayerManager.cs:13-22`, `:25-34`, `:35-88` |
| Gắn tên | Owner gửi tên lên server, server phát lại cho mọi client để cập nhật text trên đầu nhân vật | `Assets/Script/PlayerBehavior/SetName.cs:29-33`, `:37-47`, `:50-55` |
| Chiến đấu | Owner bấm Space → ServerRpc → ClientRpc → mỗi máy tự sinh đạn | `Assets/Script/PlayerBehavior/PlayerShooting.cs:35-39`, `:43-49`, `:51-86` |
| Trúng đòn | Va chạm với đạn → `PushBackScript` set vận tốc đẩy lùi trong 0.2s | `Assets/Script/PlayerBehavior/PushBackScript.cs:55-116`, `:32-39` |
| Rơi | `y < -90` → `UpdateLifeCountServerRpc()` trừ 1 mạng | `MovingScripts.cs:77-80`, `:104-108` |
| Hồi sinh | Nếu `life_count > 0`: đặt lại vị trí `(0,180,0)`, vận tốc = 0 | `MovingScripts.cs:82-87` |
| Thua | Nếu hết mạng: instantiate prefab `Thongbaochet`, set chữ `"<tên> đã thua"` | `MovingScripts.cs:88-99` |
| Hậu thua | Nhánh không-phải-server còn gọi `Destroy(gameObject)` | `MovingScripts.cs:98` |

**[HIỆN TRẠNG]** Bảng báo thua tự mờ dần trong 3 giây rồi tự huỷ: `Assets/Script/PlayerBehavior/thongbaochet.cs:7` (`fadeDuration = 3.0f`), `:48` (`Destroy(gameObject)`).

### 2.3 Lỗ hổng vòng đời — [HIỆN TRẠNG]

1. Sau khi một người hết mạng, **trận không kết thúc**. Không có code nào chuyển scene, dừng trận, hay công bố người thắng.
2. Người chơi hết mạng trên máy host **không bị huỷ object** — nhánh `IsServer` tại `MovingScripts.cs:88-92` chỉ hiện chữ, không `Destroy`. Chỉ nhánh còn lại (`:93-99`) mới `Destroy(gameObject)`. Hai máy do đó kết thúc ở trạng thái khác nhau.
3. Điều kiện `y < -90` nằm trong `if (IsOwner)` (`MovingScripts.cs:70`), nên chỉ chủ sở hữu tự phát hiện mình rơi. Không có trọng tài phía server.

---

## 3. Sơ đồ luồng màn hình

### 3.1 Scene đăng ký trong build — [HIỆN TRẠNG]

`ProjectSettings/EditorBuildSettings.asset:7-22`

| # | Đường dẫn ghi trong build settings | GUID | File thật trên đĩa |
|---|---|---|---|
| 0 | `Assets/Scenes/MainMenu.unity` | `958a0af7…` | `MainMenu.unity` ✔ |
| 1 | `Assets/Scenes/SelectCharacter.unity` | `52fa5f01…` | `SelectCharacter.unity` ✔ |
| 2 | `Assets/Scenes/JoinRoom.unity` | `8efd1969…` | **`Joinroom.unity`** ✖ lệch hoa/thường |
| 3 | `Assets/Scenes/MainGame.unity` | `d98e6400…` | `MainGame.unity` ✔ |
| 4 | `Assets/Scenes/GameOver.unity` | `23af05ed…` | `GameOver.unity` ✔ |

**[HIỆN TRẠNG]** GUID `8efd19690c60d504fb47e2cdf94377b2` trong build settings khớp đúng `Assets/Scenes/Joinroom.unity.meta`, nên Unity vẫn phân giải được asset; chỉ chuỗi `path` là lệch chữ hoa.

**[UNKNOWN]** `SceneManager.LoadSceneAsync("JoinRoom")` (`JoinRoomManager.cs:31`, `:40`) và so sánh `scene.name == "JoinRoom"` (`SetCharacter.cs:43`) có khớp với scene tên thật `Joinroom` hay không. Trên Windows (hệ thống tệp không phân biệt hoa/thường) lịch sử commit cho thấy đang chạy được, nhưng tôi không chạy được build để xác nhận. Đây là rủi ro nếu sau này build trên máy/CI khác.

### 3.2 Bản đồ chuyển cảnh — [HIỆN TRẠNG]

Toàn bộ lời gọi `LoadScene` trong repo (quét `Assets/Script`):

| Từ | Hành động | Đến | Nguồn |
|---|---|---|---|
| MainMenu | nút Play | SelectCharacter | `MainMenu.cs:11` |
| MainMenu | nút Exit | thoát app | `MainMenu.cs:13-22` |
| SelectCharacter | nút StartHost | JoinRoom | `JoinRoomManager.cs:31`, `:63` |
| SelectCharacter | nút StartClient | JoinRoom | `JoinRoomManager.cs:40`, `:104` |
| SelectCharacter | ReturnToMenu | MainMenu | `CharacterSelection.cs:93` |
| Joinroom | host bấm nút bắt đầu | MainGame | `StartGame.cs:32` |
| Joinroom | Exit | SelectCharacter | `StartGame.cs:37` |
| Joinroom | ReturnToMenu (JoinRoomManager) | MainMenu | `JoinRoomManager.cs:48` |
| MainGame | ReturnToMenu | SelectCharacter | `MainGame.cs:37` |
| GameOver | Replay | MainMenu | `GameOverManager.cs..cs:24` |

**[HIỆN TRẠNG]** Không có bất kỳ `LoadScene("GameOver")` nào trong repo. Scene `GameOver` nằm trong build settings nhưng **không có đường vào từ runtime**.

**[HIỆN TRẠNG]** `CharacterSelection.cs:86` có dòng `// SceneManager.LoadScene("JoinRoom");` đã bị comment, và `CharacterSelection.PlayGame()` (`:61-87`) chỉ ghi `PlayerPrefs` rồi dừng — không chuyển cảnh. Việc chuyển cảnh thực tế do `JoinRoomManager` đảm nhiệm.

---

## 4. Bố cục UI từng scene

### 4.0 Cách xác định — phương pháp

Tôi đối chiếu GUID script (`*.cs.meta`) với nội dung `*.unity` / `*.prefab` để biết script nào thực sự được gắn ở đâu, thay vì suy từ tên file. Kết quả quan trọng: **các nút host/client và ô nhập IP nằm ở scene `SelectCharacter`, không phải scene `Joinroom`.**

- `JoinRoomManager.cs` (GUID `08c9c74fc714d7042b5cc28a14ab405f`) **không** xuất hiện trong bất kỳ `.unity` nào; nó được gắn trên `Assets/Script/Network/NetworkManger.prefab:246`.
- `NetworkManger.prefab` (GUID `a19273208c7e2a64fab51e0938ce1048`) chỉ được đặt trong **một** scene: `Assets/Scenes/SelectCharacter.unity`.
- Prefab này cũng chứa một `TMP_Text` tên `IP Host on border` (`NetworkManger.prefab:15`) — đây chính là `IPAddressHostOnScreen` được tô đỏ ở `JoinRoomManager.cs:79-80`.

### 4.1 Scene `MainMenu` — [HIỆN TRẠNG]

Script: `MainMenu.cs` (gắn trong `Assets/Scenes/MainMenu.unity`).

GameObject quan sát được (`m_Name` trong `MainMenu.unity`):

| Object | Dòng | Vai trò |
|---|---|---|
| `Main Camera` | `:537` | camera |
| `Canvas` | `:981` | gốc UI |
| `Image` | `:1085` | ảnh nền |
| `Mainmenu` | `:765` | nút vào game → `MainMenu.Room()` |
| `Button` | `:272` | nút (chưa xác định nhãn) |
| `Exit` | `:405` | nút thoát → `MainMenu.QuitGame()` |
| `Text (TMP)` ×3 | `:137`, `:630`, `:846` | nhãn chữ |

**[UNKNOWN]** Nút nào gắn với hàm nào: binding `onClick` nằm trong `m_PersistentCalls` của scene và tôi không giải mã từng `m_Target` fileID. Hai hàm public khả dụng chỉ có `Room()` và `QuitGame()` (`MainMenu.cs:8`, `:13`).

**Bố cục [ĐỀ XUẤT] để chuẩn hoá** — hiện chưa có trong scene:

```
+--------------------------------------------+
|              CRAZY SHOOTING                |   <- tiêu đề
|                                            |
|            [  CHƠI / PLAY  ]               |   <- Mainmenu (đã có)
|            [   HƯỚNG DẪN   ]               |   [ĐỀ XUẤT] chưa có
|            [   CÀI ĐẶT     ]               |   [ĐỀ XUẤT] chưa có
|            [   THOÁT       ]               |   <- Exit (đã có)
|                                   v1.0.0   |   [ĐỀ XUẤT] chưa có
+--------------------------------------------+
```

### 4.2 Scene `SelectCharacter` — [HIỆN TRẠNG]

Đây là màn **chọn nhân vật + nhập tên + tạo/vào phòng**, gộp làm một.

Script gắn trong scene: `CharacterSelection.cs`, `SetCharacter.cs`, `NetworkUI.cs`, cộng prefab `NetworkManger` (mang `JoinRoomManager.cs`, `NetworkManager`, `UnityTransport`, và script trong `MainGameManager.cs`).

| Object | Dòng trong `SelectCharacter.unity` | Vai trò |
|---|---|---|
| `CharacterSelectionTitle` | `:469` | tiêu đề |
| `LobbyTitle` | `:548` | tiêu đề khu phòng |
| `CharacterImage` | `:802` | ảnh nhân vật đang chọn (`CharacterSelection.cs:8`, `:48-52`) |
| `CharacterNameText` | `:3004` | tên nhân vật (`CharacterSelection.cs:52`) |
| `Next` | `:338` | → `CharacterSelection.NextCharacter()` (`:24-32`) |
| `Back` | `:206` | → `CharacterSelection.PreviousCharacter()` (`:35-43`) |
| `InputField (TMP) Name` | `:628` | tên người chơi (`SetCharacter.cs:8`, `:28`) |
| `InputField (TMP) Room` | `:2068` | **ô gõ IP của host** (`JoinRoomManager.IPAddressHost`, dùng ở `:37`, `:92`) |
| `StartHost` | `:1668` | tạo phòng (`JoinRoomManager.cs:28-32`) |
| `StartClient` | `:3084` | vào phòng (`JoinRoomManager.cs:34-41`) |
| `UIManagerNetwork` | `:1787` | giữ `NetworkUI.cs` (toàn bộ thân hàm đã bị comment, `NetworkUI.cs:14-25`) |
| `BackGround` | `:1028` | nền |
| `Exit` | `:2873` | → `CharacterSelection.ReturnToMenu()` (`:90-94`) |
| `Placeholder` ×2 | `:2243`, `:2398` | chữ gợi ý trong 2 InputField |

**[HIỆN TRẠNG]** Nhân vật được lưu theo **tên sprite**, không phải theo chỉ số: `SetCharacter.cs:66-67` lấy `spriteRenderer.sprite.name`. Về sau `PlayerManager.cs:52-71` và `SetSlot.cs:13-24` so sánh chuỗi đó với `"Assassin"`, `"Cowboy"`, `"Madman"`, `"Robot"`.

**[HIỆN TRẠNG]** Tồn tại **hai** đường ghi tên người chơi, không đồng bộ nhau:
- `CharacterSelection.PlayGame()` ghi `PlayerPrefs` các khoá `PlayerName`, `RoomName`, `SelectedCharacter`, `SelectedCharacterIndex` (`:80-83`) — nhưng `PlayGame()` **không nằm trên luồng hiện tại** (nút vào phòng đã là StartHost/StartClient).
- `SetCharacter.OnValueChanged` ghi `PlayerPrefs["PlayerName"]` và `PlayerPrefs["PlayerCharacter"]` mỗi lần gõ phím (`:56-68`) — đây mới là đường sống.

**[HIỆN TRẠNG]** Ô `InputField (TMP) Room` được đặt tên như "tên phòng" nhưng thực tế chứa **địa chỉ IP**: `JoinRoomManager.cs:37` gán `ipAddress = IPAddressHost.text`, rồi `:138` parse bằng `IPAddress.TryParse`. Đây là điểm gây hiểu nhầm nặng nhất cho người chơi mới.

**Bố cục [ĐỀ XUẤT] sau khi chuyển sang Relay** (chi tiết thiết kế mạng nằm ở `TECHNICAL_ARCHITECTURE.md`):

```
+--------------------------------------------------------+
|  CHỌN NHÂN VẬT                                         |
|                                                        |
|   [<]      ( ảnh nhân vật )      [>]                   |
|              Cowboy                                    |
|                                                        |
|  Tên của bạn: [________________]                       |
|                                                        |
|  ---------------- TẠO HOẶC VÀO PHÒNG ----------------  |
|  [  TẠO PHÒNG  ]     mã phòng: [ A7K2QX ]  [ VÀO ]     |
|                      ^ 6 ký tự, không phải IP          |
|  trạng thái: Đang kết nối...                           |
|                                                        |
|  [ QUAY LẠI ]                                          |
+--------------------------------------------------------+
```

### 4.3 Scene `Joinroom` (phòng chờ) — [HIỆN TRẠNG]

Script gắn trong scene: `StartGame.cs`, `SetName.cs`, `SetSlot.cs`, `SetCharacter.cs`.

| Object | Dòng trong `Joinroom.unity` | Vai trò |
|---|---|---|
| `Canvas` ×2 | `:370`, `:1108` | gốc UI |
| `Panel` | `:619` | khung phòng |
| `SlotHost` | `:284` | ô người chơi 1 |
| `SlotCharacter` | `:695` | ô nhân vật (`SetSlot.cs:12-24` gán sprite theo nhân vật đã chọn) |
| `PlayerName` | `:138` | tên người chơi |
| `NameRoom` | `:972` | tên/mã phòng |
| `StartHost` | `:826` | nút bật khi là host (`StartGame.cs:19`) |
| `StartClient` | `:473` | nút bật khi là client (`StartGame.cs:26`) |
| `Image` | `:1228` | nền |
| `Exit` | `:1664` | → `StartGame.ReturnToMenu()` |
| `Text (TMP)` ×2 | `:1303`, `:1437` | nhãn |

**[HIỆN TRẠNG]** Phòng chờ chỉ có **một** slot (`SlotHost` + `SlotCharacter`). Không có danh sách người chơi động, không có hiển thị dạng "2/4 người đã vào". `SetSlot.cs:12-24` chỉ đọc biến static `SetCharacter.selectedCharacterSprite` của **máy cục bộ**, nên mỗi máy chỉ nhìn thấy nhân vật của chính mình.

**[HIỆN TRẠNG]** Nút `StartClient` được bật cho client (`StartGame.cs:26`), và lớp `StartGame` chỉ có đúng một hàm chuyển cảnh là `NextScene()` → `LoadScene("MainGame")` (`StartGame.cs:30-33`).
**[UNKNOWN]** Nút của client có thực sự gọi `NextScene()` hay không — cần đọc binding `onClick` trong Editor. Nếu có, client tự chuyển scene độc lập với host, không đi qua `NetworkManager.SceneManager`.

**[ĐỀ XUẤT]** Phòng chờ nên hiển thị: mã phòng (copy được), danh sách 2–4 slot đồng bộ qua mạng, trạng thái sẵn sàng, và nút Bắt đầu chỉ host bấm được.

### 4.4 Scene `MainGame` (trận đấu) — [HIỆN TRẠNG]

Script gắn trong scene: `MainGame.cs`.

| Object | Dòng trong `MainGame.unity` | Vai trò |
|---|---|---|
| `Main Camera` | `:356` | camera |
| `Grid` | `:271` | tilemap gốc |
| `BaseGround` | `:941` | nền chính (tag `Ground`) |
| `SecondGround` | `:450` | nền phụ xuyên qua được (tag `SecondGround`) |
| `Daylight` | `:1327` | ánh sáng/nền |
| `Canvas` | `:1490` | gốc UI |
| `Panel` | `:1412` | `rolePanel` — bảng báo vai trò (`MainGame.cs:8`) |
| `Text (TMP)` | `:137` | chữ trong panel |

**[HIỆN TRẠNG]** `rolePanel` ẩn ngay với host, còn client thì hiện `panelHideDelay = 5f` giây rồi ẩn (`MainGame.cs:9`, `:14-24`). Đây là **UI duy nhất** trong trận, ngoài tên trên đầu nhân vật.

**[HIỆN TRẠNG]** Không có HUD mạng: số mạng còn lại không được vẽ ra UI ở bất cứ đâu (tìm `life_count` chỉ ra `MovingScripts.cs:32`, `:82`, `:107` — không vị trí nào là UI), không có đồng hồ, không có bảng tỉ số.

**[HIỆN TRẠNG]** Tên người chơi hiện trên đầu nhân vật qua `SetName.nameDisplay` (`SetName.cs:11`, `:53`); component `SetName` gắn trên 4 prefab nhân vật (`Assets/Sprite/Player/Prefab/{Assassin,Cowboy,Madman,Robot}.prefab`). Canvas trên đầu nhân vật luôn quay về camera nhờ `FaceCamera.cs` gắn ở `Assets/Sprite/Player/Prefab/Canvas.prefab`.

**Bố cục [ĐỀ XUẤT] HUD trong trận** — hiện chưa có:

```
+--------------------------------------------------------+
| ♥♥♥ Khoa            [ Esc: Tạm dừng ]        ♥♥  Nam   |
|                                                        |
|                  ( khu vực chơi )                      |
|                                                        |
|                                                        |
|              [ Khoa đã thua ]  <- chữ đỏ, mờ dần 3s    |
+--------------------------------------------------------+
```

### 4.5 Scene `GameOver` — [HIỆN TRẠNG]

Script gắn trong scene: `GameOverManager.cs..cs` (tên file có **hai** đuôi `.cs`), `StartGame.cs`.

| Object | Dòng trong `GameOver.unity` | Vai trò |
|---|---|---|
| `'GameOverCanvas '` | `:432` | canvas kết thúc (tên object có dấu nháy và khoảng trắng thừa) |
| `'GameOverText '` | `:759` | chữ kết quả |
| `BackGround` | `:534` | nền |
| `Replay` | `:973` | → `GameOverManager.RetryGame()` → MainMenu (`GameOverManager.cs..cs:21-25`) |
| `Quit` | `:839` | → `GameOverManager.QuitGame()` (`:27-36`) |
| `Main Camera` | `:339` | camera |

**[HIỆN TRẠNG]** `GameOverManager.Start()` gọi `gameOverCanvas.SetActive(false)` (`:10`) — tức canvas bị **ẩn ngay khi scene mở**, và chỉ hiện lại khi ai đó gọi `GameOver()` (`:14-18`). Không có code nào gọi `GameOver()` (tìm toàn `Assets/Script`). Kết hợp với việc không có `LoadScene("GameOver")`, scene này hiện **không bao giờ hiện ra**.

**[HIỆN TRẠNG]** `EndSceneCode.cs` — script duy nhất sinh chữ `"<tên> is the winner"` (`:24`) — được gắn trên `Assets/Scenes/Canvas.prefab` (GUID `e0067d3063c7c684590f26bd08e06394`), và prefab đó **không được tham chiếu bởi scene hay prefab nào khác**. Đây là nhánh chết.

---

## 5. Luật thắng/thua

### 5.1 Hiện trạng — [HIỆN TRẠNG]

| Hạng mục | Giá trị | Nguồn |
|---|---|---|
| Số mạng | 3 | `MovingScripts.cs:32` |
| Điều kiện mất mạng | `transform.position.y < -90` | `MovingScripts.cs:77` |
| Ai phát hiện | chỉ owner (nằm trong `if (IsOwner)`) | `MovingScripts.cs:70` |
| Ai trừ mạng | server, qua `UpdateLifeCountServerRpc` | `MovingScripts.cs:104-108` |
| Hồi sinh | `(0, 180, 0)`, vận tốc 0 | `MovingScripts.cs:85-86` |
| Khi hết mạng | hiện chữ `"<tên> đã thua"` | `MovingScripts.cs:90-96` |
| Điều kiện thắng | **không tồn tại** | — |
| Kết thúc trận | **không tồn tại** | — |
| Chuyển màn kết quả | **không tồn tại** | §3.2 |

**[HIỆN TRẠNG]** Sát thương không làm mất mạng trực tiếp. Mạng chỉ mất khi rơi khỏi màn. Vũ khí là **công cụ đẩy**, không phải công cụ gây damage: `PushBackScript` chỉ set vận tốc (`:46`, `:50`), không chạm vào `life_count`.

**[HIỆN TRẠNG]** Có hai điểm nghi vấn logic trong khối thua (`MovingScripts.cs:77-100`):

1. `UpdateLifeCountServerRpc()` được gọi ở `:80`, **trước** khi đọc `life_count.Value` ở `:82`. Vì NetworkVariable cập nhật bất đồng bộ qua mạng, giá trị đọc ở `:82` là giá trị **trước khi trừ**.
   **[UNKNOWN]** Hệ quả chính xác (người chơi được thêm một lần hồi sinh, hay đúng 3 mạng) — cần chạy thử mới kết luận.
2. Khối `if (this.transform.position.y < -90)` nằm trong `Update()`, nên về nguyên tắc có thể kích hoạt nhiều khung hình liên tiếp cho một lần rơi.
   **[UNKNOWN]** Mức độ xảy ra thực tế — việc đặt lại vị trí ngay trong cùng khung hình (`:85`) có thể che bug này; cần test mới kết luận.

### 5.2 Luật đề xuất — [ĐỀ XUẤT]

Chưa có trong code. Đề nghị tối thiểu để trận có kết thúc:

- Trận kết thúc khi chỉ còn **đúng một** người chơi có `life_count > 0`.
- Server (host) là trọng tài duy nhất: chỉ server kiểm tra điều kiện kết thúc, rồi phát `GameOverClientRpc(winnerName, winnerClientId)`.
- Server dùng `NetworkManager.SceneManager.LoadScene("GameOver", LoadSceneMode.Single)` để cả hai máy cùng sang màn kết quả, thay cho `SceneManager.LoadScene` cục bộ.
- `GameOver` hiển thị tên người thắng lấy từ RPC, **không** lấy từ `FindGameObjectWithTag("Player")`.
- Nếu tất cả cùng hết mạng trong cùng một khung hình: hoà, hiển thị "Hoà".
- Phát hiện rơi nên chuyển về server: server đọc vị trí và trừ mạng, client chỉ gửi input.

---

## 6. Cân bằng 4 vũ khí

### 6.1 Ánh xạ nhân vật → vũ khí — [HIỆN TRẠNG]

Xác minh bằng GUID trường `bulletPrefab` serialize trên từng prefab nhân vật:

| Nhân vật | Prefab nhân vật | `bulletPrefab` GUID | Đạn | Nguồn |
|---|---|---|---|---|
| Assassin | `Assets/Sprite/Player/Prefab/Assassin.prefab` | `a25ea4d5…` | `Slashline.prefab` (vệt chém) | `Assassin.prefab:321` |
| Cowboy | `…/Cowboy.prefab` | `3014c5c4…` | `PistolBullet.prefab` | `Cowboy.prefab:324` |
| Madman | `…/Madman.prefab` | `1b0f4463…` | `Bom.prefab` | `Madman.prefab:355` |
| Robot | `…/Robot.prefab` | `8a1eaba2…` | `SniperBullet.prefab` | `Robot.prefab:163` |

GUID đối chiếu từ `Assets/Sprite/SpawnableObject/Prefab/*.prefab.meta`.

**[HIỆN TRẠNG]** Việc chọn **animation** lại dựa trên chuỗi tên GameObject chứ không dựa trên prefab: `PlayerShooting.cs:54` `gameObject.name.Contains("Cowboy")`, `:59` `"Robot"`, `:65` `"Madman"`, `:71` `"Assassin"`. Nếu tên object đổi thì animation hỏng nhưng đạn vẫn bắn, vì `Instantiate` ở `:78` nằm ngoài mọi nhánh `if`.

### 6.2 Thông số thực tế của 4 loại đạn — [HIỆN TRẠNG]

| Thuộc tính | Pistol (Cowboy) | Sniper (Robot) | Bom (Madman) | Slash (Assassin) |
|---|---|---|---|---|
| Script | `BulletPistol.cs` | `BulletSniper.cs` | `Bom.cs` | `SlashingLine.cs` |
| Thời gian sống | 4s (`:21`) | 4s (`:24`) | nổ sau 3s (prefab `:205`) | **0.1s** (`:21`) |
| `gravityScale` | `0` (prefab `:108`) | `0` (prefab `:108`) | `9` (prefab `:108`) | `1` (prefab `:168`) |
| `m_Mass` | 1 (prefab `:105`) | 1 (prefab `:105`) | 3 (prefab `:105`) | 1 (prefab `:165`) |
| Tag | `PistolBullet` (prefab `:21`) | **`PistolBullet`** (prefab `:21`) | **`Player`** (prefab `:21`) | `SlashLine` (prefab `:122`) |
| Scale khi chạy | `(0.8, 0.8, 1.4)` (`:22`) | `(7, 2, 1)` (`:24`) | giữ nguyên | `(2.2, 2.2, 2.2)` (`:22`) |
| Hành vi riêng | huỷ khi trúng Player (`:40-44`) | **xuyên thấu**: bỏ qua va chạm và giữ nguyên vận tốc (`:39-51`) | nảy trên đất, đẩy khi chạm Player (`:41-55`), nổ ra `ExpAnimator 2` (`:64-77`) | không huỷ khi trúng, tự hết hạn (`:38-46`) |
| Tốc độ ban đầu | `transform.right * 180` | `transform.right * 180` | `transform.right * 180` | `transform.right * 180` |

Tốc độ `180` là **chung cho cả bốn** — gán cứng ở `PlayerShooting.cs:24` (`bulletSpeed = 180f`) rồi áp ở `:84`. Nhịp bắn cũng **chung**: `TimeShoot = 0.5f` (`PlayerShooting.cs:27`), tức 2 phát/giây cho mọi nhân vật.

### 6.3 Lực đẩy — [HIỆN TRẠNG] tất cả **giống hệt nhau**

`PushBackScript` gắn trên cả 4 prefab nhân vật. Giá trị serialize trên prefab đều bằng `0` (ví dụ `Cowboy.prefab:337-342`, `Robot.prefab:176-181`, `Assassin.prefab:334-339`, `Madman.prefab:368-373`) và bị **ghi đè toàn bộ** trong `Start()`:

| Tham số | Giá trị | Nguồn |
|---|---|---|
| `ShotForce` (lực hất lên) | `90` | `PushBackScript.cs:26` |
| `BomForce` (lực đẩy ngang) | `80` | `PushBackScript.cs:25` |
| `ShotForceTotalTime` | `0.2` giây | `PushBackScript.cs:28` |

Nghĩa là: **trúng đạn lục, đạn bắn tỉa, bom hay vệt chém đều bị đẩy đúng một lượng như nhau** — `PushBackScript.PushBack()` (`:42-52`) set thẳng `velocity = (±80, 90)`, không phân biệt loại đạn. Bốn vũ khí hiện chỉ khác nhau về **hình dạng đòn đánh** (tầm, độ xuyên, quỹ đạo), không khác về sức mạnh.

### 6.4 Lỗ hổng cân bằng tìm được — [HIỆN TRẠNG]

1. **Bom không kích hoạt được knockback của `PushBackScript`.** `PushBackScript.cs:73` kiểm tra `CompareTag("bomno")`, nhưng `Bom.prefab:21` có tag `Player`. Tag `bomno` thực ra nằm trên prefab hiệu ứng nổ `Assets/Sprite/SpawnableObject/Prefab/ExpAnimator 2.prefab:18`. Vậy bom chỉ đẩy được **sau khi nổ**, qua va chạm với hiệu ứng nổ — mà hiệu ứng nổ bị huỷ sau 0.2s (`Bom.cs:72` và `DestroyCtr.cs:16-19`). Cửa sổ trúng đòn rất hẹp. Xác nhận chuỗi liên kết: `Bom.prefab:198` `explosionEffectPrefab` trỏ GUID `a79abfe77fde74a46a54e323540cec03` = `ExpAnimator 2.prefab`.
2. **Bom bị gán tag `Player`.** Các hệ quả dây chuyền sau đây là **[ĐỀ XUẤT kiểm chứng]**, chưa test: `BulletPistol.cs:40` sẽ huỷ đạn khi chạm bom; `Bom.cs:43` sẽ coi bom khác là người chơi và đẩy nó; `EndSceneCode.cs:16` `FindGameObjectWithTag("Player")` có thể trả về một quả bom.
3. **Sniper dùng tag của pistol** (`SniperBullet.prefab:21` = `PistolBullet`), nên vẫn đẩy được, nhưng bảng tag mất ý nghĩa phân loại.
4. **`BulletSniper` xuyên mọi thứ vĩnh viễn**: `:43-50` bỏ qua va chạm với cả `Player` lẫn `Ground`, đồng thời reset vận tốc về `initialVelocity`; sống 4s với tốc độ 180 → đi rất xa, không bị chặn. Đây là vũ khí mạnh nhất về mặt cơ học.
5. **Vệt chém sống 0.1 giây** (`SlashingLine.cs:21`) nhưng vẫn bay với tốc độ 180 như đạn → tầm hiệu quả rất ngắn. Về ý đồ đây là đòn cận chiến, nhưng cài đặt lại là một viên đạn tầm cực ngắn.
6. **Không có vũ khí nào gắn với chủ nhân.** `PlayerShooting.cs:79` chỉ `IgnoreCollision` giữa đạn và **người bắn**; không có khái niệm "ai bắn ai", nên không thống kê được điểm hạ gục.

### 6.5 Bảng cân bằng đề xuất — [ĐỀ XUẤT]

Chưa có trong code. Đề nghị tách lực đẩy theo loại đạn để 4 nhân vật thực sự khác nhau:

| Vũ khí | Nhịp bắn | Tốc độ đạn | Đẩy ngang | Đẩy dọc | Ghi chú thiết kế |
|---|---|---|---|---|---|
| Pistol (Cowboy) | 0.4s | 180 | 70 | 80 | cơ bản, dễ dùng |
| Sniper (Robot) | 1.2s | 260 | 110 | 60 | mạnh, tầm xa, bắn chậm; **nên bỏ xuyên thấu vô hạn** |
| Bom (Madman) | 1.5s | 120 (ném vòng cung) | 120 | 120 | sát thương vùng; cần sửa tag `bomno` |
| Slash (Assassin) | 0.3s | — (hitbox cận chiến) | 60 | 110 | tầm cực ngắn, hất cao, cơ động |

Kèm đề nghị: chuyển các hằng số đang hard-code trong `Start()` / `OnNetworkSpawn()` sang `ScriptableObject` cấu hình, để chỉnh cân bằng không phải sửa code.

---

## 7. Điều khiển — [HIỆN TRẠNG]

| Phím | Hành động | Nguồn |
|---|---|---|
| `A` / `D` | di chuyển trái / phải | `MovingScripts.cs:206`, `:219` |
| `W` | nhảy (tối đa 2 lần trước khi chạm đất) | `MovingScripts.cs:152`; `jumpCount = 2` tại `:47` |
| `S` | rơi nhanh, xuyên qua `SecondGround` | `MovingScripts.cs:166-176` |
| `Space` | tấn công | `PlayerShooting.cs:35` |
| `G` | ném lựu đạn | `PlayerThrowing.cs:27` — script này **không được gắn trên prefab/scene nào**, nên phím G hiện vô tác dụng |

**[HIỆN TRẠNG]** Tốc độ ngang: `rightForce * speed = 30 * 3 = 90`, bị kẹp bởi `maxHorizontalSpeed = 40` (`MovingScripts.cs:44-46`, `:63`, `:229`). Lực nhảy `upwardForce = 80` (`:42`).

**[HIỆN TRẠNG]** Không có cấu hình lại phím, không hỗ trợ tay cầm, không dùng Input System mới — toàn bộ qua `Input.GetKey*` cũ.

**[HIỆN TRẠNG]** Tất cả thông số di chuyển được gán cứng trong `MovingScripts.OnNetworkSpawn()` (`:42-48`, `:63`), **giống nhau cho cả 4 nhân vật** — tức 4 nhân vật hiện không khác nhau về tốc độ, lực nhảy hay số lần nhảy.

---

## 8. Nội dung còn thiếu

### 8.1 Thiếu về thiết kế game — [HIỆN TRẠNG: chưa tồn tại]

| Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|
| Điều kiện thắng | không có | §5.1 |
| Màn hình kết quả hoạt động | không có đường vào | §4.5 |
| Hiển thị số mạng còn lại | không có | tìm `life_count` chỉ ra 3 vị trí, không vị trí nào là UI |
| Tạm dừng (pause) | không có | tìm `Time.timeScale` chỉ thấy các chỗ *khôi phục* về `1f`: `MainGame.cs:36`, `StartGame.cs:36`, `CharacterSelection.cs:92`, `JoinRoomManager.cs:47`, `GameOverManager.cs..cs:23`; chỗ duy nhất đặt `0f` là `GameOverManager.cs..cs:17` trong hàm `GameOver()` vốn không ai gọi |
| Màn hình "đang kết nối" | không có | `JoinRoomManager` chỉ `Debug.Log` |
| Báo lỗi nhập sai IP cho người chơi | không có | `JoinRoomManager.cs:140`, `:150` chỉ `Debug.LogError` — người chơi không thấy gì |
| Thông báo mất kết nối | không có | không có `OnClientDisconnectCallback` trong repo |
| Cài đặt âm lượng / độ phân giải | không có | không có script nào chạm `AudioMixer` hoặc `Screen.SetResolution` |
| Lưu tên giữa các phiên | có một nửa | `SetCharacter.cs:57` ghi `PlayerPrefs`, nhưng không có code nào đọc lại để điền sẵn vào InputField khi mở game |
| Hướng dẫn chơi | không có | không có scene/panel nào |
| Giới hạn số người chơi | không có | không có kiểm tra `ConnectedClients.Count` |
| Nút thoát trong trận | không có | `MainGame.cs:34-38` có `ReturnToMenu()`, nhưng **[UNKNOWN]** có nút nào trong `MainGame.unity` gắn vào hàm này không — danh sách object của scene không có object tên gợi ý nút thoát |
| Âm thanh bắn/nổ/nhạc nền | **[UNKNOWN]** | không có script audio nào trong `Assets/Script`; `AudioSource` có thể gắn trực tiếp trên prefab — cần mở Editor để xác nhận |

### 8.2 Tài sản chết (dead asset) — [HIỆN TRẠNG]

Các script **không** được tham chiếu bởi bất kỳ `.unity` hay `.prefab` nào trong `Assets/` (đối chiếu GUID):

| Script | Ghi chú |
|---|---|
| `Assets/Script/GroundBehavior/PassThrough.cs` | không nơi nào dùng |
| `Assets/Script/Network/NetworkManagerSpawn.cs` | kế thừa `NetworkManager` (`:6`) nhưng không được dùng |
| `Assets/Script/PlayerBehavior/PlayerSlashing.cs` | trùng chức năng với `PlayerShooting` |
| `Assets/Script/PlayerBehavior/PlayerThrowing.cs` | phím `G` chết |
| `Assets/Script/Scene/Loader.cs` | thân hàm rỗng (`:8-17`) |
| `Assets/Script/Scene/waitingtext.cs` | chứa class `RoleTextManager` (`:5`), không được gắn ở đâu |
| `Assets/Script/Scene/EndSceneCode.cs` | chỉ gắn trên `Assets/Scenes/Canvas.prefab`, prefab đó không được dùng |
| `Assets/Script/Scene/CharacterSelection.cs` → `PlayGame()` | script còn sống trong scene, nhưng riêng hàm `PlayGame()` không nằm trên luồng nào |

**[HIỆN TRẠNG]** Ba file có **tên file lệch tên class**, một loại lỗi dễ gây mất component:

- `Assets/Script/Network/MainGameManager.cs:8` → `class GameNetworkManager`
- `Assets/Script/Scene/GameOverManager.cs..cs:4` → `class GameOverManager` (file có hai đuôi `.cs`)
- `Assets/Script/Scene/waitingtext.cs:5` → `class RoleTextManager`

**[UNKNOWN]** Unity có nạp được các component này lúc chạy hay không. Bằng chứng đọc được chỉ cho thấy tham chiếu **vẫn tồn tại** trong asset: `Assets/Script/Network/NetworkManger.prefab:278` trỏ tới GUID `9552d136f023b3d4fa0183e092dfc218` (`MainGameManager.cs`) và còn giữ nguyên giá trị serialize (`:281-283` — `playerPrefab`, `particleSystemCloud`, `rocketSpawnerPrefab`); `Assets/Scenes/GameOver.unity` trỏ tới GUID của `GameOverManager.cs..cs`. Phải mở Unity Editor để xem có báo *"The associated script can not be loaded"* hay không.
**Lưu ý quan trọng: không được kết luận đây là code chết chỉ dựa trên tên file** — `MainGameManager.cs` chính là nơi chứa logic spawn người chơi khi vào `MainGame` (`:43-54`), nên nếu nó thật sự không nạp được thì game không spawn được nhân vật nào. Điều này mâu thuẫn với lịch sử commit, nên câu trả lời thực tế gần như chắc chắn là "Unity vẫn nạp được"; nhưng tôi không xác minh được bằng cách đọc bytes.

### 8.3 Vấn đề cấu hình tag — [HIỆN TRẠNG]

`ProjectSettings/TagManager.asset:6-9` chỉ khai báo **ba** tag tuỳ biến:

```
tags:
- Enemy
- Ground
- Object
```

Nhưng code và asset dùng các tag sau:

| Tag được dùng | Khai báo? | Dùng ở |
|---|---|---|
| `Player` | ✔ (tag dựng sẵn của Unity) | 7 prefab; `BulletPistol.cs:40`, `Bom.cs:43`, `SlashingLine.cs:41`, `EndSceneCode.cs:16` |
| `MainCamera` | ✔ (dựng sẵn) | các scene |
| `Ground` | ✔ | `Bom.cs:50`, `BulletSniper.cs:41` |
| `SecondGround` | ✖ **không khai báo** | `MovingScripts.cs:53`, `RocketBehavior.cs:44`, `MainGame.unity:450` |
| `PistolBullet` | ✖ **không khai báo** | `PushBackScript.cs:58`; `PistolBullet.prefab:21`, `SniperBullet.prefab:21` |
| `SlashLine` | ✖ **không khai báo** | `PushBackScript.cs:88`, `:102`; `Slashline.prefab:122` |
| `bomno` | ✖ **không khai báo** | `PushBackScript.cs:73`; `ExpAnimator 2.prefab:18` |

`git status --porcelain ProjectSettings/TagManager.asset` trả về rỗng → file này **đúng như đã commit**, không phải thay đổi cục bộ chưa lưu.

**[UNKNOWN]** Hệ quả thực tế. `GameObject.CompareTag` / `FindWithTag` với tag chưa khai báo thường ném `UnityException` trong Unity; nếu vậy `MovingScripts.OnNetworkSpawn` (`:53`) phải hỏng ngay khi spawn. Nhưng lịch sử commit (`3fe2d9a add life count for each player`) cho thấy game từng chạy được. Hai khả năng: (a) trạng thái Editor trên máy Human khác với file đã commit, (b) phiên bản Unity này xử lý khác. **Phải mở Editor kiểm tra trước khi build phát hành** — mục kiểm tra này sẽ được đưa vào `BUILD_RELEASE_CHECKLIST.md`.

### 8.4 Thiếu về nội dung chơi — [ĐỀ XUẤT]

Chưa có trong code; đề nghị theo thứ tự ưu tiên:

1. **Bắt buộc để game "xong"**: điều kiện thắng + màn kết quả + HUD số mạng + thông báo mất kết nối.
2. **Nên có**: pause, màn hình "đang kết nối", báo lỗi vào phòng hiển thị cho người chơi, lưu/điền sẵn tên, nút thoát trong trận.
3. **Có thì tốt**: thêm map thứ hai, cân bằng riêng từng vũ khí, âm thanh, cài đặt âm lượng/độ phân giải, hướng dẫn chơi.

---

## 9. Phụ lục — bảng truy nguồn

Toàn bộ file đã tự đọc để lập tài liệu này:

| File | Dùng cho mục |
|---|---|
| `.git/HEAD` | xác nhận branch |
| `Packages/manifest.json` | phiên bản Netcode |
| `ProjectSettings/EditorBuildSettings.asset` | §3.1 |
| `ProjectSettings/TagManager.asset` | §8.3 |
| `Assets/DefaultNetworkPrefabs.asset` | danh sách 13 network prefab (dùng cho tài liệu kỹ thuật) |
| `Assets/Script/Scene/MainMenu.cs` | §2, §3, §4.1 |
| `Assets/Script/Scene/CharacterSelection.cs` | §3, §4.2 |
| `Assets/Script/Scene/JoinRoomManager.cs` | §2, §3, §4.2 |
| `Assets/Script/Scene/StartGame.cs` | §2, §3, §4.3 |
| `Assets/Script/Scene/MainGame.cs` | §3, §4.4 |
| `Assets/Script/Scene/GameOverManager.cs..cs` | §4.5 |
| `Assets/Script/Scene/EndSceneCode.cs` | §4.5, §8.2 |
| `Assets/Script/Scene/Loader.cs`, `waitingtext.cs` | §8.2 |
| `Assets/Script/Network/MainGameManager.cs` | §2.2, §8.2 |
| `Assets/Script/Network/NetworkUI.cs`, `NetworkManagerSpawn.cs` | §4.2, §8.2 |
| `Assets/Script/PlayerBehavior/MovingScripts.cs` | §1, §2, §5, §7 |
| `Assets/Script/PlayerBehavior/PlayerShooting.cs` | §2.2, §6 |
| `Assets/Script/PlayerBehavior/PlayerManager.cs` | §2.2 |
| `Assets/Script/PlayerBehavior/SetName.cs` | §2.2, §4.4 |
| `Assets/Script/PlayerBehavior/SetCharacter.cs` | §3, §4.2 |
| `Assets/Script/PlayerBehavior/SetSlot.cs` | §4.3 |
| `Assets/Script/PlayerBehavior/PushBackScript.cs` | §6.3, §6.4 |
| `Assets/Script/PlayerBehavior/thongbaochet.cs` | §2.2 |
| `Assets/Script/PlayerBehavior/PlayerSlashing.cs`, `PlayerThrowing.cs` | §7, §8.2 |
| `Assets/Script/WeaponBehavior/{BulletPistol,BulletSniper,Bom,SlashingLine,DestroyCtr}.cs` | §6.2, §6.4 |
| `Assets/Scenes/{MainMenu,SelectCharacter,Joinroom,MainGame,GameOver}.unity` | §4 |
| `Assets/Scenes/Canvas.prefab` | §4.5 |
| `Assets/Script/Network/NetworkManger.prefab` | §4.0, §8.2 |
| `Assets/Sprite/Player/Prefab/{Assassin,Cowboy,Madman,Robot,Player,Character}.prefab` | §6.1, §6.3 |
| `Assets/Sprite/SpawnableObject/Prefab/{PistolBullet,SniperBullet,Bom,Slashline,Rocket,ExpAnimator 2}.prefab` | §6.2, §6.4 |

### Bảng GUID đã dùng để truy vết

| Asset | GUID |
|---|---|
| `Joinroom.unity` | `8efd19690c60d504fb47e2cdf94377b2` |
| `NetworkManger.prefab` | `a19273208c7e2a64fab51e0938ce1048` |
| `JoinRoomManager.cs` | `08c9c74fc714d7042b5cc28a14ab405f` |
| `MainGameManager.cs` | `9552d136f023b3d4fa0183e092dfc218` |
| `PlayerShooting.cs` | `2d9635ffd1e92f74c9954b6be32d9da4` |
| `PushBackScript.cs` | `9ae9d6678b9b7604096fa8a090ff298b` |
| `Assassin` / `Cowboy` / `Madman` / `Robot` `.prefab` | `b7eaf4b5…` / `5ac330d6…` / `864d33fe…` / `d564c729…` |
| `Slashline` / `PistolBullet` / `Bom` / `SniperBullet` `.prefab` | `a25ea4d5…` / `3014c5c4…` / `1b0f4463…` / `8a1eaba2…` |
| `ExpAnimator 2.prefab` | `a79abfe77fde74a46a54e323540cec03` |
| `Assets/Scenes/Canvas.prefab` (chứa `EndSceneCode`) | `e0067d3063c7c684590f26bd08e06394` |
