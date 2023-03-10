using System;
using System.Collections.Generic;
using System.Linq;

namespace Knight_Offline
{
    class BotBalancer
    {
        private Random RNG = new Random(Guid.NewGuid().GetHashCode());

        public List<BotTemplate> GenerateTemplate(int NumberOfBots)
        {
            List<BotTemplate> BotsTemplates = new List<BotTemplate>();
            Queue<PartyType.Type> PartyTypes = new Queue<PartyType.Type>(new PartyType.Type[]
            {
                PartyType.Type.Melee,
                PartyType.Type.Magical,
                PartyType.Type.Melee,
                PartyType.Type.Magical,
                PartyType.Type.Mixed
            });

            PartyType.Type CurrentPartyType = PartyTypes.Dequeue();
            Dictionary<Player.Nation, PartyScheme> PartyScheme = new Dictionary<Player.Nation, PartyScheme>();

            for (int h = 0; h < NumberOfBots; ++h)
            {
                Player.Nation Nation;
                int BotIndex;

                // El Morad
                if (h % 2 == 0)
                {
                    Nation = Player.Nation.ElMorad;
                    BotIndex = h % 16 >> 1;

                    if (h % 16 == 0)
                    {
                        PartyScheme[Nation] = GenerateParty(CurrentPartyType);
                    }
                }
                // Karus
                else
                {
                    Nation = Player.Nation.Karus;
                    BotIndex = (h - 1) % 16 >> 1;

                    if ((h - 1) % 16 == 0)
                    {
                        PartyScheme[Nation] = GenerateParty(CurrentPartyType);
                        PartyTypes.Enqueue(CurrentPartyType);
                        CurrentPartyType = PartyTypes.Dequeue();
                    }
                }

                object RaceAndClass = DrawRaceAndClass(Nation, PartyScheme[Nation].BotRoles[BotIndex].Role);
                Player.Race Race = (Player.Race)RaceAndClass.GetType().GetProperty("Race").GetValue(RaceAndClass);
                Player.Stats BaseStats = new Player.Stats().GetBaseRaceStats(Race).AddBonusPoints(PartyScheme[Nation].BotRoles[BotIndex].Role);

                BotsTemplates.Add(new BotTemplate()
                {
                    // CharacterName = "Rob", // Random character name from "dictionary database"
                    Nation = Nation,
                    Race = Race,
                    Class = (Player.Class)RaceAndClass.GetType().GetProperty("Class").GetValue(RaceAndClass),
                    Hair = (byte)RNG.Next(0, 3),
                    Face = (byte)RNG.Next(0, 4),
                    BaseStats = BaseStats
                });
            }

            return BotsTemplates;
        }

