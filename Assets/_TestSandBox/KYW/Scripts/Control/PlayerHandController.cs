using Fusion;
using UnityEngine;

// 🤲 플레이어 손 무기 컨트롤러
// 1,2,3 키로 손에 든 무기 교체 담당
public class PlayerHandController : NetworkBehaviour
{
    [Header("Hand Weapons")]
    [SerializeField] private GameObject whipObject;      // 1번 - 채찍
    [SerializeField] private GameObject shotgunObject;   // 2번 - 샷건  
    [SerializeField] private GameObject pickaxeObject;   // 3번 - 곡괭이
    
    [Header("Settings")]
    [SerializeField] private bool showDebugInfo = true; // 디버그 정보 표시
    
    // 🎮 현재 활성화된 무기 추적
    [Networked] public int CurrentWeaponIndex { get; private set; } = 0; // 0=없음, 1=채찍, 2=샷건, 3=곡괭이
    
    // Fusion 2 공식 패턴: 이전 버튼 상태 추적 (GetPressed 사용)
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    
    // 무기 오브젝트들 배열
    private GameObject[] weaponObjects;
    
    public override void Spawned()
    {
        // 무기 배열 초기화
        weaponObjects = new GameObject[] 
        { 
            null,           // 0번 인덱스 (없음)
            whipObject,     // 1번 인덱스 (채찍)
            shotgunObject,  // 2번 인덱스 (샷건)
            pickaxeObject   // 3번 인덱스 (곡괭이)
        };
        
        // 디버그: 권한 및 무기 오브젝트 확인
        Debug.Log($"🤲 PlayerHandController 생성 - HasInputAuthority: {Object.HasInputAuthority}");
        Debug.Log($"🔫 무기 오브젝트 확인 - 채찍: {whipObject != null}, 샷건: {shotgunObject != null}, 곡괭이: {pickaxeObject != null}");
        
        // 모든 무기 비활성화로 시작
        DeactivateAllWeapons();
    }
    
    // 무기 교체 관련 모든 처리를 통합한 메서드 (Fusion 2 공식 패턴)
    public void ProcessInput(SpelunkyPlayerData input)
    {
        // Fusion 2 공식 패턴: GetPressed로 버튼 눌림 감지
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        
        // 이전 상태 업데이트 (공식 패턴)
        ButtonsPrevious = input.NetworkButtons;
        
        // TODO: 디버그용 무기 교체 기능 제거됨
        // 새로운 아이템 시스템에서는 우클릭으로 던지기, 좌클릭으로 사용
    }
    
    // 🔄 무기 교체 RPC
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void SwitchWeaponRpc(int weaponIndex)
    {
        // 같은 무기면 해제, 다른 무기면 교체
        if (CurrentWeaponIndex == weaponIndex)
        {
            // 현재 무기 해제
            CurrentWeaponIndex = 0;
            DeactivateAllWeapons();
            Debug.Log("🤲 무기 해제");
        }
        else
        {
            // 새 무기로 교체
            CurrentWeaponIndex = weaponIndex;
            ActivateWeapon(weaponIndex);
            Debug.Log($"🤲 무기 교체: {GetWeaponName(weaponIndex)}");
        }
    }
    
    // ⚔️ 특정 무기 활성화
    private void ActivateWeapon(int weaponIndex)
    {
        // 모든 무기 비활성화
        DeactivateAllWeapons();
        
        // 해당 무기만 활성화
        if (weaponIndex > 0 && weaponIndex < weaponObjects.Length && weaponObjects[weaponIndex] != null)
        {
            weaponObjects[weaponIndex].SetActive(true);
        }
    }
    
    // 🚫 모든 무기 비활성화
    private void DeactivateAllWeapons()
    {
        for (int i = 1; i < weaponObjects.Length; i++) // 0번 제외 (null)
        {
            if (weaponObjects[i] != null)
            {
                weaponObjects[i].SetActive(false);
            }
        }
    }
    
    // 📝 무기 이름 가져오기
    private string GetWeaponName(int weaponIndex)
    {
        return weaponIndex switch
        {
            1 => "채찍",
            2 => "샷건", 
            3 => "곡괭이",
            _ => "없음"
        };
    }
    
    // 📊 상태 확인 프로퍼티들
    public bool HasWeaponEquipped => CurrentWeaponIndex > 0;
    public string CurrentWeaponName => GetWeaponName(CurrentWeaponIndex);
    public GameObject CurrentWeaponObject => 
        (CurrentWeaponIndex > 0 && CurrentWeaponIndex < weaponObjects.Length) ? 
        weaponObjects[CurrentWeaponIndex] : null;
    
    // 🔍 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || !Object.HasInputAuthority) return;
        
        GUILayout.BeginArea(new Rect(10, 100, 300, 120));
        GUILayout.Box("🤲 손 무기 정보");
        GUILayout.Label($"현재 무기: {CurrentWeaponName}");
        GUILayout.Label($"무기 인덱스: {CurrentWeaponIndex}");
        GUILayout.Label("");
        GUILayout.Label("조작법:");
        GUILayout.Label("1키 - 채찍 | 2키 - 샷건 | 3키 - 곡괭이");
        GUILayout.EndArea();
    }
} 