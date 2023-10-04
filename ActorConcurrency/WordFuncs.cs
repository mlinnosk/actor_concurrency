using System.Runtime.InteropServices;
using System.Text;

namespace ActorSort;

public static class WordFuncs
{
    public static IEnumerable<string> ToLower(this IEnumerable<string> words)
    {
        foreach (var word in words)
        {
            yield return word.ToLower();
        }
    }

    public static IEnumerable<string> Unique(this IEnumerable<string> words)
    {
        var seen = new HashSet<string>();
        foreach (var word in words)
        {
            if (!seen.Contains(word))
            {
                seen.Add(word);
                yield return word;
            }
        }
    }

    public static int UniqueChars(this (string lhs, string rhs) wordPair)
    {
        var seen = new List<char>(wordPair.lhs.Length + wordPair.rhs.Length);
        //var seen = new HashSet<char>();

        foreach (char c in wordPair.lhs.AsSpan())
        {
            AddUnique(c, seen);
            //seen.Add(c);
        }
        foreach (char c in wordPair.rhs.AsSpan())
        {
            AddUnique(c, seen);
            //seen.Add(c);
        }

        return seen.Count;

        static void AddUnique(char c, List<char> l)
        {
            if (!l.Contains(c))
            {
                l.Add(c);
            }
        }
    }

    public record struct ResultPair(string Lhs, string Rhs)
    {
        public override readonly string ToString() => $"[{Lhs}:{Rhs}]";
    }

    public static (int, IReadOnlyList<ResultPair>) CountMaxUniqueChars(string reference, IReadOnlyList<string> words)
    {
        int maxCount = 0;
        var currentResult = new List<ResultPair>(1);
        foreach (var word in words)
        {
            int count = (reference, word).UniqueChars();
            if (count < maxCount)
            {
                continue;
            }

            if (count > maxCount)
            {
                maxCount = count;
                currentResult.Clear();
            }
            currentResult.Add(new ResultPair(reference, word));
        }

        return (maxCount, currentResult);
    }

    public static IEnumerable<string> AsWords(this Stream stream, Encoding encoding)
    {
        var chars = new List<byte>();

        while (stream.CanRead)
        {
            int c = stream.ReadByte();
            if (c == -1)
            {
                break;
            }

            if (char.IsWhiteSpace((char)c))
            {
                if (chars.Count > 0)
                {
                    yield return encoding.GetString(CollectionsMarshal.AsSpan(chars));
                    chars.Clear();
                }
                // else eat ws
            }
            else
            {
                chars.Add((byte)c);
            }
        }

        if (chars.Count > 0)
        {
            yield return encoding.GetString(CollectionsMarshal.AsSpan(chars));
        }
    }
}
