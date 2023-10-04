using ActorSort;
using FluentAssertions;

namespace ActorSortTest;

public class SorterFunctionsTest
{
    [Fact]
    public void Sort_WithData_WillReturnNewList()
    {
        var orig = new List<int> { 1, 2, 3 };

        var sorted = SorterFuncs.Sort(orig);

        Assert.False(ReferenceEquals(orig, sorted));
    }

    [Fact]
    public void Sort_WithData_WillActuallySort()
    {
        var orig = new List<int> { 3, 2, 1};

        var sorted = SorterFuncs.Sort(orig);

        sorted[0].Should().Be(1);
        sorted[1].Should().Be(2);
        sorted[2].Should().Be(3);
    }

    [Fact]
    public void Split_WhenEvenNumberOfElements_WillSplitFromMiddle()
    {
        var orig = new List<int> { 4, 3, 2, 1 };

        var (lhs, rhs) = SorterFuncs.Split(orig);

        lhs.Count.Should().Be(2);
        rhs.Count.Should().Be(2);
    }

    [Fact]
    public void Split_WhenEvenNumberOfElements_WillContainProperElems()
    {
        var orig = new List<int> { 4, 3, 2, 1 };

        var (lhs, rhs) = SorterFuncs.Split(orig);

        lhs[0].Should().Be(4);
        lhs[1].Should().Be(3);
        rhs[0].Should().Be(2);
        rhs[1].Should().Be(1);
    }

    [Fact]
    public void Split_WhenOddNumberOfElements_WillSplitFromMiddle()
    {
        var orig = new List<int> { 4, 3, 2 };

        var (lhs, rhs) = SorterFuncs.Split(orig);

        lhs.Count.Should().Be(1);
        rhs.Count.Should().Be(2);
    }

    [Fact]
    public void Split_WhenOddNumberOfElements_WillContainProperElems()
    {
        var orig = new List<int> { 4, 3, 2 };

        var (lhs, rhs) = SorterFuncs.Split(orig);

        lhs[0].Should().Be(4);
        rhs[0].Should().Be(3);
        rhs[1].Should().Be(2);
    }

    [Fact]
    public void Merge_WhenThereAreElements_WillMerge()
    {
        var lhs = new List<int> { 1, 3 };
        var rhs = new List<int> { 2, 4 };

        var merged = SorterFuncs.Merge(lhs, rhs);

        merged[0].Should().Be(1);
        merged[1].Should().Be(2);
        merged[2].Should().Be(3);
        merged[3].Should().Be(4);
    }

    [Fact]
    public void Merge_WhenLhsIsEmpty_WillMerge()
    {
        var lhs = new List<int>();
        var rhs = new List<int> { 2, 4 };

        var merged = SorterFuncs.Merge(lhs, rhs);

        merged[0].Should().Be(2);
        merged[1].Should().Be(4);
    }

    [Fact]
    public void Merge_WhenRhsIsEmpty_WillMerge()
    {
        var lhs = new List<int> { 1, 2 };
        var rhs = new List<int>();

        var merged = SorterFuncs.Merge(lhs, rhs);

        merged[0].Should().Be(1);
        merged[1].Should().Be(2);
    }

    [Fact]
    public void Merge_WhenDifferentNumberOfElemsInLhs_WillMerge()
    {
        var lhs = new List<int> { 1, 3, 5 };
        var rhs = new List<int> { 2, 4 };

        var merged = SorterFuncs.Merge(lhs, rhs);

        merged[0].Should().Be(1);
        merged[1].Should().Be(2);
        merged[2].Should().Be(3);
        merged[3].Should().Be(4);
        merged[4].Should().Be(5);
    }

    [Fact]
    public void Merge_WhenDifferentNumberOfElemsInRhs_WillMerge()
    {
        var lhs = new List<int> { 2, 4 };
        var rhs = new List<int> { 1, 3, 5 };

        var merged = SorterFuncs.Merge(lhs, rhs);

        merged[0].Should().Be(1);
        merged[1].Should().Be(2);
        merged[2].Should().Be(3);
        merged[3].Should().Be(4);
        merged[4].Should().Be(5);
    }

    [Fact]
    public void IsSorted_WhenDataIsSorted_RetursTrue()
    {
        var data = new List<int> { 1, 2, 3 };

        var isSorted = SorterFuncs.IsSorted(data);

        isSorted.Should().BeTrue();
    }

    [Fact]
    public void IsSorted_WhenNoData_RetursTrue()
    {
        var data = new List<int>();

        var isSorted = SorterFuncs.IsSorted(data);

        isSorted.Should().BeTrue();
    }

    [Fact]
    public void IsSorted_WhenDataIsNotSorted_RetursFalse()
    {
        var data = new List<int> { 1, 3, 2 };

        var isSorted = SorterFuncs.IsSorted(data);

        isSorted.Should().BeFalse();
    }
}