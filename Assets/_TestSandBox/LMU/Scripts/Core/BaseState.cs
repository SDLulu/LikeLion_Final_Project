using LMCore;
using System;
using UnityEngine;

namespace LMCore
{
    public interface IState
    {
        public abstract void EnterState();
        public abstract void ExitState();
        public abstract void UpdateState();
    }

    public abstract class BaseState<StateEnum, TOwner> : MonoBehaviour, IState
        where StateEnum : Enum
        where TOwner : MonoBehaviour
    {
        public StateEnum _state;
        protected Animator _anim;
        protected Rigidbody2D _rigid;
        protected TOwner _owner;

        public virtual void  Initialize(TOwner owner, StateEnum state, Animator anim, Rigidbody2D rigid)
        {
            _owner = owner;
            _state = state;
            _anim = anim;
            _rigid = rigid;
        }

        public abstract void EnterState();
        public abstract void ExitState();
        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void CancleState();

        protected virtual void OnDestroy()
        {
            _anim = null;
            _rigid = null;
            _owner = null;
        }
    }
}
