using Aop.Client.Extensions;

namespace Aop.Client.TSP;

public class Bruteforce : BaseTspClass
{
    public Bruteforce(TspInput tspInput) : base(tspInput) {}

    public override TspOutput Run()
    {
        var citiesIndexes = new int[Matrix.Length];

        for (int i = 0; i < Matrix.Length; i++)
        {
            if (i == StartCityIndex) continue;
            citiesIndexes[i] = i;
        }

        var cityIndexesOrders = citiesIndexes.GetPermutations();

        if (cityIndexesOrders == null)
            throw new Exception($"{nameof(cityIndexesOrders)} is somehow null");

        foreach (var cityIndexesOrder in cityIndexesOrders)
        {
            CalculateCostForCityOrder(cityIndexesOrder.ToArray());
        }

        return new (BestPath, Cost);
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
