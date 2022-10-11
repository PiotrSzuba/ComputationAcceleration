﻿using Aop.Client.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aop.Client.TSP;

public class Genetic : BaseTspClass
{
    private readonly double _mutationProbability = 0.05d;
    private readonly double _crossoverProbability = 0.8d;
    private int _populationSize { get; set; }
    private int _noImprove = 0;
    private List<(int cost, List<int> path)> _costsAndPaths { get; set; } = new();
    private List<List<int>> _population = new();
    private readonly Random _rnd = new();

    public Genetic(TspInput tspInput) : base(tspInput) 
    {
        _populationSize = Matrix.Length * 50;
    }

    public override TspOutput Run()
    {
        GeneratePopulation();

        while (_noImprove <= Matrix.Length / 2)
        {
            Selection();
            Crossover();
            Mutation();
            SaveBestPopulation();
        }

        return new(BestPath, Cost);
    }

    private void GeneratePopulation()
    {
        var individual = new List<int>();

        for (int i = 0; i < Matrix.Length; i++)
        {
            individual.Add(i);
        }

        for (int i = 0; i < _populationSize; i++)
        {
            individual = individual.AsRandom().ToList();
            _population.Add(individual);
            _costsAndPaths.Add((CalculateCost(_population[i]), _population[i]));
        }
    }

    private void Selection()
    {
        _costsAndPaths = _costsAndPaths.OrderBy(costAndPath => costAndPath.cost).ToList();
        if (_costsAndPaths.Count - _populationSize != 0)
        {
            _costsAndPaths.RemoveRange(_populationSize, _costsAndPaths.Count - _populationSize);
            _population.RemoveRange(_populationSize, _population.Count - _populationSize);
        }

        for (int i = 0; i < _populationSize; i++)
        {
            _population[i] = _costsAndPaths[i].path;
        }
    }

    private void Crossover()
    {
        int size = (int)(_population.Count * _crossoverProbability);
        for (int i = 0; i < size; i++)
        {
            int second;

            do
            {
                second = _rnd.Next(_population.Count);
            } while (i == second && i > second);

            OrderedCrossover(_population[i], _population[second]);
        }
    }

    private void OrderedCrossover(List<int> parent1, List<int> parent2)
    {
        var child1 = new int[Matrix.Length];
        var child2 = new int[Matrix.Length];

        var parent1Vals = new bool[parent1.Count];
        var parent2Vals = new bool[parent2.Count];

        int start = -1;
        int end = -1;
        int size = parent1.Count;

        GetTwoRandomPoints(ref start, ref end);

        var tempChild1 = new int[end - start];
        var tempChild2 = new int[end - start];

        Array.Copy(parent1.ToArray(), start, tempChild1, 0, end - start);
        Array.Copy(parent2.ToArray(), start, tempChild2, 0, end - start);

        for (int i = 0; i < end - start; i++)
        {
            parent1Vals[tempChild2[i]] = true;
            parent2Vals[tempChild1[i]] = true;
        }

        for (int i = 0, j = 0, k = 0, n = 0; i < size; i++)
        {
            if (i >= start && i < end)
            {
                child1[i] = tempChild1[j];
                child2[i] = tempChild2[j];
                j++;
            }
            else
            {
                while (parent2Vals[parent2[k]] == true)
                {
                    k++;
                }
                while (parent1Vals[parent1[n]] == true)
                {
                    n++;
                }
                child1[i] = parent2[k];
                child2[i] = parent1[n];
                k++;
                n++;
            }
        }

        _population.Add(child1.ToList());
        _population.Add(child2.ToList());
    }

    private void Mutation()
    {
        for (var i = 0; i < _population.Count; i++)
        {
            double probability = _rnd.NextDouble();

            if (probability < _mutationProbability)
            {
                int start = -1;
                int end = -1;
                GetTwoRandomPoints(ref start, ref end);
                _population[i].Reverse(start, end - start);
            }
        }
    }

    public void SaveBestPopulation()
    {
        var bestPopulation = new List<int>();
        int currentCost = int.MaxValue;
        int tempCost = 0;

        for (int i = 0; i < _population.Count; i++)
        {
            tempCost = CalculateCost(_population[i]);
            _costsAndPaths.Add((tempCost, _population[i]));

            if (currentCost <= tempCost) continue;

            currentCost = tempCost;
            bestPopulation = new(_population[i]);
        }

        if (Cost <= currentCost)
        {
            _noImprove++;
            return;
        }

        BestPath = bestPopulation;
        BestPath.Add(BestPath[0]);
        // Console.WriteLine($"Old cost: {Cost} New cost: {currentCost}");
        Cost = currentCost;
        _noImprove = 0;
    }

    private int CalculateCost(List<int> path)
    {
        int cost = 0;
        for (int i = 0; i < path.Count - 1; ++i)
        {
            cost += Matrix[path[i]][path[i + 1]];
        }

        cost += Matrix[path[Matrix.Length - 1]][path[0]];

        return cost;
    }

    private void GetTwoRandomPoints(ref int start, ref int end)
    {
        do
        {
            start = _rnd.Next(Matrix.Length);
            end = _rnd.Next(Matrix.Length);
        } while (start == end);

        if (end < start) 
            (start, end) = (end, start);
    }
}
