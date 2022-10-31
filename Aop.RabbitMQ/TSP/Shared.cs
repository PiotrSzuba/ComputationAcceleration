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
    public TspBruteforceInput TspBruteforceInput { get; set; } = new();
    public TspGeneticInput TspGeneticInput { get; set; } = new();
}

public class TspBruteforceInput
{
    public int FirstPermutationIndex { get; set; } = -1;
    public int LastPermutationIndex { get; set; } = -1;
}

public class TspGeneticInput
{
    public List<int> Individual { get; set; } = new();
    public int? MaxIterations { get; set; }
}

public class TspOutput
{
    public List<int> BestPath { get; set; } = new();
    public int Cost { get; set; }
    public int? NoImproveRuns { get; set; }

    public TspOutput() { }

    public TspOutput(List<int> bestPath, int cost)
    {
        BestPath = bestPath;
        Cost = cost;
    }

    public static TspOutput Error => new (new(), -1);
}