        private PartyScheme GenerateParty(PartyType.Type Scheme)
        {
            PartyScheme PartyScheme = new PartyScheme();

            // Melee party
            if (Scheme == PartyType.Type.Melee)
            {
                PartyScheme.PartyType = PartyType.Type.Melee;
                PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Melee, BotRole.RoleTypes.Priest, BotRole.SkillTreePrioritizationOptions.Buffer));
                PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Melee, BotRole.RoleTypes.Priest, BotRole.SkillTreePrioritizationOptions.Healer));
                PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Melee, BotRole.RoleTypes.Rogue));
                PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Melee, BotRole.RoleTypes.Mage));
                // Solid element
                int NumberOfWarriors = RNG.Next(0, 5);

                for (int h = 0; h < NumberOfWarriors; ++h)
                {
                    PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Melee, BotRole.RoleTypes.Warrior));
                }

                // Fill missing rest
                for (int h = 0, NumberOfRogues = 4 - NumberOfWarriors; h < NumberOfRogues; ++h)
                {
                    PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Melee, BotRole.RoleTypes.Rogue));
                }
            }
            // Mage party
            else if (Scheme == PartyType.Type.Magical)
            {
                PartyScheme.PartyType = PartyType.Type.Magical;
                PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Magical, BotRole.RoleTypes.Priest, BotRole.SkillTreePrioritizationOptions.Buffer));
                PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Magical, BotRole.RoleTypes.Rogue));
                PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Magical, BotRole.RoleTypes.Mage, BotRole.SkillTreePrioritizationOptions.IceMage)); // At least one is required

                // Draw missing mages
                for (int h = 0; h < 5; ++h)
                {
                    PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Magical, BotRole.RoleTypes.Mage));
                }
            }
            // Combined party
            else
            {
                PartyScheme.PartyType = PartyType.Type.Mixed; // For special purposes I guess
                PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Mixed, BotRole.RoleTypes.Priest, BotRole.SkillTreePrioritizationOptions.Buffer));
                PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Mixed, BotRole.RoleTypes.Rogue));
                // Additional shamans?
                int NumberOfPriests = RNG.Next(0, 3);

                if (NumberOfPriests > 1)
                {
                    PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Mixed, BotRole.RoleTypes.Priest, BotRole.SkillTreePrioritizationOptions.Healer));
                    PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Mixed, BotRole.RoleTypes.Priest, BotRole.SkillTreePrioritizationOptions.Debuffer));
                }
                else if (NumberOfPriests > 0)
                {
                    PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Mixed, BotRole.RoleTypes.Priest)); // Coin toss
                }

                // Rest of the gang
                for (int h = 0, NumberOfMissingBots = 8 - NumberOfPriests; h < NumberOfMissingBots; ++h)
                {
                    BotRole.RoleTypes RoleType = new BotRole.RoleTypes[] { BotRole.RoleTypes.Mage, BotRole.RoleTypes.Rogue, BotRole.RoleTypes.Warrior }.OrderBy(e => RNG.Next()).FirstOrDefault(); // Exclude priests?
                    PartyScheme.BotRoles.Add(GenerateBotRole(PartyType.Type.Melee, RoleType));
                }
            }
            // Assasin or Archer aka KOXP party?
            // else
            // {
            //      
            // }
            // Battle priest party with some assassin?
            // else
            // {
            //      
            // }

            return PartyScheme;
        }

        private BotRole GenerateBotRole(PartyType.Type PartyType, BotRole.RoleTypes RoleType, BotRole.SkillTreePrioritizationOptions SkillTreePrioritization = BotRole.SkillTreePrioritizationOptions.Irrelevant)
        {
            BotRole BotRole;

            if (PartyType == Knight_Offline.PartyType.Type.Melee)
            {
                if (RoleType == BotRole.RoleTypes.Priest)
                {
                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Priest, // Buff please
                        Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.ChitinPriest, BotRole.SubroleTypes.ShellPriest }.OrderBy(e => RNG.Next()).FirstOrDefault(),
                        SkillTreePrioritization = SkillTreePrioritization // In the late game (PK time) buffer can be turned into a debuffer
                    };
                }
                else if(RoleType == BotRole.RoleTypes.Rogue)
                {
                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Rogue, // Swift please
                        Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.Archer, BotRole.SubroleTypes.Assassin }.OrderBy(e => RNG.Next()).FirstOrDefault(),
                        SkillTreePrioritization = SkillTreePrioritization // Because it is defined by the subrole
                    };
                }
                else if (RoleType == BotRole.RoleTypes.Mage)
                {
                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Mage, // Teleport please
                        Subrole = BotRole.SubroleTypes.ShellMage, // Always armored in melee party
                        SkillTreePrioritization = new BotRole.SkillTreePrioritizationOptions[] { BotRole.SkillTreePrioritizationOptions.FireMage, BotRole.SkillTreePrioritizationOptions.IceMage, BotRole.SkillTreePrioritizationOptions.LightningMage }.OrderBy(e => RNG.Next()).FirstOrDefault() // The draw should be by weight and electric or ice mage should be prioritized
                    };
                }
                else
                {
                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Warrior,
                        Subrole = BotRole.SubroleTypes.Attacker,
                        SkillTreePrioritization = SkillTreePrioritization
                    };
                }
            }
            else if (PartyType == Knight_Offline.PartyType.Type.Magical)
            {
                if (RoleType == BotRole.RoleTypes.Priest)
                {
                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Priest, // Buff please
                        Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.ChitinPriest, BotRole.SubroleTypes.ShellPriest }.OrderBy(e => RNG.Next()).FirstOrDefault(),
                        SkillTreePrioritization = SkillTreePrioritization // Always buffer at the beginning but in the late game (PK time) can be turned into a healer
                    };
                }
                else if (RoleType == BotRole.RoleTypes.Rogue)
                {
                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Rogue, // Swift please
                        Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.Archer, BotRole.SubroleTypes.Assassin }.OrderBy(e => RNG.Next()).FirstOrDefault(),
                        SkillTreePrioritization = SkillTreePrioritization // Because it is defined by the subrole
                    };
                }
                // else if (RoleType == BotRole.RoleTypes.Warrior)
                // {
                //     BotRole = new BotRole()
                //     {
                //         Role = BotRole.RoleTypes.Warrior, // Lure please
                //         Subrole = BotRole.SubroleTypes.Tank, // For old provoke style e.g. Harpy, Ash knight or DTS party
                //         SkillTreePrioritization = SkillTreePrioritization // Because it is defined by the subrole
                //     };
                // }
                else
                {
                    // Draw here
                    if (SkillTreePrioritization == BotRole.SkillTreePrioritizationOptions.Irrelevant)
                    {
                        BotRole.SubroleTypes Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.PaperMage, BotRole.SubroleTypes.ShellMage }.OrderBy(e => RNG.Next()).FirstOrDefault();

                        if (Subrole == BotRole.SubroleTypes.PaperMage)
                        {
                            SkillTreePrioritization = BotRole.SkillTreePrioritizationOptions.FireMage;
                        }
                        else
                        {
                            SkillTreePrioritization = new BotRole.SkillTreePrioritizationOptions[] { BotRole.SkillTreePrioritizationOptions.FireMage, BotRole.SkillTreePrioritizationOptions.IceMage, BotRole.SkillTreePrioritizationOptions.LightningMage }.OrderBy(e => RNG.Next()).FirstOrDefault(); // The draw should be by weight
                        }

                        BotRole = new BotRole()
                        {
                            Role = BotRole.RoleTypes.Mage,
                            Subrole = Subrole,
                            SkillTreePrioritization = SkillTreePrioritization
                        };
                    }
                    else
                    {
                        BotRole = new BotRole()
                        {
                            Role = BotRole.RoleTypes.Mage,
                            Subrole = BotRole.SubroleTypes.ShellMage,
                            SkillTreePrioritization = SkillTreePrioritization
                        };
                    }
                }
            }
            // Combined party type
            else
            {
                if (RoleType == BotRole.RoleTypes.Priest)
                {
                    BotRole.SubroleTypes Subrole;

                    if (SkillTreePrioritization == BotRole.SkillTreePrioritizationOptions.Healer)
                    {
                        Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.ChitinPriest, BotRole.SubroleTypes.ShellPriest }.OrderBy(e => RNG.Next()).FirstOrDefault();
                    }
                    else
                    {
                        if (RNG.Next(0, 100) < 90)
                        {
                            Subrole = BotRole.SubroleTypes.BattlePriest;
                        }
                        else
                        {
                            Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.ChitinPriest, BotRole.SubroleTypes.ShellPriest }.OrderBy(e => RNG.Next()).FirstOrDefault();
                        }
                    }

                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Priest,
                        Subrole = Subrole,
                        SkillTreePrioritization = SkillTreePrioritization
                    };
                }
                else if (RoleType == BotRole.RoleTypes.Rogue)
                {
                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Rogue, // I could have shortened it but I chose readability
                        Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.Archer, BotRole.SubroleTypes.Assassin }.OrderBy(e => RNG.Next()).FirstOrDefault(),
                        SkillTreePrioritization = SkillTreePrioritization
                    };
                }
                else if (RoleType == BotRole.RoleTypes.Warrior)
                {
                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Warrior,
                        Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.Attacker, BotRole.SubroleTypes.Tank }.OrderBy(e => RNG.Next()).FirstOrDefault(),
                        SkillTreePrioritization = SkillTreePrioritization
                    };
                }
                else
                {
                    BotRole.SubroleTypes Subrole = new BotRole.SubroleTypes[] { BotRole.SubroleTypes.PaperMage, BotRole.SubroleTypes.ShellMage }.OrderBy(e => RNG.Next()).FirstOrDefault();

                    if (Subrole == BotRole.SubroleTypes.PaperMage)
                    {
                        SkillTreePrioritization = BotRole.SkillTreePrioritizationOptions.FireMage;
                    }
                    else
                    {
                        SkillTreePrioritization = new BotRole.SkillTreePrioritizationOptions[] { BotRole.SkillTreePrioritizationOptions.FireMage, BotRole.SkillTreePrioritizationOptions.IceMage, BotRole.SkillTreePrioritizationOptions.LightningMage }.OrderBy(e => RNG.Next()).FirstOrDefault(); // The draw should be by weight
                    }

                    BotRole = new BotRole()
                    {
                        Role = BotRole.RoleTypes.Mage,
                        Subrole = Subrole,
                        SkillTreePrioritization = SkillTreePrioritization
                    };
                }
            }

            return BotRole;
        }

        private object DrawRaceAndClass(Player.Nation Nation, BotRole.RoleTypes Role)
        {
            List<object> PossibleRaces = null;
            Player.Race Race = default(Player.Race); // Stupid incorrect solution, because otherwise CS0165...
            Player.Class Class;

            if (Nation == Player.Nation.ElMorad)
            {
                if (Role == BotRole.RoleTypes.Warrior)
                {
                    PossibleRaces = new List<object>()
                    {
                        new { Item = Player.Race.Barbarian, Probability = 45 },
                        new { Item = Player.Race.MaleElMoradian, Probability = 45 },
                        new { Item = Player.Race.FemaleElMoradian, Probability = 10 },
                    };

                    Class = Player.Class.ElMoradWarrior;
                }
                else if(Role == BotRole.RoleTypes.Priest)
                {
                    PossibleRaces = new List<object>()
                    {
                        new { Item = Player.Race.MaleElMoradian, Probability = 50 },
                        new { Item = Player.Race.FemaleElMoradian, Probability = 50 },
                    };

                    Class = Player.Class.ElMoradPriest;
                }
                else if(Role == BotRole.RoleTypes.Rogue)
                {
                    PossibleRaces = new List<object>()
                    {
                        new { Item = Player.Race.MaleElMoradian, Probability = 90 },
                        new { Item = Player.Race.FemaleElMoradian, Probability = 10 },
                    };

                    Class = Player.Class.ElMoradRogue;
                }
                else
                {
                    PossibleRaces = new List<object>()
                    {
                        new { Item = Player.Race.MaleElMoradian, Probability = 50 },
                        new { Item = Player.Race.FemaleElMoradian, Probability = 50 },
                    };

                    Class = Player.Class.ElMoradMagician;
                }
            }
            else
            {
                if (Role == BotRole.RoleTypes.Warrior)
                {
                    Race = Player.Race.ArchTuarek;
                    Class = Player.Class.KarusWarrior;
                }
                else if (Role == BotRole.RoleTypes.Priest)
                {
                    PossibleRaces = new List<object>()
                    {
                        new { Item = Player.Race.Tuarek, Probability = 50 },
                        new { Item = Player.Race.PuriTuarek, Probability = 50 },
                    };

                    Class = Player.Class.KarusPriest;
                }
                else if (Role == BotRole.RoleTypes.Rogue)
                {
                    Race = Player.Race.Tuarek;
                    Class = Player.Class.KarusRogue;
                }
                else
                {
                    Race = Player.Race.WrinkleTuarek;
                    Class = Player.Class.KarusMagician;
                }
            }

            if (PossibleRaces != null)
            {
                // Enum.TryParse(GetRandomItem(PossibleRaces).ToString(), true, out Race);
                Race = (Player.Race)GetRandomItem(PossibleRaces);
            }

            return new { Race, Class };
        }

        private T GetRandomItem<T>(IEnumerable<T> Items) where T : class
        {
            if (Items.Count() == 0)
            {
                return default(T);
            }

            // int TotalWeight = Items.Select(x => x.GetType().GetProperty("Probability").GetValue(x)).Cast<int>().Sum();
            int TotalWeight = Items.Sum(x => (int)x.GetType().GetProperty("Probability").GetValue(x));
            int RandomWeight = RNG.Next(TotalWeight) + 1;
            int CurrentWeight = 0;

            foreach (object Item in Items)
            {
                CurrentWeight += (int)Item.GetType().GetProperty("Probability").GetValue(Item);

                if (RandomWeight <= CurrentWeight)
                {
                    return (T)Item.GetType().GetProperty("Item").GetValue(Item);
                }
            }

            throw new ArgumentException("Collection count and weights must be greater than 0");
        }
    }

    public class BotTemplate
    {
        public string CharacterName;
        public Player.Nation Nation;
        public Player.Race Race;
        public Player.Class Class;
        public byte Hair;
        public byte Face;
        public Player.Stats BaseStats;
    }

    public class PartyType
    {
        public enum Type : byte
        {
            Melee,
            Magical,
            Mixed
        }
    }

    public class BotRole
    {
        public enum RoleTypes : byte
        {
            // Unknown
            Warrior,
            Priest,
            Rogue,
            Mage
        }

        public enum SubroleTypes : byte
        {
            Attacker,
            Tank, // For old provoke style e.g. Harpy party
            BattlePriest,
            // FullPlatePriest // I'm not sure if it was that popular
            ChitinPriest,
            ShellPriest,
            Archer,
            Assassin,
            PaperMage,
            // ChitinMage, // I'm not sure if it was that popular
            ShellMage
        }

        // For priest and mage
        public enum SkillTreePrioritizationOptions : byte
        {
            Irrelevant,
            Healer,
            Buffer,
            Debuffer,
            FireMage,
            IceMage,
            LightningMage
        }

        public RoleTypes Role;
        public SubroleTypes Subrole;
        public SkillTreePrioritizationOptions SkillTreePrioritization;

        public static RoleTypes GetRoleTypeByClass(byte Class)
        {
            switch(Class)
            {
                case (byte)Player.Class.KarusWarrior:
                case (byte)Player.Class.Berserker:
                case (byte)Player.Class.BerserkerHero:
                case (byte)Player.Class.ElMoradWarrior:
                case (byte)Player.Class.Blade:
                case (byte)Player.Class.BladeMaster:
                    return RoleTypes.Warrior;

                case (byte)Player.Class.KarusPriest:
                case (byte)Player.Class.Shaman:
                case (byte)Player.Class.ShadowKnight:
                case (byte)Player.Class.ElMoradPriest:
                case (byte)Player.Class.Cleric:
                case (byte)Player.Class.Paladin:
                    return RoleTypes.Priest;

                case (byte)Player.Class.KarusRogue:
                case (byte)Player.Class.Hunter:
                case (byte)Player.Class.ShadowVain:
                case (byte)Player.Class.ElMoradRogue:
                case (byte)Player.Class.Ranger:
                case (byte)Player.Class.KasarHood:
                    return RoleTypes.Rogue;

                case (byte)Player.Class.KarusMagician:
                case (byte)Player.Class.Sorcerer:
                case (byte)Player.Class.ElementalLord:
                case (byte)Player.Class.ElMoradMagician:
                case (byte)Player.Class.Mage:
                case (byte)Player.Class.ArchMage:
                    return RoleTypes.Mage;

                default:
                    return default(RoleTypes);
            }
        }
    }

    public class PartyScheme
    {
        public PartyType.Type PartyType;
        public List<BotRole> BotRoles = new List<BotRole>();
    }
}