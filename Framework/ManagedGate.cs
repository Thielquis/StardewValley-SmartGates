namespace SmartGates.Framework
{
    public class ManagedGate
    {
        public long PlayerId { get; set; }
        public bool Delayed { get; set; }
        public bool CanCloseGate(long playerID)
        {
            return this.PlayerId == playerID && !this.Delayed;
        }
    }
}
