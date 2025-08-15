using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Friend : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private ToggleGroup _toggleGroup;
    [SerializeField] private List<UI_PanelEdgeTransition> _panelTransitions;

    private void Awake()
    {
        foreach (var p in _panelTransitions)
            p.gameObject.SetActive(false);

        NetworkEventSystem.Inst.OnGameStateChangedEvent -= OnGameStateChanged;
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;
    }

    // 로비 상태가 아닌경우에는 비활성화
    private void OnGameStateChanged(Fusion.NetworkRunner runner, E_StateName prevState, E_StateName nextState)
    {
        if (nextState == E_StateName.LobbyState)
            this.gameObject.SetActive(true);
        else
            this.gameObject.SetActive(false);
    }

    public void Clear()
    {
        foreach (var p in _panelTransitions)
            p.gameObject.SetActive(false);
    }
}
