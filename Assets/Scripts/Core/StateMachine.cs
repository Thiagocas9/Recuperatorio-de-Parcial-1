namespace HuntingSimulation
{
    public interface IState
    {
        string Name { get; }
        void Enter();
        IState EvaluateTransitions();
        void Tick(float deltaTime);
        void Exit();
    }

    // Una sola transición por actualización; el agente nunca cambia estados.
    public sealed class StateMachine
    {
        public IState Current { get; private set; }
        public StateMachine(IState initial) { Current = initial; Current.Enter(); }
        public void Tick(float dt)
        {
            IState next = Current.EvaluateTransitions();
            if (next != null && next != Current)
            {
                Current.Exit();
                Current = next;
                Current.Enter();
            }
            Current.Tick(dt);
        }
    }
}
