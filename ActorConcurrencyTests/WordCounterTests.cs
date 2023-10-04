using ActorSort;
using FluentAssertions;
using System.Text;

namespace ActorSortTests;

public class WordCounterTests
{
    [Fact]
    public void ToLower_WillCreateALowerCasedList()
    {
        var words = new List<string> { "AaA", "bBb" };

        var lowerCased = words.ToLower().ToList();

        lowerCased.Count.Should().Be(words.Count);
        lowerCased[0].Should().Be("aaa");
        lowerCased[1].Should().Be("bbb");
    }

    [Fact]
    public void MakeUnique_WillDropDuplicates()
    {
        var words = new List<string> { "a", "b", "a" };

        var unique = words.Unique().ToList();

        unique.Count.Should().Be(2);
        unique[0].Should().Be("a");
        unique[1].Should().Be("b");
    }

    [Fact]
    public void CanConbine_UniqueAndLower()
    {
        var words = new List<string> { "AaA", "bBb", "aaa", "bbb" };

        var uniqueLower = words.ToLower().Unique().ToList();

        uniqueLower.Count.Should().Be(2);
        uniqueLower[0].Should().Be("aaa");
        uniqueLower[1].Should().Be("bbb");
    }

    [Fact]
    public void UniqueChars_WhenPairIsEmpty_ResultsToZero()
    {
        var pair = ("", "");

        int result = pair.UniqueChars();

        result.Should().Be(0);
    }

    [Fact]
    public void UniqueChars_WhenPairHasDistinctWords_ReturnsTheNumberOfUniqeChars()
    {
        var pair = ("a", "b");

        int result = pair.UniqueChars();

        result.Should().Be(2);
    }

    [Fact]
    public void UniqueChars_WhenPairHasOverlap_ReturnsTheNumberOfUniqeChars()
    {
        var pair = ("ab", "bac");

        int result = pair.UniqueChars();

        result.Should().Be(3);
    }

    [Fact]
    public void UniqueChars_WhenKnownExample_ReturnsTheNumberOfUniqeChars()
    {
        var pair = ("fine", "captain");

        int result = pair.UniqueChars();

        result.Should().Be(8);
    }

    [Fact]
    public static void WordStream_WillGiveOneWordAtATime()
    {
        var data = @"hello world,
new line  
and a second.";

        using var stream = new MemoryStream();
        stream.Write(Encoding.ASCII.GetBytes(data));
        stream.Position = 0;

        var words = stream.AsWords(Encoding.ASCII).ToList();

        words.Count.Should().Be(7);
    }

    [Fact]
    public static void CanCombineAll()
    {
        var data = @"Hello world
Hello World
Hello World";

        using var stream = new MemoryStream();
        stream.Write(Encoding.ASCII.GetBytes(data));
        stream.Position = 0;

        var words = stream.AsWords(Encoding.ASCII).ToLower().Unique().ToList();

        words.Count.Should().Be(2);

        var chars = (words[0], words[1]).UniqueChars();

        chars.Should().Be(7);
    }
}