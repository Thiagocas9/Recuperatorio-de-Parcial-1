using UnityEngine;
namespace HuntingSimulation
{
    public sealed class HuntState : HunterState
    {
        public HuntState(HunterAgent owner) : base(owner) { }
        public override string Name => "Hunt";
        public override void Enter()
        {
            hunter.Target = hunter.Nearest(false);
            hunter.SetTint(Color.red);
            hunter.RememberTarget();
        }
        public override IState EvaluateTransitions()
        {
            if (hunter.Energy <= 0) return new RestState(hunter);
            if (hunter.Target != null && !hunter.WithinBounds(hunter.Target.transform.position)) return new PatrolState(hunter);
            BoidAgent corpse = hunter.Nearest(true);
            if (corpse != null) return new GatherState(hunter, corpse);
            return null;
        }
        public override void Tick(float dt)
        {
            hunter.Energy = Mathf.Max(0, hunter.Energy - hunter.energyDrain * dt);
            if (hunter.Energy <= 0) { hunter.Stop(); return; }
            if (hunter.Target == null || hunter.Target.Dead || hunter.Target.Collected) hunter.Target = hunter.Nearest(false);
            if (hunter.Target == null) { hunter.Arrive(hunter.LastKnownPosition, hunter.waypointArrivalRadius, hunter.arrivalSlowingDistance); return; }
            hunter.RememberTarget();
            if (hunter.Cooldown <= 0 && hunter.TargetDistance <= hunter.RangeAttackRadius)
                hunter.PerformAttack(hunter.TargetDistance <= hunter.MeleeAttackRadius);
            if (hunter.Target != null && !hunter.Target.Dead) hunter.Pursue(); else hunter.Stop();
        }
    }
}
