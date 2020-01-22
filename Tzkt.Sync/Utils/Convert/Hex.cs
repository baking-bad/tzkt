using System;

namespace Tzkt.Sync.Utils
{
    public static class Hex
    {
        private static readonly int[] HexAscii = new[]
        {
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            0,   1,   2,   3,   4,   5,   6,   7,   8,   9,   255, 255, 255, 255, 255, 255,
            255, 10,  11,  12,  13,  14,  15,  255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 10,  11,  12,  13,  14,  15,  255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255
        };

        public static string Convert(byte[] bytes)
        {
            var chars = new char[bytes.Length << 1];

            for (int i = 0, j = 0; i < bytes.Length; ++i)
            {
                var l = bytes[i] & 15;
                var h = bytes[i] >> 4;

                chars[j++] = (char)(h + (h > 9 ? 87 : 48));
                chars[j++] = (char)(l + (l > 9 ? 87 : 48));
            }

            return new String(chars);
        }

        public static byte[] Parse(string hex)
        {
            if (String.IsNullOrEmpty(hex))
                throw new ArgumentNullException(nameof(hex));

            if (hex.Length % 2 > 0)
                throw new FormatException("Invalid hex string");

            var pos = hex[0] == '0' && hex[1] == 'x' ? 2 : 0;
            byte[] bytes = new byte[(hex.Length - pos) >> 1];

            for (int h, l, i = 0; i < bytes.Length; i++, pos += 2)
            {
                h = HexAscii[hex[pos]];
                l = HexAscii[hex[pos + 1]];

                if ((h | l) == 255)
                    throw new FormatException("Invalid hex string");

                bytes[i] = (byte)((h << 4) + l);
            }

            return bytes;
        }

        public static bool TryParse(string hex, out byte[] bytes)
        {
            bytes = null;

            if (String.IsNullOrEmpty(hex))
                return false;

            if (hex.Length % 2 > 0)
                return false;

            var pos = hex[0] == '0' && hex[1] == 'x' ? 2 : 0;
            bytes = new byte[(hex.Length - pos) >> 1];

            for (int h, l, i = 0; i < bytes.Length; ++i, pos += 2)
            {
                h = HexAscii[hex[pos]];
                l = HexAscii[hex[pos + 1]];

                if ((h | l) == 255)
                    return false;

                bytes[i] = (byte)((h << 4) + l);
            }

            return true;
        }
    }
}
