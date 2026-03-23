namespace SmartGates
{
    public class ModConfig
    {
        private int _gateDelay = 0;
        public int GateDelay {
            get => _gateDelay;
            set => _gateDelay = Math.Max(0, value); // ensures >=0
        }
        public bool CloseOpenGates { get; set; } = true;
    }
}
