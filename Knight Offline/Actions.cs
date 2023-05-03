namespace Knight_Offline
{
    public interface Actions
    {
        float IntervalTime { get; set; }
    }

    internal class AFK : Actions
    {
        public float IntervalTime { get; set; }
    }

    internal class Move : Actions
    {
        public float IntervalTime { get; set; }
        public float X { get; set; }
        public float Z { get; set; }
    }

    internal class test : Actions
    {
        public float IntervalTime { get; set; }
    }
}
