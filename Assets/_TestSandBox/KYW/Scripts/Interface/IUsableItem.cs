using UnityEngine;
// 플레이어가 사용 가능한 아이템을 구현하는 인터페이스.
// 이 인터페이스를 상속받으면 플레이어가 클릭할 때 이 함수들이 사용되므로, 사용 시 액션만 정해주면 됩니다.

// --------------------------------------------------------------------------------
// ⚠️ 주의: 이 아이템은 들렸을때 마우스를 따라 회전하는 '손' 오브젝트의 자식입니다.
// 이로 인해 아이템의 월드 위치(World Position)와 회전값(World Rotation)은 계속 변합니다.
// 아이템을 기준으로 무언가(총알, 이펙트 등)를 생성하거나 방향을 계산할 때는,
// 반드시 월드 좌표계 속성(transform.position, transform.forward 등)을 사용해야 의도대로 동작합니다.
// --------------------------------------------------------------------------------

// [만약 로컬 좌표를 꼭 사용해야 한다면?]
// 아이템의 특정 로컬 위치(예: 모델링 기준 총구 끝)를 기준으로 월드 좌표를 얻어야 할 경우,
// transform.TransformPoint() 함수를 사용해 로컬 좌표를 월드 좌표로 변환해야 합니다.
//
// 예시:
// // 아이템 모델의 원점 기준 (0, 0.1, 0.5) 위치를 총구 끝이라고 가정
// Vector3 muzzleLocalPos = new Vector3(0f, 0.1f, 0.5f);
//
// // 로컬 좌표를 월드 좌표로 변환
// Vector3 muzzleWorldPos = transform.TransformPoint(muzzleLocalPos);
//
// // 변환된 월드 좌표에 이펙트 생성
// Instantiate(muzzleFlashEffect, muzzleWorldPos, transform.rotation);
// --------------------------------------------------------------------------------


// 아이템 입력 인터페이스 (간단 버전)
public interface IUsableItem
{
    // 클릭 시작
    void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition);
    // 클릭 유지
    void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition);
    // 클릭 종료
    void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition);
}

