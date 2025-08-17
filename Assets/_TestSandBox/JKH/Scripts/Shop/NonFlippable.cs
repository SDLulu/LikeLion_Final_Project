// 파일 이름: NonFlippable.cs
using UnityEngine;

public class NonFlippable : MonoBehaviour
{
    private Vector3 initialLocalScale;

    void Awake()
    {
        // 스크립트가 시작될 때, 자식의 원래 로컬 스케일 값을 저장해둡니다.
        initialLocalScale = transform.localScale;
    }

    // LateUpdate는 모든 Update 로직이 끝난 후 호출되어 시각적인 처리에 적합합니다.
    void LateUpdate()
    {
        // 부모 오브젝트가 있는지, 그리고 부모의 스케일이 음수(플립된 상태)인지 확인합니다.
        if (transform.parent != null && transform.parent.localScale.x < 0)
        {
            // 부모가 플립되었다면, 자식의 x 스케일도 반대로 플립하여 원래 방향을 유지합니다.
            transform.localScale = new Vector3(-initialLocalScale.x, initialLocalScale.y, initialLocalScale.z);
        }
        else
        {
            // 부모가 원래 방향이라면, 자식도 원래 스케일로 돌아옵니다.
            transform.localScale = initialLocalScale;
        }
    }
}