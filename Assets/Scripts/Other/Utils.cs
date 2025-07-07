using System.Collections;
using Fusion;
using UnityEngine;

public static class Utils 
{
    // 🌐 네트워크 관련: 로컬 플레이어인지 확인하는 유틸리티 함수
    // NetworkObject.IsValid = 네트워크 객체가 유효한지
    // NetworkObject.HasInputAuthority = 이 클라이언트가 입력 권한을 가졌는지
    // 둘 다 true면 = 내가 조종하는 플레이어
    public static bool IsLocalPlayer(NetworkObject networkObj)
    {
        return networkObj.IsValid == networkObj.HasInputAuthority;
    }
    
    public static IEnumerator PlayAnimAndSetStateWhenFinished(GameObject parent, Animator animator, string clipName,
        bool activeStateAtTheEnd = true)
    {
        animator.Play(clipName);
        var animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSecondsRealtime(animationLength);
        parent.SetActive(activeStateAtTheEnd);
    }
}
