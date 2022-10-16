using Aop.RabbitMQ.Extensions;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Aop.RabbitMQ.TSP;

public class Bruteforce : BaseTspClass
{
    public Bruteforce(TspInput tspInput) : base(tspInput) 
    {
        //if (tspInput.TspBruteforceInput is null)
            //throw new ArgumentNullException();
    }

    public override TspOutput Run()
    {
        var sw = new Stopwatch();
        sw.Start();
        var cityIndexesOrders = GetAllCitiesPermutations(Matrix.Length);
        sw.Stop();

        Console.WriteLine($" Permutations took {sw.ElapsedMilliseconds} ms");

        foreach (var cityIndexesOrder in cityIndexesOrders)
        {
            CalculateCostForCityOrder(cityIndexesOrder.ToArray());
        }

        return new (BestPath, Cost);
    }

    public static TspOutput RunSinglePermutation(ImmutableArray<ImmutableArray<int>> matrix, List<int> permutation)
    {
        return CalculateCostForCityOrder(matrix, permutation.ToArray());
    }

    public static TspOutput RunPermutations(ImmutableArray<ImmutableArray<int>> matrix, List<List<int>> permutations, List<int> permutationIndexes)
    {
        var bestTspOutput = new TspOutput(new(), int.MaxValue);

        for (int i = 0; i < permutationIndexes.Count; i++)
        {
            var output = CalculateCostForCityOrder(matrix, permutations[permutationIndexes[i]].ToArray());
            if (output.Cost >= bestTspOutput.Cost) continue;
            bestTspOutput = output;
        }

        return bestTspOutput;
    }

    public static List<List<int>> GetAllCitiesPermutations(int citiesCount)
    {
        if (citiesCount == 0)
            return new();

        var citiesIndexes = new int[citiesCount - 1];

        for (int idx = 0, val = 0; idx < citiesIndexes.Length; val++)
        {
            if (val == StartCityIndex) continue;
            citiesIndexes[idx] = val;
            idx++;
        }
     
        var cityIndexesOrders = citiesIndexes.GetPermutationFast();

        if (cityIndexesOrders == null)
            throw new Exception($"{nameof(cityIndexesOrders)} is somehow null");

        return cityIndexesOrders
            .Select(x => x.ToList())
            .ToList();
    }

    private static TspOutput CalculateCostForCityOrder(ImmutableArray<ImmutableArray<int>> matrix, int[] cityOrder)
    {
        var path = new List<int>
        {
            StartCityIndex
        };

        int cost = 0;
        int currentCityIndex = StartCityIndex;

        for (int i = 0; i < cityOrder.Length; i++)
        {
            cost += matrix[currentCityIndex][cityOrder[i]];
            currentCityIndex = cityOrder[i];
            path.Add(cityOrder[i]);
        }

        cost += matrix[currentCityIndex][StartCityIndex];

        return new TspOutput(path, cost);
    }

    private void CalculateCostForCityOrder(int[] cityOrder)
    {
        var path = new List<int>
        {
            StartCityIndex
        };
        int currentCost = 0;
        int currentCityIndex = StartCityIndex;

        for (int i = 0; i < cityOrder.Length; i++)
        {
            currentCost += Matrix[currentCityIndex][cityOrder[i]];
            currentCityIndex = cityOrder[i];
            path.Add(cityOrder[i]);
        }

        currentCost += Matrix[currentCityIndex][StartCityIndex];

        if (Cost <= currentCost) return;

        Cost = currentCost;
        BestPath = path;
    }
}
