using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Dynamic;

namespace Knight_Offline
{
    public class PacketParser
    {
        static public Packet Packet;
        static protected Crc32 Crc32;
        static protected JvCryption JvCryption;
        static protected bool isEncrypted = false;

        public PacketParser()
        {
            if (GetType().Name == "PacketParser")
            {
                Packet = new Packet();
                Crc32 = new Crc32();
                JvCryption = new JvCryption();
            }
        }

        // Bursting buffer into a sequence of packets
        public static List<byte[]> DefragmentPackets(byte[] FragmentedPackets)
        {
            List<byte[]> DefragmentedPackets = new List<byte[]>();
            ushort BytesRead = 0;

            while (BytesRead < FragmentedPackets.Length)
            {
                ushort PacketLength = BitConverter.ToUInt16(FragmentedPackets, BytesRead + 2);

                if (FragmentedPackets.Skip(BytesRead).Take(2).SequenceEqual(new byte[] { 0xAA, 0x55 }) && FragmentedPackets.Skip(BytesRead + PacketLength + 4).Take(2).SequenceEqual(new byte[] { 0x55, 0xAA }))
                {
                    DefragmentedPackets.Add(FragmentedPackets.Skip(BytesRead).Take(PacketLength + 6).ToArray());                    
                }
                // Unknown packet
                else
                {
                    Debug.WriteLine("[RECV][Unknown packet] " + BitConverter.ToString(FragmentedPackets.Skip(BytesRead).Take(PacketLength + 6).ToArray()).Replace("-", " "));
                }

                BytesRead += (ushort)(PacketLength + 6);
            }

            return DefragmentedPackets;
        }

