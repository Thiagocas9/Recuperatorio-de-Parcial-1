using UnityEngine;
namespace HuntingSimulation
{
    public sealed class ConsumeState : BoidState
    {
        float elapsed;
        public ConsumeState(BoidAgent owner) : base(owner) { }
        public override string Name => "Consume";
        public override void Enter() { agent.SetTint(Color.green); agent.Stop(); }
        public override void Tick(float dt)
        {
            agent.Stop(); elapsed += dt;
            if (elapsed < agent.interactionInterval || !agent.AtObject) return;
            elapsed = 0;
            agent.Interest.Damage(agent.consumptionDamage);
        }
    }
}
