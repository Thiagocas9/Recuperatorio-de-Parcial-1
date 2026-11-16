using UnityEngine;
namespace HuntingSimulation
{
    public sealed class EvadeState : BoidState
    {
        public EvadeState(BoidAgent owner) : base(owner) { }
        public override string Name => "Evade";
        public override void Enter() => agent.SetTint(new Color(1, 0.45f, 0.2f));
        public override void Tick(float dt) { if (agent.SeesThreat) agent.Evade(); else agent.Stop(); }
    }
}
