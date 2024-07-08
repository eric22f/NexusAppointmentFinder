
using System.Text;

namespace Functions.Helpers
{
    public static class Tracer
    {
        private static readonly string _traceId;
        private static readonly long _baseTicks = new DateTime(2024,7,1).Ticks;

        static Tracer()
        {
            var ticks = DateTime.UtcNow.Ticks - _baseTicks;
            string base36 = Base36Converter.ToBase36(ticks);
            _traceId = InsertDashes(base36, 5);

        }

        // Returns unique Id used for logging
        public static string Id { get { return _traceId; } }

        public static DateTime GetDateTimeFromId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));

            long ticks = long.Parse(Base36Converter.FromBase36(id.Replace("-", "")));
            return new DateTime(ticks + _baseTicks);
        }

        // Function to insert dashes after every n characters in a string
        private static string InsertDashes(string input, int n)
        {
            StringBuilder sb = new ();
            for (int i = 0; i < input.Length; i++)
            {
                sb.Append(input[i]);
                if ((i + 1) % n == 0 && i != input.Length - 1)
                {
                    sb.Append('-');
                }
            }
            return sb.ToString();
        }
    }
}