using System;

namespace Tzkt.Sync
{
    static class BytesExtension
    {
        public static byte[] Concat(this byte[] src, byte[] data)
        {
            byte[] res = new byte[src.Length + data.Length];
            Buffer.BlockCopy(src, 0, res, 0, src.Length);
            Buffer.BlockCopy(data, 0, res, src.Length, data.Length);
            return res;
        }

        public static byte[] Concat(this byte[] src, byte[] data, int count)
        {
            byte[] res = new byte[src.Length + count];
            Buffer.BlockCopy(src, 0, res, 0, src.Length);
            Buffer.BlockCopy(data, 0, res, src.Length, count);
            return res;
        }

        public static byte[] Reverse(this byte[] data)
        {
            var res = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
                res[i] = data[data.Length - 1 - i];

            return res;
        }

        public static byte[] GetBytes(this byte[] src, int start, int length)
        {
            var res = new byte[length];
            Buffer.BlockCopy(src, start, res, 0, length);
            return res;
        }

        public static bool IsEqual(this byte[] src, byte[] data)
        {
            if (src.Length != data.Length)
                return false;

            for (int i = 0; i < src.Length; i++)
                if (src[i] != data[i])
                    return false;

            return true;
        }

        public static void Flush(this byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = 0;
        }

        public static bool StartWith(this byte[] src, byte[] data)
        {
            if (src.Length < data.Length)
                return false;

            for (int i = 0; i < data.Length; i++)
                if (src[i] != data[i])
                    return false;

            return true;
        }

        public static int ToInt32(this byte[] bytes)
        {
            if (bytes.Length > 4)
                throw new Exception("Failed to read Int32 from bytes");

            var res = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                res <<= 8;
                res += bytes[i];
            }
            return res;
        }

        public static int ReadInt32(this byte[] bytes, int pos)
        {
            var res = 0;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            return res;
        }

        public static long ReadInt64(this byte[] bytes, int pos)
        {
            var res = 0L;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            res <<= 8;
            res += bytes[pos++];
            return res;
        }
    }
}
