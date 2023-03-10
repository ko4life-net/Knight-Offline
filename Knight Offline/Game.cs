using System;

namespace Knight_Offline
{
    public class Game
    {
        internal byte PseudoAuthID { get; set; }
        internal byte[] AuthHash { get; set; }
        internal byte VictoryNation { get; set; }
        internal DateTime ServerDateTime { get; set; }
        internal ushort ServerNo { get; set; }
        internal ushort SocketID { get; set; } // Also known as Port?
        internal Premium Premium { get; set; } = new Premium();
        internal MyCharacter MyCharacter { get; set; } = new MyCharacter();
        internal Zone Zone { get; set; } = new Zone();
    }

    public class MyCharacter
    {
        internal string CharacterID { get; set; }
        internal byte CurrentZoneID { get; set; }
        internal float CurrentPositionX { get; set; }
        internal float CurrentPositionZ { get; set; }
        internal float CurrentPositionY { get; set; }
        internal Player.Nation Nation { get; set; }
        internal Player.Race Race { get; set; }
        internal Player.Class Class { get; set; }
        internal byte Face { get; set; }
        internal byte Hair { get; set; }
        internal bool IsKing { get; set; }
        internal byte Title { get; set; }
        internal byte Level { get; set; }
        internal uint ExperiencePoints { get; set; }
        internal uint RequiredExperiencePointsToLevelUp { get; set; }
        internal uint NationalPoints { get; set; }
        internal uint MonthlyNationalPoints { get; set; }
        internal byte City { get; set; }
        internal Clan Clan { get; set; } = new Clan();
        internal ushort HP { get; set; }
        internal ushort MaxHP { get; set; }
        internal ushort MSP { get; set; }
        internal ushort MaxMSP { get; set; }
        internal ushort Attack { get; set; }
        internal ushort Defense { get; set; }
        internal StatisticsPoints StatisticsPoints { get; set; } = new StatisticsPoints();
        internal Resistances Resistances { get; set; } = new Resistances();
        internal Skills Skills { get; set; } = new Skills();
        internal Inventory Inventory { get; set; } = new Inventory();
        internal byte Authority { get; set; }
        internal byte Rank { get; set; }
        internal byte PersonalRank { get; set; }
        internal bool IsChicken { get; set; }
        internal uint MannerPoints { get; set; }     
    }

    public class Premium
    {
        internal byte AccountStatus { get; set; }
        internal byte PremiumType { get; set; } // May be enum type
        internal ushort PremiumTime { get; set; } // Premium time counted in days
    }

    public class Clan
    {
        internal ushort AllianceID { get; set; }
        internal byte AllianceFlag { get; set; }
        internal ushort ClanID { get; set; }
        internal byte ClanDuty { get; set; }
        internal string ClanName { get; set; }
        internal byte ClanGrade { get; set; }
        internal byte ClanRank { get; set; }
        internal ushort ClanMarkVersion { get; set; }
        internal ushort ClanCapeID { get; set; }
    }

    public class StatisticsPoints
    {
        internal byte FreePoints { get; set; }
        internal StatisticPoint STR { get; set; } = new StatisticPoint();
        internal StatisticPoint HP { get; set; } = new StatisticPoint();
        internal StatisticPoint DEX { get; set; } = new StatisticPoint();
        internal StatisticPoint INT { get; set; } = new StatisticPoint();
        internal StatisticPoint MP { get; set; } = new StatisticPoint();
    }

    public class StatisticPoint
    {
        internal byte Base { get; set; }
        internal byte Delta { get; set; }
    }

    public class Resistances
    {
        internal byte FireResistance { get; set; }
        internal byte GlacierResistance { get; set; }
        internal byte LightningResistance { get; set; }
        internal byte MagicResistance { get; set; }
        internal byte CourseResistance { get; set; }
        internal byte PoisonResistance { get; set; }
    }

    public class Skills
    {
        internal byte FreePoints { get; set; }
        internal byte Leadership { get; set; }
        internal byte Politics { get; set; }
        internal byte Language { get; set; }
        internal byte SiegeWeapons { get; set; }
        internal byte Tree1 { get; set; }
        internal byte Tree2 { get; set; }
        internal byte Tree3 { get; set; }
        internal byte Master { get; set; }
    }

    public class Inventory
    {
        internal uint Gold { get; set; }
        internal float Weight { get; set; }
        internal float MaxWeight { get; set; }
        internal Item[] EquippedItems { get; set; } = new Item[Defines.EquippedItemSlots];
        internal Item[] InventoryItems { get; set; } = new Item[Defines.InventoryItemSlots];
    }

    public class Item
    {
        internal uint ID { get; set; }
        internal ushort Durability { get; set; }
        internal ushort Amount { get; set; }
        internal byte RentalFlag { get; set; }
        internal ushort RemainingRentalTime { get; set; }
    }
    public class Zone
    {
        // internal Weather Weather { get; set; } = new Weather();
        internal bool CanTradeWithOtherNation { get; set; }
        internal byte ZoneType { get; set; } // May be enum type
        internal bool CanTalkToOtherNation { get; set; }
        internal ushort Tax { get; set; }
    }

    public class Weather
    {
        internal enum WeatherTypes : byte
        {
            Sunny = 1,
            Rain,
            Snow
        }

        internal WeatherTypes Type { get; set; }
        internal ushort Percentage { get; set; }
    }
}