using UnityEngine;

public class BossDeadState : BossStateBase
{
    public override BossStateName StateName => BossStateName.Dead;

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(animState, animTransitionLength, 0, 0);
    }

    protected override void OnEnterState()
    {
        // 죽으면 물리적 움직임을 완전히 멈춥니다.
        if (boss.nrb != null)
        {
            boss.nrb.Rigidbody.linearVelocity = Vector2.zero;
            boss.nrb.Rigidbody.bodyType = RigidbodyType2D.Kinematic; // 물리적 충돌 및 힘의 영향을 받지 않도록 설정
        }

        // 콜라이더를 비활성화하여 다른 오브젝트와 충돌하지 않도록 합니다.
        // if(boss.coll != null)
        // {
        //     boss.coll.enabled = false;
        // }
    }

    protected override void OnFixedUpdate()
    {
        // 호스트가 아니거나 소멸 타이머가 끝나지 않았으면 return
        if (!boss.Object.HasStateAuthority || !boss.DespawnTimer.Expired(Runner)) return;
        
        // 타이머가 만료되면 보스 객체를 네트워크에서 소멸시킵니다.
        Runner.Despawn(boss.Object);
    }
}
