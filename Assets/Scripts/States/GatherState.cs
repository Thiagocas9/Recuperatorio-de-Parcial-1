using UnityEngine;
namespace HuntingSimulation
{
    public sealed class GatherState : HunterState
    {
        readonly BoidAgent corpse;
        public float Progress { get; private set; }
        public GatherState(HunterAgent owner, BoidAgent target) : base(owner) => corpse = target;
        public override string Name => "Gather";
        public override void Enter() { hunter.Target = corpse; hunter.SetTint(Color.green); }
        public override IState EvaluateTransitions()
        {
            if (corpse == null || !corpse.Dead || corpse.Collected || corpse.CollectionRequested) return new PatrolState(hunter);
            if (Progress >= hunter.gatherDuration) { hunter.FinishGather(); return new PatrolState(hunter); }
            return null;
        }
        public override void Tick(float dt)
        {
            if (hunter.TargetDistance > hunter.gatherArrivalRadius) { Progress = 0; hunter.Seek(corpse.transform.position); }
            else { hunter.Stop(); Progress += dt; }
        }
    }
}
