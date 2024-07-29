
namespace NexusAzureFunctions.Helpers;

// Convert numbers to words
public static class NumbersToWords
{
    private static readonly string[] Ones =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
    ];

    private static readonly string[] Thousands = ["", "thousand", "million", "billion"];

    public static string ConvertToWords(int number, bool useProperCase = false)
    {
        var words = ConvertToWords(number);

        return useProperCase ? char.ToUpper(words[0]) + words[1..] : words;
    }

    private static string ConvertToWords(int number)
    {
        if (number == 0)
        {
            return "zero";
        }
        if (number < 0)
        {
            return "negative " + ConvertToWords(-number);
        }

        var words = string.Empty;
        var i = 0;

        while (number > 0)
        {
            if (number % 1000 != 0)
            {
                words = $"{ConvertChunkToWords(number % 1000)} {Thousands[i]} {words}".Trim();
            }

            number /= 1000;
            i++;
        }

        return words.Trim();
    }

    private static string ConvertChunkToWords(int number)
    {
        var words = string.Empty;

        if (number >= 100)
        {
            words += $"{Ones[number / 100]} hundred ";
            number %= 100;
        }

        if (number >= 20)
        {
            words += $"{Tens[number / 10]} ";
            number %= 10;
        }

        if (number > 0)
        {
            words += $"{Ones[number]}";
        }

        return words.Trim();
    }
}