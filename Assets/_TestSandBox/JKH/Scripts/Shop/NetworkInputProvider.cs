using Fusion;
using UnityEngine;

public class NetworkInputProvider : MonoBehaviour, IBeforeUpdate
{
    public NetworkRunner Runner;
    private NetworkInputData _input;

    public void BeforeUpdate()
    {
        // 버튼 상태 업데이트
        _input.Set(NetworkInputData.BUTTON_JUMP, Input.GetButton("Jump"));
        _input.Set(NetworkInputData.BUTTON_INTERACT, Input.GetKeyDown(KeyCode.E)); // E 키 상호작용
        _input.Set(NetworkInputData.BUTTON_UP, Input.GetKey(KeyCode.W));
        _input.Set(NetworkInputData.BUTTON_DOWN, Input.GetKey(KeyCode.S));
        _input.Set(NetworkInputData.BUTTON_LEFT, Input.GetKey(KeyCode.A));
        _input.Set(NetworkInputData.BUTTON_RIGHT, Input.GetKey(KeyCode.D));

        if (Runner != null && Runner.IsRunning)
        {
            //Runner.Set(this, _input);
        }
    }
}