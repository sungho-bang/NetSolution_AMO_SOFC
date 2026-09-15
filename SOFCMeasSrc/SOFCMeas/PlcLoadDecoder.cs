using System;
using System.Globalization;

namespace SOFCMeas
{
    internal static class PlcLoadDecoder
    {
        internal const ushort WordCount = 5;

        internal static int ToLegacyRawValue(double loadKgf, double loadScale)
        {
            // The legacy integer is only a compatibility representation. Live
            // graphs and current CSV files retain kgf even when this saturates.
            double value = Math.Round(loadKgf * loadScale, MidpointRounding.AwayFromZero);
            if (value <= int.MinValue) return int.MinValue;
            if (value >= int.MaxValue) return int.MaxValue;
            return (int)value;
        }

        internal static bool TryDecode(ushort[] words, out double loadKgf)
        {
            string reason;
            return TryDecodeFrame(words, out loadKgf, out reason);
        }

        internal static bool TryDecodeFrame(ushort[] words, out double loadKgf, out string reason)
        {
            loadKgf = 0D;
            reason = null;
            if (words == null || words.Length != WordCount)
            {
                reason = "invalid_word_count";
                return false;
            }

            // D700..D704: 02 31 2B 20 30 2E 34 31 30 03 -> STX + "1+ 0.410" + ETX.
            // Each WORD stores the first ASCII character in its low byte.
            char[] characters = new char[WordCount * 2];
            for (int index = 0; index < words.Length; index++)
            {
                characters[index * 2] = (char)(words[index] & 0xFF);
                characters[index * 2 + 1] = (char)(words[index] >> 8);
            }

            // Validate the complete supported frame before interpreting any load.
            // The observed header is '1'; its device-specific meaning is not assumed.
            if (characters[0] != '\x02')
            {
                reason = "invalid_stx";
                return false;
            }
            if (characters[9] != '\x03')
            {
                reason = "invalid_etx";
                return false;
            }
            if (characters[1] != '1')
            {
                reason = "unsupported_header";
                return false;
            }
            if (characters[2] != '+' && characters[2] != '-')
            {
                reason = "invalid_sign";
                return false;
            }

            // Six-character magnitude field: left padding spaces are allowed.
            // Require digits, with at most one decimal point between digits.
            int start = 3;
            const int end = 9;
            while (start < end && characters[start] == ' ') start++;
            if (start == end)
            {
                reason = "empty_magnitude";
                return false;
            }
            bool hasDecimalPoint = false;
            for (int index = start; index < end; index++)
            {
                char character = characters[index];
                if (character == '.' && !hasDecimalPoint && index > start && index < end - 1)
                {
                    hasDecimalPoint = true;
                }
                else if (character < '0' || character > '9')
                {
                    reason = "invalid_magnitude";
                    return false;
                }
            }

            decimal value;
            if (!decimal.TryParse(
                    new string(characters, start, end - start),
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out value))
            {
                reason = "invalid_magnitude";
                return false;
            }

            double magnitude = (double)value;
            loadKgf = characters[2] == '-' ? -magnitude : magnitude;
            return true;
        }
    }
}
