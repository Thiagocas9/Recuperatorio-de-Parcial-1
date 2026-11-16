namespace HuntingSimulation
{
    public sealed class DeadState : BoidState
    {
        public DeadState(BoidAgent owner) : base(owner) { }
        public override string Name => "Dead";
        public override void Enter() => agent.EnterDead();
        public override IState EvaluateTransitions() => agent.CollectionRequested ? new RespawnState(agent) : null;
        public override void Tick(float dt) => agent.Stop();
    }
}
