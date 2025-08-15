using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_FriendSlotContainer : MonoBehaviour
{
    [System.Serializable]
    public class FriendUI
    {
        public E_FriendSlotForm Form;
        public RectTransform ContentHolder;
        public Image ToggleImage;
        public List<UI_FriendSlot> Slots = new();
    }

    [Header("설정")]
    [SerializeField] private ToggleGroup _toggleGroup;
    [SerializeField] private List<FriendUI> _friendUIs;

    private E_FriendSlotForm _currentForm = E_FriendSlotForm.Friend;

    private void Awake()
    {
        BindDataAction();
    }

    private void OnEnable()
    {
        UpdateFriendForm();
    }

    private void BindDataAction()
    {
        // 클릭시 업데이트 되어야하는 데이터 설정
        foreach (var f in _friendUIs)
        {
            if (f?.ToggleImage == null)
            {
                Debug.LogWarning($"FriendUI {f?.Form}의 ToggleImage가 설정되지 않았습니다.");
                continue;
            }

            switch (f.Form)
            {
                case E_FriendSlotForm.Friend:
                    {
                        var trigger = f.ToggleImage.AddComponent<EventTrigger>();
                        var entry = new EventTrigger.Entry();
                        entry.eventID = EventTriggerType.PointerClick;
                        entry.callback.AddListener((data) => UpdateFriendForm());
                        trigger.triggers.Add(entry);
                    }
                    break;
                case E_FriendSlotForm.Request:
                    {
                        var trigger = f.ToggleImage.AddComponent<EventTrigger>();
                        var entry = new EventTrigger.Entry();
                        entry.eventID = EventTriggerType.PointerClick;
                        entry.callback.AddListener((data) => UpdateRequestForm());
                        trigger.triggers.Add(entry);
                    }
                    break;
                case E_FriendSlotForm.Response:
                    {
                        var trigger = f.ToggleImage.AddComponent<EventTrigger>();
                        var entry = new EventTrigger.Entry();
                        entry.eventID = EventTriggerType.PointerClick;
                        entry.callback.AddListener((data) => UpdateResponseForm());
                        trigger.triggers.Add(entry);
                    }
                    break;
            }
        }
    }

    public void UpdateFriendForm()
    {
        Friends.Inst.GetFriendList(
            onSuccess: (friendData) => UpdateFriendSlots(E_FriendSlotForm.Friend, friendData),
            onFail: (error) => Debug.LogError($"친구 목록 조회 실패: {error}")
        );
    }

    public void UpdateRequestForm()
    {
        Friends.Inst.GetSentFriendRequests(
            onSuccess: (requestData) => UpdateFriendSlots(E_FriendSlotForm.Request, requestData),
            onFail: (error) => Debug.LogError($"보낸 친구 요청 조회 실패: {error}")
        );
    }

    public void UpdateResponseForm()
    {
        Friends.Inst.GetReceivedFriendRequests(
            onSuccess: (requestData) => UpdateFriendSlots(E_FriendSlotForm.Response, requestData),
            onFail: (error) => Debug.LogError($"받은 친구 요청 조회 실패: {error}")
        );
    }

    /// <summary>
    /// 특정 FriendUI에 새로운 슬롯을 생성하는 함수
    /// </summary>
    public UI_FriendSlot CreateFriendSlot(FriendUI ui, int index)
    {
        string slotPrefabPath = "Prefabs/UI_FriendSlot";        
        var slotPrefab = Resources.Load<GameObject>(slotPrefabPath);
        if (slotPrefab == null)
        {
            Debug.LogError($"UI_FriendSlot 프리팹을 찾을 수 없습니다.. {slotPrefabPath}");
            return null;
        }

        var slotObject = Instantiate(slotPrefab, ui.ContentHolder);
        var friendSlot = slotObject.GetComponent<UI_FriendSlot>();
        
        if (friendSlot == null)
        {
            Debug.LogError("생성된 슬롯에 UI_FriendSlot 컴포넌트가 없습니다.");
            return null;
        }

        slotObject.name = $"FriendSlot_{ui.Form}_{index}";
        return friendSlot;
    }

    /// <summary>
    /// 폼과 함께 친구 슬롯들을 업데이트하는 함수
    /// </summary>
    public void UpdateFriendSlots(E_FriendSlotForm form, Friends.FriendData[] friendData)
    {
        // 해당 폼의 UI 찾기
        var targetUI = GetFriendUIByForm(form);
        if (targetUI == null)
        {
            Debug.LogError($"폼 {form}에 해당하는 FriendUI를 찾을 수 없습니다.");
            return;
        }

        // 기존 슬롯들 비활성화
        foreach (var slot in targetUI.Slots)
        {
            if (slot != null)
            {
                slot.gameObject.SetActive(false);
            }
        }

        // 필요한 만큼 슬롯 생성 또는 재사용
        for (int i = 0; i < friendData.Length; i++)
        {
            UI_FriendSlot slot;

            // 기존 슬롯 재사용
            if (i < targetUI.Slots.Count && targetUI.Slots[i] != null)
            {
                slot = targetUI.Slots[i];
            }
            // 새 슬롯 생성
            else
            {
                slot = CreateFriendSlot(targetUI, i);
                targetUI.Slots.Add(slot);
            }

            slot.gameObject.SetActive(true);
            slot.UpdateData(form, friendData[i]);
        }

        // 사용하지 않는 슬롯들 정리
        CleanupExtraSlots(targetUI, friendData.Length);

        string formName = form switch
        {
            E_FriendSlotForm.Friend => "친구 목록",
            E_FriendSlotForm.Request => "보낸 요청",
            E_FriendSlotForm.Response => "받은 요청",
            _ => "알 수 없음"
        };

        Debug.Log($"<color=cyan>{formName} 슬롯 업데이트 완료: {friendData.Length}개 슬롯</color>");
    }

    /// <summary>
    /// 폼에 해당하는 FriendUI를 찾는 함수
    /// </summary>
    private FriendUI GetFriendUIByForm(E_FriendSlotForm form)
    {
        foreach (var ui in _friendUIs)
        {
            if (ui.Form == form)
                return ui;
        }
        return null;
    }

    /// <summary>
    /// 사용하지 않는 슬롯들을 정리하는 함수
    /// </summary>
    private void CleanupExtraSlots(FriendUI targetUI, int usedSlotCount)
    {
        for (int i = usedSlotCount; i < targetUI.Slots.Count; i++)
        {
            if (targetUI.Slots[i] != null)
            {
                targetUI.Slots[i].gameObject.SetActive(false);
            }
        }
    }
}
