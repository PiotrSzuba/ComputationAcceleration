using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aop.Client.TSP;

public class TspInput
{
    public ImmutableArray<ImmutableArray<int>> Matrix { get; set; }

    public TspInput(ImmutableArray<ImmutableArray<int>> imMatrix)
    {
        Matrix = imMatrix;
    }
}

public class TspOutput
{
    public List<int> BestPath { get; set; } = new();
    public int Cost { get; set; }

    public TspOutput(List<int> bestPath, int cost)
    {
        BestPath = bestPath;
        Cost = cost;
    }
}

public abstract class BaseTspClass
{
    protected ImmutableArray<ImmutableArray<int>> Matrix { get; set; }
    public int Cost { get; set; }
    public List<int> BestPath { get; set; }
    public const int StartCityIndex = 0;

    public BaseTspClass(TspInput tspInput)
    {
        Matrix = tspInput.Matrix;
        Cost = int.MaxValue;
        BestPath = new();
    }

    public abstract TspOutput Run();
}
