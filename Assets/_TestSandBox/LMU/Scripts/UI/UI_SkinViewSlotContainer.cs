using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UI_SkinViewSlotContainer : MonoBehaviour
{
    [Header("디버그용")]
    private List<UI_SkinViewSlot> _skinViewSlots = new();

    private void Awake()
    {
        // 기존 SkinSlot 제거
        var originSlots =  GetComponentsInChildren<UI_SkinViewSlot>().ToList();
        for(int i = originSlots.Count-1; i >= 0; i--)
        {
            GameObject.Destroy(originSlots[i].gameObject);
        }

        int maxSlotCount = DataManager.Inst.SkinData.Count;
        int firstSkinID = DataManager.Inst.SkinData.First().Key;
        for(int i = 0; i < maxSlotCount; i++)
        {
            CreateSlot(firstSkinID + i);
        }
    }

    private void OnDestroy()
    {
        for(int i = 0; i < _skinViewSlots.Count; i++)
            RemoveSlot(i);
        _skinViewSlots.Clear();
        _skinViewSlots = null;
    }

 
    public void CreateSlot(int id)
    {   
        var skinData = DataManager.Inst.GetSkinData(id);
        var prefab = Resources.Load<GameObject>("Prefabs/UI_SkinViewSlot");
        var slot = GameObject.Instantiate(prefab, this.transform);
        slot.transform.SetParent(this.transform);
        var slotScript = slot.GetComponent<UI_SkinViewSlot>();
        _skinViewSlots.Add(slotScript);
        slotScript.UpdateData(skinData);
    }

    public void RemoveSlot(int id)  
    {
        for(int i = _skinViewSlots.Count - 1; i >= 0; i--)
        {   
            if(_skinViewSlots[i].DataID == id)
            {
                var removeSlot = _skinViewSlots[i];
                _skinViewSlots.RemoveAt(i);
                GameObject.Destroy(removeSlot.gameObject);
            }
        }
    }
}
