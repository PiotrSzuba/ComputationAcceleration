using Aop.RabbitMQ.Extensions;
using Aop.RabbitMQ.Permutations;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Aop.RabbitMQ.TSP;

public class Bruteforce
{
    private static int StartCityIndex = 0;

    public static TspOutput Run(TspInput tspInput)
    {
        var bestTspOutput = new TspOutput(new(), int.MaxValue);
        var citiesIndexes = GetCities(tspInput);

        var permutationGenerator = new PermutationMixOuelletSaniSinghHuttunen(
            citiesIndexes, 
            tspInput.TspBruteforceInput.FirstPermutationIndex, 
            tspInput.TspBruteforceInput.LastPermutationIndex);

        permutationGenerator.ExecuteForEachPermutation((permutation) =>
        {
            var output = CalculateCostForCityOrder(tspInput.Matrix, permutation);
            if (output.Cost < bestTspOutput.Cost)
            {
                bestTspOutput = output;
            }
        });

        return bestTspOutput;
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

    private static int[] GetCities(TspInput tspInput)
    {
        var citiesIndexes = new int[tspInput.Matrix.Length - 1];

        for (int idx = 0, val = 0; idx < citiesIndexes.Length; val++)
        {
            if (val == StartCityIndex) continue;
            citiesIndexes[idx] = val;
            idx++;
        }

        return citiesIndexes;
    }
}
