using LMCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    [SerializeField] private Image _test;

    private void Awake()
    {
        var evt = _test.gameObject.GetOrAddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry(){eventID = EventTriggerType.PointerDown};
        entry.callback.AddListener((data) => OnClick(data));
        evt.triggers.Add(entry);
    }

    private void OnClick(BaseEventData eventData)
    {
        Debug.Log("OnClick");
    }
}
