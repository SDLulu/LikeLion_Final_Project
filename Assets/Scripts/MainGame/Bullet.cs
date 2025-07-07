using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// 🔫 총알의 움직임과 충돌 처리를 담당하는 클래스
// 📡 NetworkBehaviour: 네트워크 동기화가 필요한 컴포넌트
//
// 🌐 왜 NetworkBehaviour인가?
// - 총알은 모든 플레이어에게 보여야 함 (다른 플레이어가 쏜 총알도 봐야 함)
// - 충돌, 데미지 등은 서버에서 권위적으로 처리 (치팅 방지)
// - 서버에서 Spawn된 총알이 모든 클라이언트에 자동 동기화
//
// 🎯 NetworkBehaviour vs MonoBehaviour 선택 기준:
//
// ✅ NetworkBehaviour 사용 (실시간 동기화 필요):
//    🏃 이동하는 적 - 위치 실시간 동기화
//    🔫 총알 - 물리 시뮬레이션 + 충돌 감지
//    📦 동적 생성 아이템 - Runner.Spawn/Despawn 필요
//    🚀 이동하는 플랫폼 - 위치 변화 동기화
//
// ✅ MonoBehaviour + 중앙 관리 사용 (효율적!):
//    🗿 정적 함정 - 맵에 고정, 상태만 on/off
//    🎁 고정 아이템 - 맵에 배치, 수집 상태만 관리
//    🚪 스위치/버튼 - 위치 변화 없음, 상태만 중요
//    🏺 파괴 가능한 벽 - 파괴 여부만 동기화
//
// 📊 성능 비교:
//    ❌ 100개 NetworkObject = 3200bytes/tick (무거움)
//    ✅ 1개 중앙관리자 + 100개 MonoBehaviour = 12.5bytes/tick (가벼움)
//
// 💡 핵심 질문: "정말 실시간 동기화가 필요한가?"
//    🔄 실시간 필요 → NetworkBehaviour
//    📊 상태만 중요 → MonoBehaviour + 중앙 관리
public class Bullet : NetworkBehaviour
{
    // 🎯 충돌 감지용 레이어 마스크들
    [SerializeField] private LayerMask playerLayerMask; // 플레이어 레이어
    [SerializeField] private LayerMask groundLayerMask; // 지형 레이어
    
    // ⚔️ 총알 설정값들
    [SerializeField] private int bulletDmg = 10;        // 데미지량
    [SerializeField] private float moveSpeed = 20;      // 이동 속도
    [SerializeField] private float lifeTimeAmount = 0.8f; // 생존 시간 (초)
    
    // 🌐 네트워크 동기화 변수들
    // 모든 클라이언트가 같은 값을 공유해야 하는 중요한 상태들
    [Networked] private NetworkBool didHitSomething { get; set; } // 뭔가에 충돌했는지
    [Networked] private TickTimer lifeTimeTimer { get; set; }     // 수명 타이머
    
    // 🔧 컴포넌트 참조
    private Collider2D coll;

    // 🎬 네트워크 오브젝트가 생성될 때 한 번 호출
    public override void Spawned()
    {
        // 🎯 물리 시뮬레이션 활성화 (충돌 감지를 위해 필요)
        Runner.SetIsSimulated(Object, true);
        
        // 🔧 충돌 감지를 위한 컴포넌트 가져오기
        coll = GetComponent<Collider2D>();
        
        // ⏰ 총알 수명 타이머 시작
        // TickTimer: Fusion의 네트워크 동기화 타이머
        // 모든 클라이언트에서 같은 시간에 타이머가 만료됨
        lifeTimeTimer = TickTimer.CreateFromSeconds(Runner, lifeTimeAmount);
    }

    // 📡 네트워크 물리 업데이트 (고정된 프레임레이트로 실행)
    public override void FixedUpdateNetwork()
    {
        // 🎯 아직 충돌하지 않았다면 충돌 검사 실행
        if (!didHitSomething)
        {
            CheckIfHitGround();   // 지형 충돌 체크
            CheckIfWeHitAPlayer(); // 플레이어 충돌 체크
        }

        // 🏃 수명이 남아있고 충돌하지 않았다면 계속 이동
        if (lifeTimeTimer.ExpiredOrNotRunning(Runner) == false && !didHitSomething)
        {
            // 💫 총알이 바라보는 방향(transform.right)으로 이동
            // Runner.DeltaTime: 네트워크 틱 간격 (일반 Time.deltaTime과 다름)
            transform.Translate(transform.right * moveSpeed * Runner.DeltaTime, Space.World);
        }

        // 💀 수명이 다했거나 뭔가에 충돌했으면 총알 제거
        if (lifeTimeTimer.Expired(Runner) || didHitSomething)
        {
            lifeTimeTimer = TickTimer.None; // 타이머 리셋
            Runner.Despawn(Object);         // 네트워크에서 오브젝트 제거
        }
    }

    // 🗻 지형과 충돌했는지 확인하는 함수
    private void CheckIfHitGround()
    {
        // 🔍 Fusion 물리 시스템을 사용한 충돌 감지
        // Runner.GetPhysicsScene2D(): Fusion의 네트워크 동기화된 물리 시스템
        var groundCollider = Runner.GetPhysicsScene2D()
            .OverlapBox(transform.position, coll.bounds.size, 0, groundLayerMask);

        // 💥 지형과 충돌했으면 충돌 플래그 설정
        if (groundCollider != default)
        {
            didHitSomething = true;
        }
    }

    // 🎯 플레이어 충돌 감지를 위한 결과 리스트 (재사용으로 GC 최적화)
    private List<LagCompensatedHit> hits = new List<LagCompensatedHit>();
    
    // 👥 플레이어와 충돌했는지 확인하는 함수
    private void CheckIfWeHitAPlayer()
    {
        // 🎯 지연 보상 충돌 감지 (Lag Compensation)
        // 네트워크 지연을 고려해서 정확한 충돌 판정을 수행
        // Object.InputAuthority: 이 총알을 쏜 플레이어 권한
        Runner.LagCompensation.OverlapBox(transform.position, coll.bounds.size, Quaternion.identity,
            Object.InputAuthority, hits, playerLayerMask);

        // 🔍 충돌한 대상이 있는지 확인
        if (hits.Count > 0)
        {
            // 📝 모든 충돌 대상을 순회하며 처리
            foreach (var item in hits)
            {
                // ✅ 유효한 히트박스인지 확인
                if (item.Hitbox != null)
                {
                    // 🎮 충돌한 오브젝트에서 PlayerController 컴포넌트 찾기
                    var player = item.Hitbox.GetComponentInParent<PlayerController>();
                    
                    // 🛡️ 자기 자신을 쏘지 않았는지 확인 (팀킬 방지)
                    var didNotHitOurOwnPlayer = player.Object.InputAuthority.PlayerId != Object.InputAuthority.PlayerId;

                    // ✅ 다른 플레이어이고 살아있는 상태인지 확인
                    if (didNotHitOurOwnPlayer && player.PlayerIsAlive)
                    {
                        // 🖥️ 서버에서만 데미지 처리 (치팅 방지)
                        if (Runner.IsServer)
                        {
                            // 💥 플레이어에게 데미지를 입히는 RPC 호출
                            player.GetComponent<PlayerHealthController>().Rpc_ReducePlayerHealth(bulletDmg);
                        }
                        
                        // 🎯 충돌 플래그 설정하고 루프 탈출
                        didHitSomething = true;
                        break;
                    }
                }
            }
        }
    }
}
