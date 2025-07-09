using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore
{
    public abstract class BaseFSM<StateEnum, TOwner> : MonoBehaviour
        where StateEnum : System.Enum
        where TOwner : MonoBehaviour
    {
        [field: SerializeField] public StateEnum CurState { get; protected set; }
        [field: SerializeField] public StateEnum PreState { get; protected set; }
        [field: SerializeField] public StateEnum PrePreState { get; protected set; }

        public List<BaseState<StateEnum, TOwner>> StateList { get; protected set; } = new();
        public Dictionary<StateEnum, BaseState<StateEnum, TOwner>> StateDict { get; protected set; } = new();
        public Dictionary<IState, StateEnum> IStateDict { get; protected set; } = new();

        private void OnDestroy()
        {
            StateList.Clear();
            StateDict.Clear();
            IStateDict.Clear();

            StateList = null;
            StateDict = null;
            IStateDict = null;
        }

        public abstract void OnUpdate();

        protected bool _firstState = true;
        protected bool _changeState = false;
        protected bool _inited = false;

        protected void RegisterState(BaseState<StateEnum, TOwner> state)
        {
            StateList.Add(state);
            StateDict.Add(state._state, state);
            IStateDict.Add(state, state._state);
        }

        public virtual void SetState(string state)
        {
            if (Enum.TryParse(typeof(StateEnum), state, out var result))
            {
                SetState((StateEnum)result);
            }
            else
            {
                Debug.LogError($"SetState 실패! {state}");
            }
        }

        public Action<StateEnum> OnChangeState;
        public virtual void SetState(StateEnum state)
        {
            if (CurState.ToString() == state.ToString())
                return;

            Debug.Log($"<color=#00FF00FF>------------ 상태 : {state}</color>");

            // 이전 상태 종료
            if (_firstState == false)
            {
                StateDict[CurState].ExitState();
            }
            _firstState = false;

            // 상태 변경
            PreState = CurState;
            CurState = state;
            StateDict[CurState].EnterState();

            // 상태 변경 이벤트 호출
            OnChangeState?.Invoke(CurState);
        }
    }
}