        // May also be called PacketDecomposer, PacketTranslator or PacketDisposer
        // [DebuggerNonUserCode]
        public dynamic ParsePacket(byte[] ReceivedPacket)
        {
            // Should we use checked statement?
            dynamic ParsedPacket = new ExpandoObject(); // New Harry Potter spell?

            // Yes I know that condition is duplicated
            if (ReceivedPacket.Take(2).SequenceEqual(new byte[] { 0xAA, 0x55 }) && ReceivedPacket.Skip(ReceivedPacket.Length - 2).Take(2).SequenceEqual(new byte[] { 0x55, 0xAA }))
            {
                ParsedPacket.PacketLength = BitConverter.ToUInt16(ReceivedPacket, 2);
                Packet.OpCodes Opcode;

                if (isEncrypted)
                {
                    byte[] DecryptedPacket = JvCryption.Decrypt(ReceivedPacket.Skip(4).Take((ushort)ParsedPacket.PacketLength).ToArray());

                    // Checking the encryption signature
                    if (DecryptedPacket.Take(2).SequenceEqual(new byte[] { 0xFC, 0x1E }))
                    {
                        DecryptedPacket.CopyTo(ReceivedPacket, 4);
                        // Add PacketID to ParsedPacket object?

                        // The snakes in the garden, pray on your downfall
                        if (Enum.IsDefined(typeof(Packet.OpCodes), ReceivedPacket[9]))
                        {
                            Opcode = ParsedPacket.OpCode = (Packet.OpCodes)ReceivedPacket[9];
                        }
                        else
                        {
                            Opcode = ParsedPacket.OpCode = Packet.OpCodes.WIZ_UNKOWN;
                            Debug.WriteLine("[INFO] Unknown packet -> 0x" + ReceivedPacket[9].ToString("X2"));
                        }
                    }
                    else
                    {
                        // Something went wrong
                        Debug.WriteLine("[INFO] Encryption signature not found"); // or detected as you wish

                        return new ExpandoObject(); // Potentially need to be fixed!
                    }
                }
                else
                {
                    if ((Packet.OpCodes)ReceivedPacket.Skip(4).First() == Packet.OpCodes.WIZ_COMPRESS_PACKET)
                    {
                        // Just in case
                        ushort CompressedLength = BitConverter.ToUInt16(ReceivedPacket, 6);
                        ushort OriginalLength = BitConverter.ToUInt16(ReceivedPacket, 8);
                        byte[] crc = ReceivedPacket.Skip(10).Take(4).ToArray();
                        // uint crc = BitConverter.ToUInt32(ReceivedPacket, 10);
                    }

                    // RuntimeBinderException -> Tools > Options (or Debug > Options) > Debugging -> enable Just my Code
                    Opcode = ParsedPacket.OpCode = (Packet.OpCodes)ReceivedPacket[14];
                }

                // Maybe we should add raw packet to ParsedPacket object?

                switch (Opcode)
                {
                    case Packet.OpCodes.WIZ_LOGIN:
                        // Possible responses
                        // 00 -> Nation not selected
                        // 01 -> Karus
                        // 02 -> El Morad
                        // ?? -> There may be other responses?
                        // FF -> Something went wrong also known as fail or e.g. TB_USER strAuthority equal 255
                        ParsedPacket.Nation = ReceivedPacket[10];
                        // Possible responses for LS_LOGIN_REQ packet
                        // AA 55 02 00 F3 01 55 AA = Logged in
                        // AA 55 02 00 F3 02 55 AA = Account doesn't exist
                        // AA 55 02 00 F3 03 55 AA = Wrong password
                        // AA 55 02 00 F3 04 55 AA = Account banned
                        // AA 55 02 00 F3 05 55 AA = In game?
                        // AA 55 02 00 F3 06 55 AA = Authentication error
                        // AA 55 02 00 F3 FF 55 AA = Authentication failed
                        break;

                    case Packet.OpCodes.WIZ_NEW_CHAR:
                        // Possible responses
                        // 00 -> Character created
                        // 01 -> All slots occupied
                        // 02 -> Incorrect/invalid data
                        // 03 -> Character name taken
                        // 04 -> Error occured
                        // ?? -> There may be other responses?
                        ParsedPacket.Code = ReceivedPacket[10];
                        break;

                    case Packet.OpCodes.WIZ_SEL_CHAR:
                        ParsedPacket.Code = ReceivedPacket[10];
                        ParsedPacket.CurrentZoneID = ReceivedPacket[11];
                        ParsedPacket.CurrentPositionX = BitConverter.ToInt16(ReceivedPacket, 12) / 10.0f;
                        ParsedPacket.CurrentPositionZ = BitConverter.ToInt16(ReceivedPacket, 14) / 10.0f;
                        ParsedPacket.CurrentPositionY = BitConverter.ToInt16(ReceivedPacket, 16) / 10.0f;
                        ParsedPacket.VictoryNation = ReceivedPacket[18];
                        ParsedPacket.AuthHash = ReceivedPacket.Skip(20).Take(ReceivedPacket[19]).ToArray();
                        // ParsedPacket.AuthHash = Encoding.ASCII.GetString(ReceivedPacket.Skip(20).Take(ReceivedPacket[19]).ToArray());                        
                        break;

                    case Packet.OpCodes.WIZ_VERSION_CHECK:
                        ParsedPacket.ServerVersion = BitConverter.ToInt16(ReceivedPacket, 15);
                        JvCryption.PublicKey = ParsedPacket.PubliceKey = ReceivedPacket.Skip(17).Take(8).ToArray();
                        ParsedPacket.PseudoAuthID = ReceivedPacket[25];
                        break;

                    case Packet.OpCodes.WIZ_ALLCHAR_INFO_REQ:
                        ParsedPacket.Code = ReceivedPacket[10];

                        // If request was successful, the server returns 0x01
                        if (ParsedPacket.Code == 0x01)
                        {
                            // The order is as follows: middle slot, left slot, right slot
                            object[] CharacterList = new object[Defines.MaxCharactersPerAccount];
                            int Offset = 11;

                            for (byte h = 0; h < Defines.MaxCharactersPerAccount; ++h)
                            {
                                {
                                    ushort CharacterIDLength = BitConverter.ToUInt16(ReceivedPacket, Offset);
                                    string CharacterID = Encoding.ASCII.GetString(ReceivedPacket.Skip(Offset + 2).Take(CharacterIDLength).ToArray());
                                    Offset += CharacterIDLength + 2;
                                    byte Race = ReceivedPacket[Offset];
                                    byte Class = (byte)BitConverter.ToUInt16(ReceivedPacket, Offset + 1); // Ask MGAME why this variable is a short... especially since the data it contains is in the byte range type
                                    byte Level = ReceivedPacket[Offset + 3];
                                    byte Face = ReceivedPacket[Offset + 4];
                                    byte Hair = ReceivedPacket[Offset + 5];
                                    byte ZoneID = ReceivedPacket[Offset + 6];
                                    // Items
                                    uint HelmetItemID = BitConverter.ToUInt32(ReceivedPacket, Offset + 7);
                                    ushort HelmetDurability = BitConverter.ToUInt16(ReceivedPacket, Offset + 11);
                                    uint PauldronItemID = BitConverter.ToUInt32(ReceivedPacket, Offset + 13);
                                    ushort PauldronDurability = BitConverter.ToUInt16(ReceivedPacket, Offset + 17);
                                    uint CloakItemID = BitConverter.ToUInt32(ReceivedPacket, Offset + 19);
                                    ushort CloakDurability = BitConverter.ToUInt16(ReceivedPacket, Offset + 23);
                                    uint LeftWeaponItemID = BitConverter.ToUInt32(ReceivedPacket, Offset + 25);
                                    ushort LeftWeaponDurability = BitConverter.ToUInt16(ReceivedPacket, Offset + 29);
                                    uint RightWeaponItemID = BitConverter.ToUInt32(ReceivedPacket, Offset + 31);
                                    ushort RightWeaponDurability = BitConverter.ToUInt16(ReceivedPacket, Offset + 35);
                                    uint PadItemID = BitConverter.ToUInt32(ReceivedPacket, Offset + 37);
                                    ushort PadDurability = BitConverter.ToUInt16(ReceivedPacket, Offset + 41);
                                    uint GlovesItemID = BitConverter.ToUInt32(ReceivedPacket, Offset + 43);
                                    ushort GlovesDurability = BitConverter.ToUInt16(ReceivedPacket, Offset + 47);
                                    uint BootsItemID = BitConverter.ToUInt32(ReceivedPacket, Offset + 49);
                                    ushort BootsDurability = BitConverter.ToUInt16(ReceivedPacket, Offset + 53);
                                    Offset += 55;

                                    // The funniest thing is that we only need CharacterID, Class and ZoneID but let's grab everything and clutter the memory because we can afford
                                    CharacterList[h] = new
                                    {
                                        CharacterID,
                                        Race,
                                        Class,
                                        Level,
                                        Face,
                                        Hair,
                                        ZoneID,
                                        HelmetItem = new { ID = HelmetItemID, Durability = HelmetDurability },
                                        PauldronItem = new { ID = PauldronItemID, Durability = PauldronDurability },
                                        CloakItem = new { ID = CloakItemID, Durability = CloakDurability },
                                        LeftWeaponItem = new { ID = LeftWeaponItemID, Durability = LeftWeaponDurability },
                                        RightWeaponItem = new { ID = RightWeaponItemID, Durability = RightWeaponDurability },
                                        PadItem = new { ID = PadItemID, Durability = PadDurability },
                                        GlovesItem = new { ID = GlovesItemID, Durability = GlovesDurability },
                                        BootsItem = new { ID = BootsItemID, Durability = BootsDurability }
                                    };
                                }
                            }

                            ParsedPacket.CharacterList = CharacterList;
                        }
                        break;

                    case Packet.OpCodes.WIZ_GAMESTART:
                        // Nothing happens here, no additional data
                        break;

                    case Packet.OpCodes.WIZ_MYINFO:
                        {
                            ParsedPacket.SocketID = BitConverter.ToUInt16(ReceivedPacket, 10);
                            byte CharacterIDLength = ReceivedPacket[12];
                            ParsedPacket.CharacterID = Encoding.ASCII.GetString(ReceivedPacket.Skip(13).Take(CharacterIDLength).ToArray());
                            ushort Offset = (ushort)(CharacterIDLength + 13);
                            ParsedPacket.CurrentPositionX = BitConverter.ToUInt16(ReceivedPacket, Offset) / 10.0f;
                            ParsedPacket.CurrentPositionZ = BitConverter.ToUInt16(ReceivedPacket, Offset + 2) / 10.0f;
                            ParsedPacket.CurrentPositionY = BitConverter.ToUInt16(ReceivedPacket, Offset + 4) / 10.0f; // I haven't checked if it should be a ushort yet
                            ParsedPacket.Nation = ReceivedPacket[Offset + 6];
                            ParsedPacket.Race = ReceivedPacket[Offset + 7];
                            ParsedPacket.Class = BitConverter.ToUInt16(ReceivedPacket, Offset + 8); // As I said before it should be a byte
                            ParsedPacket.Face = ReceivedPacket[Offset + 10];
                            ParsedPacket.Hair = ReceivedPacket[Offset + 11];
                            ParsedPacket.IsKing = Convert.ToBoolean(ReceivedPacket[Offset + 12]);
                            ParsedPacket.Title = ReceivedPacket[Offset + 13];
                            ParsedPacket.Level = ReceivedPacket[Offset + 14];
                            ParsedPacket.RemainingStatisticsPoints = ReceivedPacket[Offset + 15]; // May also be called FreeStatisticsPoints
                            ParsedPacket.RequiredExperiencePointsToLevelUp = BitConverter.ToUInt32(ReceivedPacket, Offset + 16);
                            ParsedPacket.ExperiencePoints = BitConverter.ToUInt32(ReceivedPacket, Offset + 20);
                            ParsedPacket.NationalPoints = BitConverter.ToUInt32(ReceivedPacket, Offset + 24); // May also be called LoyaltyPoints or RealmPoints
                            ParsedPacket.MonthlyNationalPoints = BitConverter.ToUInt32(ReceivedPacket, Offset + 28); // May also be called MonthlyLoyaltyPoints or MonthlyRealmPoints
                            ParsedPacket.City = ReceivedPacket[Offset + 32];
                            ParsedPacket.ClanID = BitConverter.ToUInt16(ReceivedPacket, Offset + 33);
                            ParsedPacket.ClanDuty = ReceivedPacket[Offset + 35];
                            ParsedPacket.AllianceID = BitConverter.ToUInt16(ReceivedPacket, Offset + 36);
                            ParsedPacket.AllianceFlag = ReceivedPacket[Offset + 38];
                            byte ClanNameLength = ReceivedPacket[Offset + 39];
                            ParsedPacket.ClanName = Encoding.ASCII.GetString(ReceivedPacket.Skip(Offset + 40).Take(ClanNameLength).ToArray());
                            Offset += (ushort)(ClanNameLength + 40);
                            ParsedPacket.ClanGrade = ReceivedPacket[Offset];
                            ParsedPacket.ClanRank = ReceivedPacket[Offset + 1];
                            ParsedPacket.ClanMarkVersion = BitConverter.ToUInt16(ReceivedPacket, Offset + 2);
                            ParsedPacket.ClanCapeID = BitConverter.ToUInt16(ReceivedPacket, Offset + 4);
                            ParsedPacket.MaxHP = BitConverter.ToUInt16(ReceivedPacket, Offset + 6);
                            ParsedPacket.HP = BitConverter.ToUInt16(ReceivedPacket, Offset + 8);
                            ParsedPacket.MaxMSP = BitConverter.ToUInt16(ReceivedPacket, Offset + 10);
                            ParsedPacket.MSP = BitConverter.ToUInt16(ReceivedPacket, Offset + 12);
                            ParsedPacket.MaxWeight = BitConverter.ToUInt16(ReceivedPacket, Offset + 14) / 10.0f;
                            ParsedPacket.Weight = BitConverter.ToUInt16(ReceivedPacket, Offset + 16) / 10.0f;

                            // Delta may also be called Bonus
                            ParsedPacket.StatisticsPoints = new
                            {
                                STR = new { Base = ReceivedPacket[Offset + 18], Delta = ReceivedPacket[Offset + 19] },
                                HP  = new { Base = ReceivedPacket[Offset + 20], Delta = ReceivedPacket[Offset + 21] },
                                DEX = new { Base = ReceivedPacket[Offset + 22], Delta = ReceivedPacket[Offset + 23] },
                                INT = new { Base = ReceivedPacket[Offset + 24], Delta = ReceivedPacket[Offset + 25] },
                                MP  = new { Base = ReceivedPacket[Offset + 26], Delta = ReceivedPacket[Offset + 27] }
                            };

                            ParsedPacket.Attack = BitConverter.ToUInt16(ReceivedPacket, Offset + 28);
                            ParsedPacket.Defense = BitConverter.ToUInt16(ReceivedPacket, Offset + 30);

                            ParsedPacket.Resistances = new
                            {
                                FireResistance = ReceivedPacket[Offset + 32],
                                GlacierResistance = ReceivedPacket[Offset + 33],
                                LightningResistance = ReceivedPacket[Offset + 34],
                                MagicResistance = ReceivedPacket[Offset + 35],
                                CourseResistance = ReceivedPacket[Offset + 36],
                                PoisonResistance = ReceivedPacket[Offset + 37]
                            };

                            ParsedPacket.Gold = BitConverter.ToUInt32(ReceivedPacket, Offset + 38);
                            ParsedPacket.Authority = ReceivedPacket[Offset + 42];
                            ParsedPacket.Rank = ReceivedPacket[Offset + 43]; // I'm not sure about this variable
                            ParsedPacket.PersonalRank = ReceivedPacket[Offset + 44]; // I'm not sure about this variable

                            // If the Leadership, Politics, Language or Siege weapons points are greater than 0 then server resets all points                            
                            // object[] SkillList = new object[Defines.SkillsSequenceLength];

                            // for (byte h = 0; h < Defines.SkillsSequenceLength; ++h)
                            // {
                            //     SkillList[h] = ReceivedPacket[Offset + 45 + h];
                            // }

                            // ParsedPacket.SkillPoints = SkillList;                            
                            
                            ParsedPacket.SkillPoints = new
                            {
                                FreePoints = ReceivedPacket[Offset + 45],
                                Leadership = ReceivedPacket[Offset + 46],
                                Politics = ReceivedPacket[Offset + 47],
                                Language = ReceivedPacket[Offset + 48],
                                SiegeWeapons = ReceivedPacket[Offset + 49],
                                Tree1 = ReceivedPacket[Offset + 50],
                                Tree2 = ReceivedPacket[Offset + 51],
                                Tree3 = ReceivedPacket[Offset + 52],
                                Master = ReceivedPacket[Offset + 53]
                            };
                            
                            Offset += Defines.SkillsSequenceLength + 45;
                            // Items section
                            object[] EquippedItemList = new object[Defines.EquippedItemSlots];
                            object[] InventoryItemList = new object[Defines.InventoryItemSlots];

                            for (byte h = 0; h < Defines.EquippedItemSlots; ++h)
                            {
                                EquippedItemList[h] = new
                                {
                                    ID = BitConverter.ToUInt32(ReceivedPacket, Offset),
                                    Durability = BitConverter.ToUInt16(ReceivedPacket, Offset + 4),
                                    Count = BitConverter.ToUInt16(ReceivedPacket, Offset + 6), // May also be called Amount
                                    RentalFlag = ReceivedPacket[Offset + 8],
                                    RemainingRentalTime = BitConverter.ToUInt16(ReceivedPacket, Offset + 9)
                                };

                                Offset += 11;
                            }

                            for (byte h = 0; h < Defines.InventoryItemSlots; ++h)
                            {
                                InventoryItemList[h] = new
                                {
                                    ID = BitConverter.ToUInt32(ReceivedPacket, Offset),
                                    Durability = BitConverter.ToUInt16(ReceivedPacket, Offset + 4),
                                    Count = BitConverter.ToUInt16(ReceivedPacket, Offset + 6), // May also be called Amount
                                    RentalFlag = ReceivedPacket[Offset + 8],
                                    RemainingRentalTime = BitConverter.ToUInt16(ReceivedPacket, Offset + 9)
                                };

                                Offset += 11;
                            }

                            ParsedPacket.EquippedItemList = EquippedItemList;
                            ParsedPacket.InventoryItemList = InventoryItemList;
                            ParsedPacket.AccountStatus = ReceivedPacket[Offset]; // Most likely but needs verification
                            ParsedPacket.PremiumType = ReceivedPacket[Offset + 1];
                            ParsedPacket.PremiumTime = BitConverter.ToUInt16(ReceivedPacket, Offset + 2); // Premium time counted in days
                            ParsedPacket.IsChicken = Convert.ToBoolean(ReceivedPacket[Offset + 4]);
                            ParsedPacket.MannerPoints = BitConverter.ToUInt32(ReceivedPacket, Offset + 5);
                        }
                        break;

                    case Packet.OpCodes.WIZ_TIME:
                        ParsedPacket.Year = BitConverter.ToUInt16(ReceivedPacket, 10);
                        ParsedPacket.Month = BitConverter.ToUInt16(ReceivedPacket, 12);
                        ParsedPacket.Day = BitConverter.ToUInt16(ReceivedPacket, 14);
                        ParsedPacket.Hour = BitConverter.ToUInt16(ReceivedPacket, 16);
                        ParsedPacket.Minute = BitConverter.ToUInt16(ReceivedPacket, 18);
                        break;

                    case Packet.OpCodes.WIZ_WEATHER:
                        // Possible weathers
                        // 01 -> Sunny
                        // 02 -> Rain
                        // 03 -> Snow
                        ParsedPacket.Weather = ReceivedPacket[10];
                        ParsedPacket.WeatherAmount = BitConverter.ToUInt16(ReceivedPacket, 11); ; // May also be called Percentage
                        break;

                    case Packet.OpCodes.WIZ_NOTICE:
                        // Requires investigation
                        break;

                    case Packet.OpCodes.WIZ_COMPRESS_PACKET:
                        // Not implemented yet
                        break;

                    case Packet.OpCodes.WIZ_FRIEND_PROCESS:
                        // Not implemented yet
                        break;

                    case Packet.OpCodes.WIZ_ZONEABILITY:
                        ParsedPacket.Code = ReceivedPacket[10];

                        // If request was successful, the server returns 0x01
                        if (ParsedPacket.Code == 0x01)
                        {
                            ParsedPacket.CanTradeWithOtherNation = Convert.ToBoolean(ReceivedPacket[11]);
                            ParsedPacket.ZoneType = ReceivedPacket[12];
                            ParsedPacket.CanTalkToOtherNation = Convert.ToBoolean(ReceivedPacket[13]);
                            ParsedPacket.Tariff = BitConverter.ToUInt16(ReceivedPacket, 14); // Zone tax
                        }

                        // Getting/receive 5E 02 02 00 packet means getting AC buff
                        break;

                    case Packet.OpCodes.WIZ_SERVER_INDEX:
                        ParsedPacket.Code = ReceivedPacket[10];

                        // If request was successful, the server returns 0x01
                        if (ParsedPacket.Code == 0x01)
                        {
                            ParsedPacket.ServerNo = BitConverter.ToUInt16(ReceivedPacket, 12);
                        }
                        break;

                    case Packet.OpCodes.WIZ_QUEST:
                        // Not implemented yet and requires investigation
                        // 02 00 00 00
                        break;

                    case Packet.OpCodes.WIZ_PREMIUM:
                        ParsedPacket.Code = ReceivedPacket[10];

                        // If request was successful, the server returns 0x01
                        if (ParsedPacket.Code == 0x01)
                        {
                            // Possible Premium types
                            // 00 -> No premium, that's obvious
                            // 01 -> Bronze premium
                            // 02 -> Silver premium
                            // 03 -> Gold premium
                            // ?? -> Platinum premium
                            ParsedPacket.PremiumType = ReceivedPacket[11];
                            ParsedPacket.PremiumTime = BitConverter.ToUInt32(ReceivedPacket, 12); // Premium time counted in days and I doubt if it's unsigned int but ok
                        }
                        break;

                    // Unsupported packet
                    default:
                        // Debug.WriteLine(BitConverter.ToString(ReceivedPacket).Replace("-", " "));
                        break;
                }

                // For debug purpose
                Debug.WriteLine("[RECV] " + BitConverter.ToString(ReceivedPacket).Replace("-", " "));
            }
            else
            {
                ParsedPacket.OpCode = Packet.OpCodes.WIZ_UNKOWN;
                Debug.WriteLine("[RECV WTF] " + BitConverter.ToString(ReceivedPacket).Replace("-", " "));
            }

            return ParsedPacket;
        }
    }

