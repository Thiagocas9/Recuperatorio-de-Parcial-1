namespace HuntingSimulation
{
    public sealed class HunterFSM
    {
        readonly StateMachine machine;
        public string Current => machine.Current.Name;
        public float GatherProgress => machine.Current is GatherState gather ? gather.Progress : 0;
        public HunterFSM(HunterAgent owner) => machine = new StateMachine(new PatrolState(owner));
        public void Tick(float dt) => machine.Tick(dt);
    }
}
