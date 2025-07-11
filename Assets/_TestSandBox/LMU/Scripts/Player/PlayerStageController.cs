using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

/// <summary>
/// 스테이지에 의해 통제되는 컴포넌트
/// </summary>
public class PlayerStageController : NetworkBehaviour
{
    public void SetPosition(Vector2 position)
    {
        var rigid = GetComponent<NetworkRigidbody2D>();
        rigid.Teleport(position);
    }
}
