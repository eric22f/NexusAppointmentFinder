

using NexusAzureFunctions.Helpers;

namespace NexusAzureFunctionsTests.HelpersTests;

public class NumbersToWordsTests
{
    [Fact]
    public void ConvertToWords_ArrayOfNumbers_ReturnsCorrectResponse()
    {
        // Arrange
        string[] expected = 
        [
            "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen", "Twenty",
            "Twenty one", "Twenty two", "Twenty three", "Twenty four", "Twenty five", "Twenty six", "Twenty seven", "Twenty eight", "Twenty nine", "Thirty",
            "Thirty one", "Thirty two", "Thirty three", "Thirty four", "Thirty five", "Thirty six", "Thirty seven", "Thirty eight", "Thirty nine", "Forty"
        ];

        for (int i = 0; i < expected.Length; i++)
        {
            // Act
            string result = NumbersToWords.ConvertToWords(i, true);
            string resultLower = NumbersToWords.ConvertToWords(i);

            // Assert
            Assert.Equal(expected[i], result);
            Assert.Equal(expected[i].ToLower(), resultLower);
        }
    }

    [Fact]
    public void ConvertToWords_NegativeNumber_ReturnsCorrectResponse()
    {
        // Arrange
        int number = -1;

        // Act
        string result = NumbersToWords.ConvertToWords(number);
        string resultUpper = NumbersToWords.ConvertToWords(number, true);

        // Assert
        Assert.Equal("negative one", result);
        Assert.Equal("Negative one", resultUpper);
    }

    [Fact]
    public void ConvertToWords_Number_ReturnsCorrectResponse()
    {
        // Arrange
        int number = 1234567890;

        // Act
        string result = NumbersToWords.ConvertToWords(number);
        string resultUpper = NumbersToWords.ConvertToWords(number, true);

        // Assert
        Assert.Equal("one billion two hundred thirty four million five hundred sixty seven thousand eight hundred ninety", result);
        Assert.Equal("One billion two hundred thirty four million five hundred sixty seven thousand eight hundred ninety", resultUpper);
    }
}