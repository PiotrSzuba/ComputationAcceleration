using Aop.RabbitMQ.Extensions;
using System.Collections.Immutable;

namespace Aop.RabbitMQ.TSP;

public class Bruteforce : BaseTspClass
{
    public Bruteforce(TspInput tspInput) : base(tspInput) 
    {
        if (tspInput.TspBruteforceInput is null)
            throw new ArgumentNullException();
    }

    public override TspOutput Run()
    {
        var cityIndexesOrders = GetAllCitiesPermutations(Matrix.Length);

        foreach (var cityIndexesOrder in cityIndexesOrders)
        {
            CalculateCostForCityOrder(cityIndexesOrder.ToArray());
        }

        return new (BestPath, Cost);
    }

    public static TspOutput RunSinglePermutation(ImmutableArray<ImmutableArray<int>> matrix, IList<int> permutation)
    {
        return CalculateCostForCityOrder(matrix, permutation.ToArray());
    }

    public TspOutput RunSinglePermutation()
    {
        if (TspBruteforceInput is null)
            return TspOutput.Error;

        CalculateCostForCityOrder(TspBruteforceInput.Permutation.ToArray());

        return new(BestPath, Cost);
    }

    public static List<List<int>> GetAllCitiesPermutations(int citiesCount)
    {
        var citiesIndexes = new int[citiesCount - 1];

        for (int idx = 0, val = 0; idx < citiesIndexes.Length; val++)
        {
            if (val == StartCityIndex) continue;
            citiesIndexes[idx] = val;
            idx++;
        }
     
        var cityIndexesOrders = citiesIndexes.GetPermutations();

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
