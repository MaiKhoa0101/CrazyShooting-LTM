using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// NetworkTransform do CHỦ SỞ HỮU điều khiển, không phải server.
///
/// Vì sao cần:
/// MovingScripts cho owner tự set linearVelocity và transform.position
/// (MovingScripts.cs:146, :155, :169, :232). Nếu dùng NetworkTransform mặc định
/// của Unity — vốn là server-authoritative — thì server sẽ liên tục kéo nhân vật
/// về vị trí của nó và người chơi sẽ thấy mình bị giật ngược mỗi khi di chuyển.
/// Cho owner giữ quyền thì cách điều khiển hiện tại chạy nguyên vẹn.
///
/// Thành phần này thay cho component cũ đã mất script sau khi gỡ package
/// com.unity.multiplayer.samples.coop. Component cũ mang GUID
/// df2868252ab5c4d1da357e8f11f1b524, không còn .meta nào phân giải được,
/// và vẫn đang nằm lại trên 6 prefab dưới dạng "Missing Script".
///
/// CHỈ gắn lên 4 prefab nhân vật (Assassin, Cowboy, Madman, Robot).
/// KHÔNG gắn lên Canvas.prefab — xem lý do trong báo cáo bàn giao.
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    /// <summary>
    /// Trả về false để chuyển quyền điều khiển từ server sang owner.
    /// Ghi đè NetworkTransform.OnIsServerAuthoritative()
    /// (NetworkTransform.cs:4550 trong package 2.13.3).
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