    // May also be called PacketComposer or PacketBuilder
    public class Packet : PacketParser
    {
        private OpCodes PacketOpCode;
        private uint PacketID;
        private List<byte> Data = new List<byte>();

        // TODO: Gaps should be filled and could potentially be transferred to global definitions
        public enum OpCodes : byte
        {
            WIZ_LOGIN             = 0x01, // Account login
            WIZ_NEW_CHAR          = 0x02, // Create character
            WIZ_DEL_CHAR          = 0x03, // Delete character
            WIZ_SEL_CHAR          = 0x04, // Select character
            WIZ_SEL_NATION        = 0x05, // Select nation
            WIZ_MOVE              = 0x06, // Move character
            WIZ_ALLCHAR_INFO_REQ  = 0x0C, // Request account informations about all characters
            WIZ_GAMESTART         = 0x0D, // Starting the game, which consequently gives a lot of informations
            WIZ_MYINFO            = 0x0E, // Packet about your character
            WIZ_LOGOUT            = 0x0F, // Logout request
            WIZ_TIME              = 0x13, // Game server time
            WIZ_WEATHER           = 0x14, // Game weather
            WIZ_VERSION_CHECK     = 0x2B,
            WIZ_NOTICE            = 0x2E, // Notice packet
            WIZ_SPEEDHACK_CHECK   = 0x41, // Cyclic packet "maintaining" the connection with server
            WIZ_COMPRESS_PACKET   = 0x42,
            WIZ_SERVER_CHANGE     = 0x46,   
            WIZ_FRIEND_PROCESS    = 0x49, // Get the status of your friends, don't kid yourself, you don't have any
            WIZ_ZONEABILITY       = 0x5E,
            WIZ_QUEST             = 0x64,
            WIZ_SERVER_INDEX      = 0x6B,
            WIZ_PREMIUM           = 0x71,
            WIZ_RENTAL            = 0x73,
            WIZ_SKILLDATA         = 0x79,
            WIZ_UNKOWN            = 0xFF, // Don't delete it!!!
        }

