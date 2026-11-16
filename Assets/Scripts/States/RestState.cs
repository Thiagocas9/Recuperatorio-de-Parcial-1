using UnityEngine;
namespace HuntingSimulation
{
    public sealed class RestState : HunterState
    {
        public RestState(HunterAgent owner) : base(owner) { }
        public override string Name => "Rest";
        public override void Enter() { hunter.Target = null; hunter.Stop(); hunter.SetTint(Color.blue); }
        public override IState EvaluateTransitions() => hunter.Energy >= hunter.MaxEnergy ? new PatrolState(hunter) : null;
        public override void Tick(float dt)
        {
            hunter.Stop();
            hunter.Energy = Mathf.Min(hunter.MaxEnergy, hunter.Energy + hunter.energyRecharge * dt);
        }
    }
}
