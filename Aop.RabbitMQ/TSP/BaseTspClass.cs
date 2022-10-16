using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aop.RabbitMQ.TSP;

public enum TspAlgoritms
{
    Bruteforce,
    Genetic,
}

public class TspInput
{
    public Guid TaskId { get; set; }
    public TspAlgoritms Algoritm { get; set; }
    public ImmutableArray<ImmutableArray<int>> Matrix { get; set; }
    public TspBruteforceInput? TspBruteforceInput { get; set; }
    public TspGeneticInput? TspGeneticInput { get; set; }
}

public class TspBruteforceInput 
{
    public List<int> PermutationIndexes { get; set; } = new();
}

public class TspGeneticInput
{
    public List<int> Population { get; set; } = new();
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

    public static TspOutput Error => new (new(), -1);
}

public abstract class BaseTspClass
{
    protected ImmutableArray<ImmutableArray<int>> Matrix { get; set; }
    public int Cost { get; set; }
    public List<int> BestPath { get; set; }
    public TspBruteforceInput? TspBruteforceInput { get; set; }
    public TspGeneticInput? TspGeneticInput { get; set; }
    public const int StartCityIndex = 0;

    public BaseTspClass(TspInput tspInput)
    {
        Matrix = tspInput.Matrix;
        Cost = int.MaxValue;
        BestPath = new();
        TspBruteforceInput = tspInput.TspBruteforceInput;
        TspGeneticInput = tspInput.TspGeneticInput;
    }

    public abstract TspOutput Run();
}
