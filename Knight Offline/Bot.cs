using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Knight_Offline
{
    public class Bot
    {
        private GlobalConfiguration GlobalConfiguration;
        private readonly BotConfiguration BotConfiguration;
        private PacketParser PacketParser = new PacketParser();
        private ConcurrentQueue<byte[]> SendPacketsQueue = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<byte[]> ReceivedPacketsQueue = new ConcurrentQueue<byte[]>();
        private AsynchronousTCPClient Client;
        private Game Game = new Game();
        private Time Time = new Time();
        private ClientStates ClientState = ClientStates.Initiation;
        private BotModes BotMode = BotModes.Automatic;
        private Random RNG = new Random(Guid.NewGuid().GetHashCode());
        private ConcurrentQueue<Actions> Actions = new ConcurrentQueue<Actions>();

        public Bot(GlobalConfiguration GlobalConfiguration, BotConfiguration BotConfiguration)
        {
            this.GlobalConfiguration = GlobalConfiguration;
            this.BotConfiguration = BotConfiguration;
            Client = new AsynchronousTCPClient(ReceivedPacketsQueue);
            // BotID here
        }

        public async Task Run(CancellationTokenSource CancellationTokenSource, string ServerIP, int Port)
        {
            Client.Connect(ServerIP, Port);
            // Let's start the fun, hemorrhage gaming
            ClientState = ClientStates.EncryptionHandshake;
            Client.Send(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_VERSION_CHECK).Build());

            while (true)
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    Client.Disconnect();

                    return;
                }

                Time.UpdateTime();

                if (!ReceivedPacketsQueue.IsEmpty)
                {
                    while (ReceivedPacketsQueue.TryDequeue(out byte[] PendingPacket))
                    {
                        ProcessResponse(PacketParser.ParsePacket(PendingPacket));
                    }
                }

                // Pending calculations and actions?
                Tick();
                HandleCyclicalPackets();

                if (!SendPacketsQueue.IsEmpty)
                {
                    while (SendPacketsQueue.TryDequeue(out byte[] PendingPacket))
                    {
                        Client.Send(PendingPacket);
                    }
                }

                await Task.Delay(500); // This is only an indicative value that will be changed in the future
            }
        }

        // [DebuggerNonUserCode]
        private void ProcessResponse(dynamic Response)
        {
            switch ((Packet.OpCodes)Response.OpCode)
            {
                case Packet.OpCodes.WIZ_LOGIN:
                    // Select nation
                    if ((byte)Response.Nation == 0)
                    {
                        ClientState = ClientStates.NationSelectScreen;
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SEL_NATION).AddByte((byte)BotConfiguration.BotTemplate.Nation).Build());
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_ALLCHAR_INFO_REQ).Build());
                    }
                    // Verification of selected nation
                    else if(Enum.IsDefined(typeof(Player.Nation), (byte)Response.Nation))
                    {
                        ClientState = ClientStates.CharacterSelectScreen;

                        if ((byte)Response.Nation == (byte)BotConfiguration.BotTemplate.Nation)
                        {
                            SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_ALLCHAR_INFO_REQ).Build());
                            // Change game state and set nation in game object
                        }
                        else
                        {
                            Debug.WriteLine("Bot template has a different nation value than the selected nation on your account");
                        }
                    }
                    else
                    {
                        // Fail?
                    }
                    break;

                case Packet.OpCodes.WIZ_NEW_CHAR:
                    // Character created
                    if (Response.Code == 0x00)
                    {
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_ALLCHAR_INFO_REQ).Build());
                    }
                    // Character name taken
                    // else if (Response.Code == 0x03)
                    // {
                    //     // We could potentially try again
                    // }
                    else
                    {
                        Debug.WriteLine("Creating character failed");
                        // Call WinAPI form here!
                    }
                    break;
                    
                case Packet.OpCodes.WIZ_SEL_CHAR:
                    // Character selected 
                    if (Response.Code == 0x01)
                    {
                        ClientState = ClientStates.LoadingGame;
                        // We will fetch this informations also from the WIZ_MYINFO packet, I presume that this data is needed to load the terrain and objects while loading the game
                        Game.MyCharacter.CurrentZoneID = (byte)Response.CurrentZoneID;
                        Game.MyCharacter.CurrentPositionX = (float)Response.CurrentPositionX;
                        Game.MyCharacter.CurrentPositionZ = (float)Response.CurrentPositionZ;
                        Game.MyCharacter.CurrentPositionY = (float)Response.CurrentPositionY;
                        Game.VictoryNation = (byte)Response.VictoryNation;
                        Game.AuthHash = (byte[])Response.AuthHash;

                        Initializeintervals();
                        // WIZ_RENTAL packet - someone should look at it and discover the meaning
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_RENTAL).AddByte(0x02).AddByte(0x03).AddByte(0x02).Build());
                        // Send WIZ_SPEEDHACK_CHECK packet with initialization flag
                        SendSpeedhackCheckPacket(false);
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SERVER_INDEX).Build());
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_GAMESTART).AddByte(0x01).AddString(BotConfiguration.AccountID).Build());
                    }
                    else
                    {
                        Debug.WriteLine("Selecting character failed");
                        // Call WinAPI form here!
                    }

                    break;

                case Packet.OpCodes.WIZ_GAMESTART:
                    SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_FRIEND_PROCESS).AddByte(0x01).Build()); // enum -> FRIEND_REQUEST
                    break;
                    
                case Packet.OpCodes.WIZ_VERSION_CHECK:
                    if (GlobalConfiguration.TargetVersion == (ushort)Response.ServerVersion)
                    {
                        ClientState = ClientStates.Authorization;
                        Game.PseudoAuthID = (byte)Response.PseudoAuthID;
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_LOGIN).AddString(BotConfiguration.AccountID).AddString(BotConfiguration.Password).Build());
                    }
                    else
                    {
                        Debug.WriteLine("Invalid target version");
                        // Call WinAPI form here!
                    }
                    break;

                case Packet.OpCodes.WIZ_ALLCHAR_INFO_REQ:
                    if (Response.Code == 0x01)
                    {
                        BotRole.RoleTypes TargetRoleType = BotRole.GetRoleTypeByClass((byte)BotConfiguration.BotTemplate.Class);
                        // string TargetClass = BotConfiguration.BotTemplate.Class.GetDescription();
                        bool[] EmptySlots = new bool[] { true, true, true };
                        byte? CharacterSlot = null;

                        for (byte h = 0; h < Defines.MaxCharactersPerAccount; ++h)
                        {
                            // Check if we have a character
                            if (Response.CharacterList[h].CharacterID != string.Empty)
                            {
                                // string RoleType = ((Player.Class)Response.CharacterList[h].Class).GetDescription();
                                EmptySlots[h] = false;

                                if(TargetRoleType == BotRole.GetRoleTypeByClass(Response.CharacterList[h].Class) && CharacterSlot == null)
                                {
                                    CharacterSlot = h;
                                }
                            }
                        }

                        // Create character
                        if (CharacterSlot == null)
                        {
                            int FirstEmptySlot = Array.FindIndex(EmptySlots, (x) => { return x; }); // I know it's nasty

                            if (FirstEmptySlot != -1)
                            {
                                string CharacterName = (!string.IsNullOrEmpty(BotConfiguration.BotTemplate.CharacterName) ? BotConfiguration.BotTemplate.CharacterName : "Bot_" + BotConfiguration.InstanceID + "_" + FirstEmptySlot);
                                SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_NEW_CHAR).AddByte((byte)FirstEmptySlot).AddString(CharacterName).AddByte((byte)BotConfiguration.BotTemplate.Race)
                                    .AddShort((ushort)BotConfiguration.BotTemplate.Class).AddByte(BotConfiguration.BotTemplate.Face).AddByte(BotConfiguration.BotTemplate.Hair).AddByte(BotConfiguration.BotTemplate.BaseStats.STR)
                                    .AddByte(BotConfiguration.BotTemplate.BaseStats.HP).AddByte(BotConfiguration.BotTemplate.BaseStats.DEX).AddByte(BotConfiguration.BotTemplate.BaseStats.INT)
                                    .AddByte(BotConfiguration.BotTemplate.BaseStats.MP).Build());
                                // Possible slot IDs
                                // 0 -> Middle
                                // 1 -> Left
                                // 2 -> Right
                            }
                            // Let's try remove character
                            else
                            {
                                // TODO: In the future, add the ability to send a packet related to the removal of a character
                                Debug.WriteLine("no free slot for a new character :<");
                                // Call WinAPI form here!
                            }
                        }
                        // Select character
                        else
                        {                                
                            SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SEL_CHAR).AddString(BotConfiguration.AccountID).AddString(Response.CharacterList[(byte)CharacterSlot].CharacterID).AddByte(0x01)
                                .AddByte(Response.CharacterList[(byte)CharacterSlot].ZoneID).Build());
                            Debug.WriteLine("Select character");
                            // Call WinAPI form here!
                        }
                    }
                    break;

                case Packet.OpCodes.WIZ_MYINFO:
                    Game.SocketID = (ushort)Response.SocketID;
                    // Character section
                    Game.MyCharacter.CharacterID = (string)Response.CharacterID;
                    Game.MyCharacter.CurrentPositionX = (float)Response.CurrentPositionX;
                    Game.MyCharacter.CurrentPositionZ = (float)Response.CurrentPositionZ;
                    Game.MyCharacter.CurrentPositionY = (float)Response.CurrentPositionY;
                    Game.MyCharacter.Nation = (Player.Nation)Response.Nation;
                    Game.MyCharacter.Race = (Player.Race)Response.Race;
                    Game.MyCharacter.Class = (Player.Class)Response.Class;
                    Game.MyCharacter.Face = (byte)Response.Face;
                    Game.MyCharacter.Hair = (byte)Response.Hair;
                    Game.MyCharacter.IsKing = (bool)Response.IsKing;
                    Game.MyCharacter.Title = (byte)Response.Title;
                    Game.MyCharacter.Level = (byte)Response.Level;
                    Game.MyCharacter.ExperiencePoints = (uint)Response.ExperiencePoints;
                    Game.MyCharacter.RequiredExperiencePointsToLevelUp = (uint)Response.RequiredExperiencePointsToLevelUp;
                    Game.MyCharacter.NationalPoints = (uint)Response.NationalPoints;
                    Game.MyCharacter.MonthlyNationalPoints = (uint)Response.MonthlyNationalPoints;
                    Game.MyCharacter.City = (byte)Response.City;
                    // Clan section
                    Game.MyCharacter.Clan.AllianceID = (ushort)Response.AllianceID;
                    Game.MyCharacter.Clan.AllianceFlag = (byte)Response.AllianceFlag;
                    Game.MyCharacter.Clan.ClanID = (ushort)Response.ClanID;
                    Game.MyCharacter.Clan.ClanDuty = (byte)Response.ClanDuty;
                    Game.MyCharacter.Clan.ClanName = (string)Response.ClanName;
                    Game.MyCharacter.Clan.ClanGrade = (byte)Response.ClanGrade;
                    Game.MyCharacter.Clan.ClanRank = (byte)Response.ClanRank;
                    Game.MyCharacter.Clan.ClanMarkVersion = (ushort)Response.ClanMarkVersion;
                    Game.MyCharacter.Clan.ClanCapeID = (ushort)Response.ClanCapeID;
                    // Character statistics
                    Game.MyCharacter.HP = (ushort)Response.HP;
                    Game.MyCharacter.MaxHP = (ushort)Response.MaxHP;
                    Game.MyCharacter.MSP = (ushort)Response.MSP;
                    Game.MyCharacter.MaxMSP = (ushort)Response.MaxMSP;
                    Game.MyCharacter.Attack = (ushort)Response.Attack;
                    Game.MyCharacter.Defense = (ushort)Response.Defense;
                    //  Statistics points
                    Game.MyCharacter.StatisticsPoints.FreePoints = (byte)Response.RemainingStatisticsPoints;
                    Game.MyCharacter.StatisticsPoints.STR.Base = (byte)Response.StatisticsPoints.STR.Base;
                    Game.MyCharacter.StatisticsPoints.STR.Delta = (byte)Response.StatisticsPoints.STR.Delta;
                    Game.MyCharacter.StatisticsPoints.HP.Base = (byte)Response.StatisticsPoints.HP.Base;
                    Game.MyCharacter.StatisticsPoints.HP.Delta = (byte)Response.StatisticsPoints.HP.Delta;
                    Game.MyCharacter.StatisticsPoints.DEX.Base = (byte)Response.StatisticsPoints.DEX.Base;
                    Game.MyCharacter.StatisticsPoints.DEX.Delta = (byte)Response.StatisticsPoints.DEX.Delta;
                    Game.MyCharacter.StatisticsPoints.INT.Base = (byte)Response.StatisticsPoints.INT.Base;
                    Game.MyCharacter.StatisticsPoints.INT.Delta = (byte)Response.StatisticsPoints.INT.Delta;
                    Game.MyCharacter.StatisticsPoints.MP.Base = (byte)Response.StatisticsPoints.MP.Base;
                    Game.MyCharacter.StatisticsPoints.MP.Delta = (byte)Response.StatisticsPoints.MP.Delta;
                    // Resistances
                    Game.MyCharacter.Resistances.FireResistance = (byte)Response.Resistances.FireResistance;
                    Game.MyCharacter.Resistances.GlacierResistance = (byte)Response.Resistances.GlacierResistance;
                    Game.MyCharacter.Resistances.LightningResistance = (byte)Response.Resistances.LightningResistance;
                    Game.MyCharacter.Resistances.MagicResistance = (byte)Response.Resistances.MagicResistance;
                    Game.MyCharacter.Resistances.CourseResistance = (byte)Response.Resistances.CourseResistance;
                    Game.MyCharacter.Resistances.PoisonResistance = (byte)Response.Resistances.PoisonResistance;
                    // Skills
                    Game.MyCharacter.Skills.FreePoints = (byte)Response.SkillPoints.FreePoints;
                    Game.MyCharacter.Skills.Leadership = (byte)Response.SkillPoints.Leadership;
                    Game.MyCharacter.Skills.Politics = (byte)Response.SkillPoints.Politics;
                    Game.MyCharacter.Skills.Language = (byte)Response.SkillPoints.Language;
                    Game.MyCharacter.Skills.SiegeWeapons = (byte)Response.SkillPoints.SiegeWeapons;
                    Game.MyCharacter.Skills.Tree1 = (byte)Response.SkillPoints.Tree1;
                    Game.MyCharacter.Skills.Tree2 = (byte)Response.SkillPoints.Tree2;
                    Game.MyCharacter.Skills.Tree3 = (byte)Response.SkillPoints.Tree3;
                    Game.MyCharacter.Skills.Master = (byte)Response.SkillPoints.Master;
                    // Inventory
                    Game.MyCharacter.Inventory.Gold = (uint)Response.Gold;
                    Game.MyCharacter.Inventory.Weight = (float)Response.Weight;
                    Game.MyCharacter.Inventory.MaxWeight = (float)Response.MaxWeight;

                    // Equipped items
                    for (byte h = 0; h < Defines.EquippedItemSlots; ++h)
                    {
                        Game.MyCharacter.Inventory.EquippedItems[h] = new Item
                        {
                            ID = (uint)Response.EquippedItemList[h].ID,
                            Durability = (ushort)Response.EquippedItemList[h].Durability,
                            Amount = (ushort)Response.EquippedItemList[h].Count,
                            RentalFlag = (byte)Response.EquippedItemList[h].RentalFlag,
                            RemainingRentalTime = (ushort)Response.EquippedItemList[h].RemainingRentalTime,
                        };
                    }

                    // Inventory items
                    for (byte h = 0; h < Defines.InventoryItemSlots; ++h)
                    {
                        Game.MyCharacter.Inventory.InventoryItems[h] = new Item
                        {
                            ID = (uint)Response.InventoryItemList[h].ID,
                            Durability = (ushort)Response.InventoryItemList[h].Durability,
                            Amount = (ushort)Response.InventoryItemList[h].Count,
                            RentalFlag = (byte)Response.InventoryItemList[h].RentalFlag,
                            RemainingRentalTime = (ushort)Response.InventoryItemList[h].RemainingRentalTime,
                        };
                    }

                    // Premium issues
                    Game.Premium.AccountStatus = (byte)Response.AccountStatus;
                    Game.Premium.PremiumType = (byte)Response.PremiumType;
                    Game.Premium.PremiumTime = (ushort)Response.PremiumTime;
                    // Rest
                    Game.MyCharacter.Authority = (byte)Response.Authority;
                    Game.MyCharacter.Rank = (byte)Response.Rank;
                    Game.MyCharacter.PersonalRank = (byte)Response.PersonalRank;
                    Game.MyCharacter.IsChicken = (bool)Response.IsChicken;
                    Game.MyCharacter.MannerPoints = (uint)Response.MannerPoints;
                    break;

                case Packet.OpCodes.WIZ_TIME:
                    Game.ServerDateTime = new DateTime(Response.Year, Response.Month, Response.Day, Response.Hour, Response.Minute, 0);
                    break;

                // case Packet.OpCodes.WIZ_WEATHER:
                //     Game.Zone.Weather.Type = (Weather.WeatherTypes)Response.Weather;
                //     Game.Zone.Weather.Percentage = (ushort)Response.WeatherAmount;
                //     break;

                case Packet.OpCodes.WIZ_FRIEND_PROCESS:
                    ClientState = ClientStates.InGame;
                    SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SKILLDATA).AddByte(0x02).Build()); // ???
                    SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_GAMESTART).AddByte(0x02).AddString(BotConfiguration.AccountID).Build()); // ??? -> WE CAN PUT HERE ANYTHING... server doesnt care about Character ID
                    SendMovePacket(Game.MyCharacter.CurrentPositionX, Game.MyCharacter.CurrentPositionZ, 0, 0);
                    break;

                case Packet.OpCodes.WIZ_ZONEABILITY:     
                    Game.Zone.CanTradeWithOtherNation = (bool)Response.CanTradeWithOtherNation;
                    Game.Zone.ZoneType = (byte)Response.ZoneType;
                    Game.Zone.CanTalkToOtherNation = (bool)Response.CanTalkToOtherNation;
                    Game.Zone.Tax = (ushort)Response.Tariff;
                    break;

                case Packet.OpCodes.WIZ_SERVER_INDEX:
                    if (Response.Code == 0x01)
                    {
                        Game.ServerNo = (ushort)Response.ServerNo;
                    }
                    break;

                default:
                    // Not implemented yet, you're surprised?
                    break;
            }
        }

        private void Initializeintervals()
        {
            // Time.Intervals.Add("WIZ_TIMENOTIFY", new Interval());
            // Time.Intervals.Add("WIZ_DATASAVE", new Interval());
            Time.Intervals.Add("WIZ_SPEEDHACK_CHECK", new Interval());
            // Other
            // Time.Intervals.Add("MovementProcess", new Interval());
            Time.Intervals.Add("ActionProcess", new Interval());
        }

        private void Tick()
        {
            if (ClientState == ClientStates.InGame && BotMode == BotModes.Automatic)
            {
                lock (Actions)
                {
                    if (Actions.Count == 0)
                    {
                        if (Game.MyCharacter.IsChicken == false && Game.MyCharacter.Level < 21)
                        {
                            SimulateBreak(2, 10);
                            Move(360, 425);
                            SimulateBreak(2, 5);
                        }
                    }
                }
            }
        }

        private void HandleCyclicalPackets()
        {
            if (Time.Intervals.Count > 0)
            {
                if (Time.Intervals["WIZ_SPEEDHACK_CHECK"].IsTimeElapsed(10.0f))
                {
                    SendSpeedhackCheckPacket();
                }

                // Planned actions
                if (Actions.TryPeek(out Actions Action) && Time.Intervals["ActionProcess"].IsTimeElapsed(Action.IntervalTime))
                {
                    // Planned movement
                    if (Action is Move)
                    {
                        Move Move = Action as Move;
                        SendMovePacket(Move.X, Move.Z, 45, 3);
                    }

                    Actions.TryDequeue(out Action);
                }
            }
        }

        private void SendSpeedhackCheckPacket(bool IsInitialized = true)
        {
            SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SPEEDHACK_CHECK).AddByte(Convert.ToByte(!IsInitialized)).AddFloat(Time.GetTotalElapsedTime()).Build());
        }

        private void SendMovePacket(float WillX, float WillZ, ushort Speed, byte Flag)
        {
            SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_MOVE).AddShort((ushort)(WillX * 10)).AddShort((ushort)(WillZ * 10)).AddShort(0).AddShort(Speed).AddByte(Flag).Build());
        }

        private void SimulateBreak(int MinBreak, int MaxBreak)
        {
            Actions.Enqueue(new AFK()
            {
                IntervalTime = RNG.Next(MinBreak * 10, MaxBreak * 10) / 10.0f
            });
        }

        public void Move(float TargetX, float TargetZ)
        {
            // It's just an early prototype
            float PositionX = Game.MyCharacter.CurrentPositionX;
            float PositionZ = Game.MyCharacter.CurrentPositionZ;
            float DeltaX = Math.Abs(PositionX - TargetX);
            float DeltaZ = Math.Abs(PositionZ - TargetZ);
            float Distance = (float)Math.Sqrt(Math.Pow(DeltaX, 2) + (float)Math.Pow(DeltaZ, 2));
            ushort NumberOfMovementPackets = (ushort)(Distance / 6.75f);
            float DistanceX = DeltaX / NumberOfMovementPackets;
            float DistanceZ = DeltaZ / NumberOfMovementPackets;
            
            if (TargetX < PositionX)
            {
                DistanceX *= -1;
            }

            if (TargetZ < PositionZ)
            {
                DistanceZ *= -1;
            }
            
            for (ushort h = 0; h < NumberOfMovementPackets; ++h)
            {
                PositionX += DistanceX;
                PositionZ += DistanceZ;

                Actions.Enqueue(new Move()
                {
                    IntervalTime = 1.5f,
                    X = PositionX,
                    Z = PositionZ
                });
            }

            Actions.Enqueue(new Move()
            {
                IntervalTime = 1.5f,
                X = TargetX,
                Z = TargetZ
            });
        }

        public enum ClientStates : byte
        {
            Initiation,
            EncryptionHandshake,
            Authorization,
            NationSelectScreen,
            CharacterSelectScreen,
            LoadingGame,
            InGame
        }

        public enum BotModes : byte
        {
            Automatic,
            Manual
        }
    }
}