        public Packet OpCode(OpCodes OpCode)
        {
            PacketOpCode = OpCode;

            return this;
        }

        public Packet AddByte(byte Value)
        {
            Data.Add(Value);

            return this;
        }

        public Packet AddShort(short Value)
        {
            Data.AddRange(BitConverter.GetBytes(Value));

            return this;
        }

        public Packet AddShort(ushort Value)
        {
            Data.AddRange(BitConverter.GetBytes(Value));

            return this;
        }

        public Packet AddFloat(float Value)
        {
            Data.AddRange(BitConverter.GetBytes(Value));

            return this;
        }

        // TODO: You need to add missing methods related to data types (e.g. DWORD aka int or long)

        public Packet AddString(string Value)
        {
            // If operation speed is unsatisfactory then use Buffer.BlockCopy()
            Data.AddRange(new byte[] { Convert.ToByte(Value.Length), 0x00 }.Concat(Encoding.ASCII.GetBytes(Value)).ToArray());

            return this;
        }

        public byte[] Build()
        {
            List<byte> Packet = new List<byte>();
            Packet.AddRange(new byte[] { 0xAA, 0x55 }); // Packet header
            // Packet.AddRange(new byte[] { 0x00, 0x00 }); // Packet size

            if (PacketID > 0)
            {
                Packet.AddRange(BitConverter.GetBytes(PacketID));
            }

            ++PacketID;
            Packet.Add(Convert.ToByte(PacketOpCode));
            Packet.AddRange(Data);
            // TODO: Add packet compression support

            // Adding a CRC32 checksum to the packet
            if (PacketOpCode != OpCodes.WIZ_VERSION_CHECK)
            {
                Packet.AddRange(Crc32.ComputeChecksumBytes(Packet.Skip(2).ToArray()));
            }

            Packet.AddRange(new byte[] { 0x55, 0xAA }); // Packet tail
            int PacketDataLength = Packet.Count - 4;
            // Magic. Do not touch!
            // Packet.RemoveRange(2, 2);
            Packet.InsertRange(2, BitConverter.GetBytes((ushort)PacketDataLength));
            // For debug purpose
            Debug.WriteLine("[SEND] " + BitConverter.ToString(Packet.ToArray()).Replace("-", " "));

            if (isEncrypted)
            {
                byte[] EncryptedData = JvCryption.Encrypt(Packet.GetRange(4, PacketDataLength).ToArray());
                Packet.RemoveRange(4, PacketDataLength);
                Packet.InsertRange(4, EncryptedData);

                // byte[] EncryptedData = JvCryption.Encrypt(Packet.GetRange(4, PacketDataLength).ToArray());

                // for (int h = 4, i = 0; i < EncryptedData.Length; ++h, ++i)
                // {
                //     Packet[h] = EncryptedData[i];
                // }
            }

            // Clearing buffer
            Data.Clear();

            return Packet.ToArray();
        }
    }
}