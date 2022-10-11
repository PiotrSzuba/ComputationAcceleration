using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CommunityToolkit.HighPerformance;

namespace Aop.Client.Benchmarks;

public class ListsBenchamarks
{
    private static Random rnd = new();
    private static int Size = 10000;
    ImmutableArray<ImmutableArray<int>> ImmArray = CreateImmArr();
    List<List<int>> list = Create2dList();
    int[][] array = Create2dArr();

    private static int[][] Create2dArr()
    {
        var matrix = new List<List<int>>();
        for (int i = 0; i < Size; i++)
        {
            var line = new List<int>();
            for (int j = 0; j < Size; j++)
            {
                line.Add(rnd.Next(int.MaxValue));
            }
            matrix.Add(line);
        }

        return matrix.Select(x => x.ToArray()).ToArray();
    }
    private static List<List<int>> Create2dList()
    {
        var matrix = new List<List<int>>();
        for (int i = 0; i < Size; i++)
        {
            var line = new List<int>();
            for (int j = 0; j < Size; j++)
            {
                line.Add(rnd.Next(int.MaxValue));
            }
            matrix.Add(line);
        }
        return matrix;
    }
    private static IReadOnlyCollection<IReadOnlyCollection<int>> CreateReadOnly()
    {
        return Create2dList().Select(x => x.AsReadOnly()).ToList().AsReadOnly();
    }

    private static ImmutableArray<ImmutableArray<int>> CreateImmArr()
    {
        var builder1d = ImmutableArray.CreateBuilder<int>();
        var builder2d = ImmutableArray.CreateBuilder<ImmutableArray<int>>();

        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                builder1d.Add(rnd.Next(int.MaxValue));
            }
            builder2d.Add(builder1d.ToImmutableArray());
            builder1d.Clear();
        }

        return builder2d.ToImmutableArray();
    }

    private static ImmutableList<ImmutableList<int>> CreateImmList()
    {
        var builder1d = ImmutableList.CreateBuilder<int>();
        var builder2d = ImmutableList.CreateBuilder<ImmutableList<int>>();

        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                builder1d.Add(rnd.Next(int.MaxValue));
            }
            builder2d.Add(builder1d.ToImmutableList());
            builder1d.Clear();
        }

        return builder2d.ToImmutableList();
    }

    private static Int64 SomeMath(int elem)
    {
        return elem + elem * 2 / 4 * 2;
    }

    [Benchmark]
    public void Array()
    {
        foreach (var line in array)
        {
            foreach (var elem in line)
            {
                SomeMath(elem);
            }
        }
    }

    [Benchmark]
    public void List()
    {
        foreach (var line in list)
        {
            foreach (var elem in line)
            {
                SomeMath(elem);
            }
        }
    }

    public void ReadonlyCollection()
    {
        IReadOnlyCollection<IReadOnlyCollection<int>> readOnlyCol = CreateReadOnly();

        foreach (var line in readOnlyCol)
        {
            foreach (var elem in line)
            {
                SomeMath(elem);
            }
        }
    }

    [Benchmark]
    public void Immutable_Array()
    {
        foreach (var line in ImmArray)
        {
            foreach (var elem in line)
            {
                SomeMath(elem);
            }
        }
    }

    public void Immutable_List()
    {
        ImmutableList<ImmutableList<int>> ImmList = CreateImmList();

        foreach (var line in ImmList)
        {
            foreach (var elem in line)
            {
                SomeMath(elem);
            }
        }
    }
}
