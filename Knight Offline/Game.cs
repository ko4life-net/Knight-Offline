namespace Knight_Offline
{
    class Game
    {
        internal byte CurrentZoneID { get; set; }
        internal ushort CurrentPositionX { get; set; }
        internal ushort CurrentPositionZ { get; set; }
        internal ushort CurrentPositionY { get; set; }
        internal byte VictoryNation { get; set; }
        internal byte[] AuthHash { get; set; }
    }

    class MyCharacter
    {
        public enum Nation : byte
        {
            // 0 = Unselected nation
            Karus = 1,
            ElMorad = 2,
        }
    }
}