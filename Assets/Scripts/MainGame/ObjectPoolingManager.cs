using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Unity.Mathematics;
using UnityEngine;

// 🔄 오브젝트 풀링 시스템 - 성능 최적화의 핵심!
// 📦 MonoBehaviour: Unity 컴포넌트로 동작
// 🏭 INetworkObjectProvider: Fusion의 네트워크 오브젝트 생성/해제를 관리하는 인터페이스
public class ObjectPoolingManager : MonoBehaviour, INetworkObjectProvider
{
    // 📚 핵심 데이터 구조: 프리팹별 오브젝트 풀을 관리하는 딕셔너리
    // 🗝️ Key: 프리팹 소스 (예: Bullet 프리팹)
    // 📋 Value: 해당 프리팹으로 생성된 네트워크 오브젝트들의 리스트
    // 예시: Bullet 프리팹 → [bullet1, bullet2, bullet3...] (재사용 대기 중인 총알들)
    private Dictionary<INetworkPrefabSource, List<NetworkObject>> prefabsThatHadBeenInstantiated = new();

    // 🎬 게임 시작 시 한 번 호출
    private void Start()
    {
        // 🌐 글로벌 매니저에 자신을 등록 (다른 스크립트에서 접근 가능하도록)
        if (GlobalManagers.Instance != null)
        {
            GlobalManagers.Instance.ObjectPoolingManager = this;
        }
    }

    // 🏭 가장 중요한 메서드! Runner.Spawn()이 호출될 때마다 자동으로 실행됨
    // 🎯 새 오브젝트를 생성하거나 기존 오브젝트를 재사용하여 성능 최적화
    public NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject result)
    {
        NetworkObject networkObject = null;
        
        // 🔍 요청된 프리팹의 정보 가져오기
        NetworkPrefabId prefabID = context.PrefabId;
        INetworkPrefabSource prefabSource = NetworkProjectConfig.Global.PrefabTable.GetSource(prefabID);
        
        // 📋 해당 프리팹으로 이미 생성된 오브젝트들이 있는지 확인
        prefabsThatHadBeenInstantiated.TryGetValue(prefabSource, out var networkObjects);

        bool foundMatch = false;
        
        // 🔄 재사용 가능한 오브젝트 찾기 (성능 최적화의 핵심!)
        if (networkObjects?.Count > 0)
        {
            foreach (var item in networkObjects)
            {
                // ✅ 조건 체크: 존재하고 + 비활성화 상태인 오브젝트 찾기
                if (item != null && item.gameObject.activeSelf == false)
                {
                    // 🎉 재사용 가능한 오브젝트 발견!
                    // 새로 생성하지 않고 기존 오브젝트를 재활용
                    networkObject = item;
    
                    foundMatch = true;
                    break; // 하나 찾으면 바로 종료
                }
            }
        }

        // 🚫 재사용 가능한 오브젝트가 없는 경우
        // 1. 완전히 새로운 프리팩 타입이거나
        // 2. 함수가 너무 빨리 호출되어서 재사용할 오브젝트가 아직 준비되지 않았거나
        if (foundMatch == false)
        {
            // 🆕 새로운 오브젝트 인스턴스 생성
            networkObject = CreateObjectInstance(prefabSource);
            
            // 🔧 생성 실패 시 처리
            if (networkObject == null)
            {
                Debug.LogError($"Failed to create network object instance for prefab {prefabSource?.Description ?? "Unknown"}");
                result = null;
                return NetworkObjectAcquireResult.Failed;
            }
        }

        // 📤 결과 반환
        result = networkObject;
        return NetworkObjectAcquireResult.Success;
    }

    // 🏗️ 새로운 오브젝트 인스턴스를 생성하는 함수
    private NetworkObject CreateObjectInstance(INetworkPrefabSource prefab)
    {
        // 🔧 프리팹 리소스 확보 (동기적으로 즉시 로드)
        prefab.Acquire(synchronous: true);
        
        // ✅ 프리팹이 즉시 사용 가능한지 확인
        if (!prefab.IsCompleted)
        {
            Debug.LogError($"Prefab {prefab.Description} not immediately available");
            // 필요에 따라 다른 처리 방식을 사용할 수 있음
        }

        // 🎭 실제 게임오브젝트 생성 (기본 위치와 회전으로)
        var obj = Instantiate(prefab.WaitForResult(), Vector3.zero, Quaternion.identity);

        // 📚 딕셔너리에 새로 생성된 오브젝트 추가
        if (prefabsThatHadBeenInstantiated.TryGetValue(prefab, out var instanceData))
        {
            // 🔗 이미 해당 프리팹 타입이 딕셔너리에 있으면 리스트에 추가
            instanceData.Add(obj);
        }
        else
        {
            // 🆕 처음 생성되는 프리팹 타입이면 새로운 리스트를 만들어서 딕셔너리에 추가
            var list = new List<NetworkObject> { obj };
            prefabsThatHadBeenInstantiated.Add(prefab, list);
        }

        return obj; 
    }
    
    // 🗑️ Runner.Despawn()이 호출될 때 자동으로 실행됨
    // 🔄 오브젝트를 완전히 파괴하지 않고 비활성화하여 재사용을 위해 보관
    public void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
    {
        // 💡 핵심: Destroy() 대신 SetActive(false)로 비활성화!
        // 이렇게 하면 나중에 다시 활성화해서 재사용 가능
        context.Object.gameObject.SetActive(false);
    }

    // 🔍 프리팹 GUID를 통해 프리팹 ID를 가져오는 함수
    public NetworkPrefabId GetPrefabId(NetworkRunner runner, NetworkObjectGuid prefabGuid)
    {
        return runner.Prefabs.GetId(prefabGuid);
    }

    // 🧹 딕셔너리에서 특정 네트워크 오브젝트를 제거하는 함수
    // 오브젝트가 완전히 파괴될 때 메모리 누수 방지를 위해 사용
    public void RemoveNetworkObjectFromDic(NetworkObject obj)
    {
        // 📋 모든 프리팹 타입을 순회하며 해당 오브젝트 찾기
        if (prefabsThatHadBeenInstantiated.Count > 0)
        {
            foreach (var item in prefabsThatHadBeenInstantiated)
            {
                // 🔍 해당 프리팹 타입의 오브젝트 리스트에서 일치하는 오브젝트 찾기
                foreach (var networkObject in item.Value.Where(networkObject => networkObject == obj))
                {
                    // 🗑️ 찾으면 리스트에서 제거하고 반복문 종료
                    item.Value.Remove(networkObject);
                    break;
                }
            }
        }
    }
}















