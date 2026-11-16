using UnityEngine;
namespace HuntingSimulation
{
    public abstract class HunterState : IState
    {
        protected readonly HunterAgent hunter;
        protected HunterState(HunterAgent owner) => hunter = owner;
        public abstract string Name { get; }
        public virtual void Enter() { }
        public virtual void Exit() { }
        public abstract IState EvaluateTransitions();
        public abstract void Tick(float dt);
    }
}
