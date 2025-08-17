using UnityEngine;

// 애니메이션 이벤트를 받아 처리할 수 있는 모든 스크립트가 구현해야 할 인터페이스
public interface IAnimationTriggerReceiver
{
    void OnAnimationEvent(string eventName);
}
public class BossAnimationTrigger : MonoBehaviour
{
    // 인스펙터 창에서 BossFSM이 있는 게임 오브젝트를 끌어다 놓으세요.
    public BossFSM bossFsm;

    /// <summary>
    /// [애니메이션 이벤트] 발사 신호를 중계합니다.
    /// </summary>
    public void RelayEvent(string eventName)
    {
        // FSM의 현재 상태가 IAnimationEventReceiver를 구현했는지 확인합니다.
        var receiver = bossFsm.StateMachine.ActiveState as IAnimationTriggerReceiver;

        // 구현했다면, eventName과 함께 이벤트를 전달합니다.
        receiver?.OnAnimationEvent(eventName);
    }
}
