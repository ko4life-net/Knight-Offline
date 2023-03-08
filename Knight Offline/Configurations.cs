namespace Knight_Offline
{
    public class GlobalConfiguration
    {
        public ushort TargetVersion { get; set; }
        // public bool IsDebugging { get; set; }
    }

    public class BotConfiguration
    {
        public ushort InstanceID { get; set; }
        public string AccountID { get; set; }
        public string Password { get; set; }
        public BotTemplate BotTemplate;
        // Preffered role?
    }

    // May also be called Globals
    public class Defines
    {
        public const byte MaxCharactersPerAccount = 3;
        public const byte SkillsSequenceLength = 9;
        public const byte EquippedItemSlots = 14;
        public const byte InventoryItemSlots = 28;
    }
}