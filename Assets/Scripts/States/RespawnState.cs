namespace HuntingSimulation
{
    public sealed class RespawnState : BoidState
    {
        float elapsed;
        public RespawnState(BoidAgent owner) : base(owner) { }
        public override string Name => "Respawn";
        public override void Enter() => agent.HideForRespawn();
        public override IState EvaluateTransitions()
        {
            if (elapsed >= agent.respawnDelay && agent.TryRespawn()) return new FlockingState(agent);
            return null;
        }
        public override void Tick(float dt) { elapsed += dt; agent.Stop(); }
    }
}
