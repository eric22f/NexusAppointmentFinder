using System;
using System.Text;

namespace NexusAzureFunctions.Helpers
{
    public static class Base36Converter
    {
        private const string Base36Chars = "0123456789abcdefghijklmnopqrstuvwxyz";

        // Convert a number to a base36 string
        public static string ToBase36(long number)
        {
            if (number < 0)
                throw new ArgumentException("Number must be non-negative.", nameof(number));

            if (number == 0)
                return "0";

            var sb = new StringBuilder();
            while (number > 0)
            {
                var remainder = number % 36;
                sb.Insert(0, Base36Chars[(int)remainder]);
                number /= 36;
            }

            return sb.ToString();
        }

        // Convert a base36 string to a number
        public static string FromBase36(string base36)
        {
            if (string.IsNullOrWhiteSpace(base36))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(base36));

            base36 = base36.ToLower();
            long result = 0;
            for (int i = 0; i < base36.Length; i++)
            {
                var c = base36[i];
                var value = Base36Chars.IndexOf(c);
                if (value == -1)
                    throw new ArgumentException($"Invalid character '{c}' in base36 string.", nameof(base36));

                result += value * (long)Math.Pow(36, base36.Length - i - 1);
            }

            return result.ToString();
        }
    }
}