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
                HandleCyclicalPackets();

                if (!SendPacketsQueue.IsEmpty)
                {
                    while (SendPacketsQueue.TryDequeue(out byte[] PendingPacket))
                    {
                        Client.Send(PendingPacket);
                    }
                }

                await Task.Delay(2000); // This is only an indicative value that will be changed in the future
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
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SEL_NATION).AddByte((byte)BotConfiguration.BotTemplate.Nation).Build());
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_ALLCHAR_INFO_REQ).Build());
                    }
                    // Verification of selected nation
                    else if(Enum.IsDefined(typeof(Player.Nation), (byte)Response.Nation))
                    {
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
                        Game.CurrentZoneID = (byte)Response.CurrentZoneID;
                        Game.CurrentPositionX = (ushort)Response.CurrentPositionX;
                        Game.CurrentPositionZ = (ushort)Response.CurrentPositionZ;
                        Game.CurrentPositionY = (ushort)Response.CurrentPositionY;
                        Game.VictoryNation = (byte)Response.VictoryNation;
                        Game.AuthHash = (byte[])Response.AuthHash;

                        Initializeintervals();
                        // WIZ_RENTAL packet - someone should look at it and discover the meaning
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_RENTAL).AddByte(0x02).AddByte(0x03).AddByte(0x02).Build());
                        // Send WIZ_SPEEDHACK_CHECK packet with initialization flag
                        SendSpeedhackCheckPacket(false);
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SERVER_INDEX).Build());
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_GAMESTART).AddByte(0x01).AddString("testing6").Build());
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
                        SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_LOGIN).AddString("testing6").AddString("testing6").Build());
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
                            SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SEL_CHAR).AddString("testing6").AddString(Response.CharacterList[(byte)CharacterSlot].CharacterID).AddByte(0x01)
                                .AddByte(Response.CharacterList[(byte)CharacterSlot].ZoneID).Build());
                            Debug.WriteLine("Select character");
                            // Call WinAPI form here!
                        }
                    }
                    break;

                case Packet.OpCodes.WIZ_FRIEND_PROCESS:
                    SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SKILLDATA).AddByte(0x02).Build()); // ???
                    SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_GAMESTART).AddByte(0x02).AddString("Rob").Build()); // ??? -> WE CAN PUT HERE ANYTHING... server doesnt care about Character ID
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
        }

        private void HandleCyclicalPackets()
        {
            if (Time.Intervals.Count > 0)
            {
                if (Time.Intervals["WIZ_SPEEDHACK_CHECK"].IsTimeElapsed(10.0f))
                {
                    SendSpeedhackCheckPacket();
                }
            }
        }

        private void SendSpeedhackCheckPacket(bool IsInitialized = true)
        {
            SendPacketsQueue.Enqueue(PacketParser.Packet.OpCode(Packet.OpCodes.WIZ_SPEEDHACK_CHECK).AddByte(Convert.ToByte(!IsInitialized)).AddFloat(Time.GetTotalElapsedTime()).Build());
        }

        public enum ClientState : byte
        {
            EncryptionHandshake = 1,
            Authorization,
            NationSelectScreen,
            CharacterSelectScreen,
            LoadingGame,
            InGame
        }
    }
}