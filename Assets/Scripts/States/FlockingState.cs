using UnityEngine;
namespace HuntingSimulation
{
    public sealed class FlockingState : BoidState
    {
        public FlockingState(BoidAgent owner) : base(owner) { }
        public override string Name => "Flocking";
        public override void Enter() => agent.SetTint(Color.cyan);
        public override void Tick(float dt) => agent.Flock();
    }
}
