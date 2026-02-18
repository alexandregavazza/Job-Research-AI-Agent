using JobResearchAgent.Services.FileManipulator;

namespace JobResearchAgent.Tests;

public class FileSanitizerTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Sanitize_EmptyOrWhitespace_ReturnsEmptyString(string input, string expected)
    {
        var sanitizer = new FileSanitizer();

        var result = sanitizer.Sanitize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_RemovesSpaces()
    {
        var sanitizer = new FileSanitizer();

        var result = sanitizer.Sanitize("My File Name");

        Assert.Equal("MyFileName", result);
    }

    [Fact]
    public void Sanitize_ReplacesInvalidCharacters()
    {
        var sanitizer = new FileSanitizer();

        var result = sanitizer.Sanitize("bad:name?.txt");

        Assert.Equal("bad_name_.txt", result);
    }

    [Fact]
    public void Sanitize_MixedSpacesAndInvalidCharacters()
    {
        var sanitizer = new FileSanitizer();

        var result = sanitizer.Sanitize("report 2024:final?.pdf");

        Assert.Equal("report2024_final_.pdf", result);
    }
}
