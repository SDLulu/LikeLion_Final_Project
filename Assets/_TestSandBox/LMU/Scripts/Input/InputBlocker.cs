using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputBlocker : MonoBehaviour
{
    [Header("디버그용")]
    [SerializeField] private List<GraphicRaycaster> _graphicRaycasters = new();

    private EventSystem _eventSystem;
    private List<RaycastResult> _cacheRayResults = new();
    private PointerEventData _cacheEvt;
    private CancellationTokenSource _cts = new();
    private void Awake()
    {
        ReSearchAsync(1, 10, _cts);
    }
    public async void ReSearchAsync(int intervalSec, int maxCount, CancellationTokenSource cts)
    {
        while (true)
        {
            if (cts.IsCancellationRequested)
                break;

            _cacheEvt = null;
            _cacheEvt = new PointerEventData(this._eventSystem);
            _eventSystem = FindAnyObjectByType<EventSystem>();
            _graphicRaycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None).ToList();
            await Awaitable.WaitForSecondsAsync(intervalSec, cts.Token);
            maxCount--;
            if (maxCount <= 0)
                break;
        }
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _graphicRaycasters.Clear();
        _cacheRayResults.Clear();
    }

    /// <summary>
    /// 마우스 포인터가 UI 위에 있는지 확인
    /// </summary>
    public bool IsPointerOverUI()
    {
        if (_eventSystem == null || _graphicRaycasters == null)
        {
            return false;
        }

        _cacheEvt.position = Mouse.current.position.ReadValue();

        for (int i = _graphicRaycasters.Count - 1; i >= 0 ; i--)
        {
            if (_graphicRaycasters[i] == null)
            {
                _graphicRaycasters.RemoveAt(i);
            }
        }

        for (int i = 0; i < _graphicRaycasters.Count; i++)
        {
            _cacheRayResults.Clear();
            _graphicRaycasters[i].Raycast(_cacheEvt, _cacheRayResults);
            if (_cacheRayResults.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 마우스 관련 입력이 차단되어야 하는지 확인
    /// </summary>
    public bool ShouldBlockMouseInput()
    {
        return IsPointerOverUI();
    }

    /// <summary>
    /// 키보드 관련 입력이 차단되어야 하는지 확인
    /// </summary>
    public bool ShouldBlockKeyboardInput()
    {
        return false;
    }

}
