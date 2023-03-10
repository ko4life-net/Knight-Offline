using System.ComponentModel;

namespace Knight_Offline
{
    public class Player
    {
        public enum Nation : byte
        {
            // 0 = Unselected nation
            Karus = 1,
            ElMorad = 2
        }

        public enum Race : byte
        {
            ArchTuarek = 1,
            Tuarek = 2,
            WrinkleTuarek = 3,
            PuriTuarek = 4,
            Barbarian = 11,
            MaleElMoradian = 12,
            FemaleElMoradian = 13
        }

        public enum Class : byte
        {
            [Description("Warrior")]
            KarusWarrior = 101,
            [Description("Warrior")]
            Berserker = 105,
            [Description("Warrior")]
            BerserkerHero = 106,
            [Description("Rogue")]
            KarusRogue = 102,
            [Description("Rogue")]
            Hunter = 107,
            [Description("Rogue")]
            ShadowVain = 108,
            [Description("Priest")]
            KarusPriest = 104,
            [Description("Priest")]
            Shaman = 109,
            [Description("Priest")]
            ShadowKnight = 110,
            [Description("Magician")]
            KarusMagician = 103,
            [Description("Magician")]
            Sorcerer = 111,
            [Description("Magician")]
            ElementalLord = 112,
            [Description("Warrior")]
            ElMoradWarrior = 201,
            [Description("Warrior")]
            Blade = 205,
            [Description("Warrior")]
            BladeMaster = 206,
            [Description("Rogue")]
            ElMoradRogue = 202,
            [Description("Rogue")]
            Ranger = 207,
            [Description("Rogue")]
            KasarHood = 208,
            [Description("Magician")]
            ElMoradMagician = 203,
            [Description("Magician")]
            Mage = 209,
            [Description("Magician")]
            ArchMage = 210,
            [Description("Priest")]
            ElMoradPriest = 204,
            [Description("Priest")]
            Cleric = 211,
            [Description("Priest")]
            Paladin = 212
        }

        public class Stats
        {
            [Description("Strength")]
            public byte STR;
            [Description("Dexterity")]
            public byte DEX;
            [Description("Health points")]
            public byte HP;
            [Description("Intelligence")]
            public byte INT;
            [Description("Magic Power")]
            public byte MP;

            public Stats GetBaseRaceStats(Race Race)
            {
                switch (Race)
                {
                    case Race.Barbarian:
                    case Race.ArchTuarek:
                        STR = HP = 65;
                        DEX = 60;
                        INT = MP = 50;
                        break;

                    case Race.MaleElMoradian:
                    case Race.Tuarek:
                        STR = HP = 60;
                        DEX = 70;
                        INT = MP = 50;
                        break;

                    case Race.FemaleElMoradian:
                    case Race.WrinkleTuarek:
                        STR = HP = MP = 50;
                        DEX = INT = 70;
                        break;

                    case Race.PuriTuarek:
                        STR = MP = 50;
                        DEX = HP = 60;
                        INT = 70;
                        break;
                }

                return this;
            }

            public Stats AddBonusPoints(BotRole.RoleTypes Role)
            {
                switch (Role)
                {
                    case BotRole.RoleTypes.Warrior:
                    case BotRole.RoleTypes.Priest:
                        STR += 10;
                        break;

                    case BotRole.RoleTypes.Rogue:
                        DEX += 10;
                        break;

                    case BotRole.RoleTypes.Mage:
                        MP += 10;
                        break;
                }

                return this;
            }
        }
    }
}