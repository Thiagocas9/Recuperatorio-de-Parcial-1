using UnityEngine;
namespace HuntingSimulation
{
    public sealed class SeekObjectState : BoidState
    {
        public SeekObjectState(BoidAgent owner) : base(owner) { }
        public override string Name => "Seek Object";
        public override void Enter() => agent.SetTint(Color.yellow);
        public override void Tick(float dt) { if (agent.SeesInterest) agent.SeekObject(); else agent.Stop(); }
    }
}
