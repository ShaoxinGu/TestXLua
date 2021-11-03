using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class AssetUtility
{
    private static readonly CRC32 crc32 = new CRC32();

    /// <summary>
    /// 计算一个Stream对象的CRC32散列码
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string GetCRC32Hash(Stream input)
    {
        byte[] data = crc32.ComputeHash(input);
        return ToHash(data);
    }

    private static string ToHash(byte[] data)
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte t in data)
        {
            sb.Append(t.ToString("x2"));
        }
        return sb.ToString();
    }

    internal sealed class CRC32 : HashAlgorithm
    {
        private const uint DEFAULT_POLYNOMIAL = 0xedb88320u;
        private const uint DEFAULT_SEED = 0xffffffffu;

        private static uint[] DefaultTable;

        private readonly uint seed;
        private readonly uint[] table;
        private uint hash;

        public CRC32() : this(DEFAULT_POLYNOMIAL, DEFAULT_SEED) { }

        public CRC32(uint polynomial, uint init)
        {
            if (!BitConverter.IsLittleEndian)
                throw new PlatformNotSupportedException("Not supported on Big Endian processors");

            table = InitializeTable(polynomial);
            seed = hash = init;
        }

        public override int HashSize
        {
            get { return 32; }
        }

        public override void Initialize()
        {
            hash = seed;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            hash = CalculateHash(table, hash, array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            var hashBuffer = UInt32ToBigEndianBytes(~hash);
            HashValue = hashBuffer;
            return hashBuffer;
        }

        public static uint Compute(byte[] buffer)
        {
            return Compute(DEFAULT_SEED, buffer);
        }

        public static uint Compute(uint seed, byte[] buffer)
        {
            return Compute(DEFAULT_POLYNOMIAL, seed, buffer);
        }

        public static uint Compute(uint polynomial, uint seed, byte[] buffer)
        {
            return ~CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length);
        }

        private static uint[] InitializeTable(uint polynomial)
        {
            if (polynomial == DEFAULT_POLYNOMIAL && DefaultTable != null)
                return DefaultTable;
            var createTable = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var entry = (uint)i;
                for (var j = 0; j < 8; j++)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry >>= 1;
                createTable[i] = entry;
            }
            if (polynomial == DEFAULT_POLYNOMIAL)
                DefaultTable = createTable;
            return createTable;
        }

        private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int size)
        {
            var hashValue = seed;
            for (var i = start; i < start + size; i++)
                hashValue = (hashValue >> 8) ^ table[buffer[i] ^ (hashValue & 0xff)];
            return hashValue;
        }

        private static byte[] UInt32ToBigEndianBytes(uint uint32)
        {
            var result = BitConverter.GetBytes(uint32);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);
            return result;
        }
    }
}
