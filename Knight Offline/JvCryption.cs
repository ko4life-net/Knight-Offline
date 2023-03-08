using System;
using System.Collections;

namespace Knight_Offline
{
    public class JvCryption : PacketParser
    {
        // private readonly long PrivateKey = 0x1234567890123456;
        private readonly byte[] PrivateKey = { 0x56, 0x34, 0x12, 0x90, 0x78, 0x56, 0x34, 0x12 };
        private readonly byte[] TransactionKey = new byte[8];

        public byte[] PublicKey
        {
            set
            {
                if (isEncrypted)
                {
                    throw new Exception("Value has already been set!");
                }
                
                new BitArray(value).Xor(new BitArray(PrivateKey)).CopyTo(TransactionKey, 0);
                isEncrypted = true;
            }
        }

        public byte[] Encrypt(byte[] Data) => Decrypt(Data);

        public byte[] Decrypt(byte[] Data)
        {
            int DataLength = Data.Length;
            byte[] DataOut = new byte[Data.Length];
            int RKey = 0x086D;
            int LengthKey = (DataLength * 0x9D) & 0xFF;

            for (int h = 0; h < DataLength; ++h)
            {
                byte RSK = (byte)((RKey >> 0x08) & 0xFF);
                DataOut[h] = (byte)(((Data[h] ^ RSK) ^ TransactionKey[h % 8]) ^ LengthKey);
                RKey *= 0x087B;
            }

            return DataOut;
        }
    }
}