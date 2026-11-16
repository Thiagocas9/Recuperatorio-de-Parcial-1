using UnityEngine;
namespace HuntingSimulation
{
    public sealed class PatrolState : HunterState
    {
        public PatrolState(HunterAgent owner) : base(owner) { }
        public override string Name => "Patrol";
        public override void Enter() { hunter.Target = null; hunter.SetTint(new Color(0.6f, 0.45f, 1)); }
        public override IState EvaluateTransitions()
        {
            BoidAgent corpse = hunter.Nearest(true);
            if (corpse != null) return new GatherState(hunter, corpse);
            if (hunter.Cooldown <= 0 && hunter.Nearest(false) != null) return new HuntState(hunter);
            return null;
        }
        public override void Tick(float dt) { hunter.Patrol(); hunter.SpawnInterest(); }
    }
}
