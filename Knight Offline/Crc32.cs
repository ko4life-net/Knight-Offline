using System;

namespace Knight_Offline
{
    public class Crc32 : PacketParser
    {
        private readonly uint[] Table;

        public Crc32()
        {
            uint Poly = 0xEDB88320;
            Table = new uint[256];
            uint Temp = 0;

            for (uint i = 0; i < 256; ++i)
            {
                Temp = i;

                for (int j = 8; j > 0; --j)
                {
                    if ((Temp & 1) == 1)
                    {
                        Temp = (uint)((Temp >> 1) ^ Poly);
                    }
                    else
                    {
                        Temp >>= 1;
                    }
                }

                Table[i] = Temp;
            }
        }

        public uint ComputeChecksum(byte[] Bytes)
        {
            uint crc = 0xFFFFFFFF;

            for (int i = 0, NumberOfBytes = Bytes.Length; i < NumberOfBytes; ++i)
            {
                byte index = (byte)(((crc) & 0xFF) ^ Bytes[i]);
                crc = (uint)((crc >> 8) ^ Table[index]);
            }

            return crc;
        }

        public byte[] ComputeChecksumBytes(byte[] bytes)
        {
            return BitConverter.GetBytes(ComputeChecksum(bytes));
        }
    }
}