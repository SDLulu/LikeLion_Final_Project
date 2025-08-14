using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

/// <summary>
/// 스테이지에 의해 통제되는 컴포넌트
/// </summary>
public class PlayerStageController : NetworkBehaviour
{
    /// <summary>
    /// Note : 매프레임 호출하니 갑자기 위치가 튀는 문제 발생
    /// 내부적으로 RPC 쓰는것 같긴한데 그래서 문제가 발생하는듯?
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        if (Runner.IsServer == false)
            return;

        var rigid = GetComponent<NetworkRigidbody2D>();
        rigid.Teleport(position);
    }

    public Vector2 GetPosition()
    {
        var rigid = GetComponent<NetworkRigidbody2D>();
        var ret = rigid.RBPosition;
        return ret;
    }

    public Vector2 GetScreenPosition()
    {
        var worldPos = GetPosition();
        var screenPos = Camera.main.WorldToScreenPoint(worldPos);
        return screenPos;
    }
}
