using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

/// <summary>
/// 스테이지에 의해 통제되는 컴포넌트
/// </summary>
public class PlayerStageController : NetworkBehaviour
{
    /// <summary>
    /// 위치를 이동시키는 함수, NewtorkRigidbody2D 내에서 네트워크 동기화 보장
    /// Note : 매프레임 호출하니 위치가 튀는 문제 발생
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
