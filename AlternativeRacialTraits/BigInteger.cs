using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AlternativeRacialTraits
{
     class BigInteger {
        Byte[] bytes = null;
        bool neg = false;

        static readonly String allowedchars = "0123456789abcdef";

        public int Length() { return (bytes == null) ? 0 : bytes.Length; }

        // parses a string, but fails for anything that isn't a hexstring
        public static BigInteger Parse(String str, NumberStyles ignoredStyle) {
            if (ignoredStyle != NumberStyles.HexNumber) throw new NotImplementedException("Non hex styles are not implemented!");
            var biggie = new BigInteger();
            // trim leading zeroes, but make sure we have an even number of chars
            var charBase = str.Trim().ToLower();
            if (charBase.Length != 0 && allowedchars.IndexOf(charBase[0]) >= 8) biggie.neg = true;
            charBase = charBase.TrimStart('0');
            if (charBase.Length % 2 != 0) charBase = charBase.Insert(0, biggie.neg ? "f" : "0");
            // let's make it a char array
            var chars = charBase.ToCharArray();
            // byte array length is div 2 because 00..FF can fit into 1 byte
            int length = (chars.Length / 2);
            if (length != 0) {
                biggie.bytes = new Byte[length];
                int current = 0; // buffer for current byte
                for (int i = 0; i < chars.Length; ++i) {
                    int next = allowedchars.IndexOf(chars[i]);
                    if (next == -1) throw new FormatException("this is not a hex string!");
                    if (i % 2 == 0) {
                        current = next;
                    } else {
                        biggie.bytes[i / 2] = (byte)(current * 16 + next);
                    }
                }
            }
            return biggie;
        }

        // doesn't actually parse, but should cover the usage in EA
        public static bool TryParse(String input, out BigInteger result) {
            String numericchars = "0123456789";
            result = new BigInteger();
            return input.Trim().ToCharArray().All(c => numericchars.IndexOf(c) != -1);
        }

        public static BigInteger operator ^(BigInteger a, BigInteger b) {
            if (a.bytes == null) return b;
            if (b.bytes == null) return a;
            var c = new BigInteger();
            if (a.Length() < b.Length()) {
                int l = b.Length();
                int o = l - a.Length();
                c.bytes = new byte[l];
                Array.Copy(b.bytes, c.bytes, l);
                for (int i = 0; i < a.Length(); ++i) {
                    c.bytes[i + o] ^= a.bytes[i];
                }
            } else {
                int l = a.Length();
                int o = l - b.Length();
                c.bytes = new byte[l];
                Array.Copy(a.bytes, c.bytes, l);
                for (int i = 0; i < b.Length(); ++i) {
                    c.bytes[i + o] ^= b.bytes[i];
                }
            }
            c.neg = c.bytes[0] > 127;
            return c;
        }

        public String ToString(String ignore) {
            if (ignore != "x32") throw new NotImplementedException("Non x32 styles are not implemented!");
            StringBuilder retStr = new StringBuilder(32);
            int bLength = bytes?.Length * 2 ?? 0;
            if (bLength < 32) { retStr.Append(neg ? 'f' : '0', 32 - bLength); };
            if (bytes != null) {
                Array.ForEach(bytes, b => { retStr.Append(b.ToString("x2")); });
            }
            var str = retStr.ToString();
            if (!neg && allowedchars.IndexOf(str[0]) >= 8) str = "0" + str;
            if (neg) {
                while (str.Length > 32 && allowedchars.IndexOf(str[1]) >= 8 && str[0] == 'f') {
                    str = str.Substring(1);
                }
            }
            return str;
        }
}
}