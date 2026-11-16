using UnityEngine;
namespace HuntingSimulation
{
    public abstract class BoidState : IState
    {
        protected readonly BoidAgent agent;
        protected BoidState(BoidAgent owner) => agent = owner;
        public abstract string Name { get; }
        public virtual void Enter() { }
        public virtual void Exit() { }
        public abstract void Tick(float dt);
        public virtual IState EvaluateTransitions()
        {
            if (agent.Dead) return new DeadState(agent);
            if (agent.SeesThreat) return this is EvadeState ? null : new EvadeState(agent);
            if (agent.AtObject) return this is ConsumeState ? null : new ConsumeState(agent);
            if (agent.SeesInterest) return this is SeekObjectState ? null : new SeekObjectState(agent);
            return this is FlockingState ? null : new FlockingState(agent);
        }
    }
}
