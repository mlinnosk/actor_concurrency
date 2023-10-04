namespace ActorSort;

public static class SorterFuncs
{
    public static List<int> Sort(IReadOnlyList<int> list)
    {
        var copy = new List<int>(list);
        copy.Sort();
        return copy;
    }

    public static IReadOnlyList<int> Merge(IReadOnlyList<int> sortedLhs, IReadOnlyList<int> sortedRhs)
    {
        var merged = new List<int>(sortedLhs.Count + sortedRhs.Count);

        int lhsIndex = 0;
        int rhsIndex = 0;
        while (true)
        {
            if (lhsIndex < sortedLhs.Count)
            {
                if (rhsIndex >= sortedRhs.Count)
                {
                    AddAllFrom(sortedLhs, lhsIndex, merged);
                    break;
                }
                (lhsIndex, rhsIndex) = AddBigger(sortedLhs, lhsIndex, sortedRhs, rhsIndex, merged);
            }
            else if (rhsIndex < sortedRhs.Count)
            {
                if (lhsIndex >= sortedLhs.Count)
                {
                    AddAllFrom(sortedRhs, rhsIndex, merged);
                    break;
                }
                (lhsIndex, rhsIndex) = AddBigger(sortedLhs, lhsIndex, sortedRhs, rhsIndex, merged);
            }
            else
            {
                break;
            }
        }

        return merged;


        static int AddAllFrom(IReadOnlyList<int> src, int startIndex, List<int> dst)
        {
            while (startIndex < src.Count)
            {
                dst.Add(src[startIndex]);
                ++startIndex;
            }
            return startIndex;
        }

        static (int lhsIndex, int rhsIndex) AddBigger(
            IReadOnlyList<int> lhs, int lhsIndex,
            IReadOnlyList<int> rhs, int rhsIndex,
            List<int> dst)
        {
            if (lhs[lhsIndex] <= rhs[rhsIndex])
            {
                dst.Add(lhs[lhsIndex]);
                return (lhsIndex + 1, rhsIndex);
            }

            dst.Add(rhs[rhsIndex]);
            return (lhsIndex, rhsIndex + 1);
        }
    }

    public static (List<int>, List<int>) Split(IReadOnlyList<int> data)
    {
        int midPoint = data.Count / 2;

        var lhs = new List<int>(midPoint);
        for (int i = 0; i < midPoint; ++i)
        {
            lhs.Add(data[i]);
        }
        var rhs = new List<int>(data.Count - midPoint);
        for (int i = midPoint; i < data.Count; ++i)
        {
            rhs.Add(data[i]);
        }

        return (lhs, rhs);
    }

    public static bool IsSorted(IReadOnlyList<int> data)
    {
        int? prev = null;
        foreach (int val in data)
        {
            if (prev is not null)
            {
                if (prev > val)
                {
                    return false;
                }
            }
            prev = val;
        }

        return true;
    }
}
