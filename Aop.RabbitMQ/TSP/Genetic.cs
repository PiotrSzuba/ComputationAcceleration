using Aop.RabbitMQ.Extensions;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Aop.RabbitMQ.TSP;

public class Genetic
{
    private ImmutableArray<ImmutableArray<int>> Matrix { get; set; }
    private int Cost { get; set; } = int.MaxValue;
    private List<int> BestPath { get; set; } = new();
    private List<Individual> Population { get; set; } = new();

    private const double _mutationProbability = 0.05d;
    private const double _crossoverProbability = 0.8d;
    private int _populationSize { get; set; }
    private int _noImprove = 0;
    private readonly Random _rnd = new();
    private Func<bool> StopCondition { get; set; }

    public Genetic(TspInput tspInput)
    {
        Matrix = tspInput.Matrix;
        _populationSize = tspInput.Matrix.Length * 100;
        StopCondition = () => _noImprove <= Matrix.Length / 2;
    }

    public TspOutput Run()
    {
        var sw = new Stopwatch();
        sw.Start();

        GeneratePopulation(GenerateIndividual());

        while (StopCondition())
        {
            //sw.Restart();
            Selection();
            Crossover();
            Mutation();
            SaveBestPopulation();
            //sw.Stop();
        }

        Population.Clear();
        sw.Stop();

        return new(BestPath, Cost);
    }

    public TspOutput Run(TspInput tspInput)
    {
        if (Population.Count == 0)
        {
            var individual = tspInput.TspGeneticInput.Individual.Count == 0 
                ? GenerateIndividual() 
                : tspInput.TspGeneticInput.Individual.ToArray();
            GeneratePopulation(individual);
        }

        var sw = new Stopwatch();
        sw.Start();
    
        while (sw.ElapsedMilliseconds <= 4000)
        {
            Selection(tspInput);
            Crossover();
            Mutation();
            SaveBestPopulation();
        }
        sw.Stop();
        
        //zwracać Matrix.Count / 10 miast jako migracje

        return new TspOutput
        {
            BestPath = BestPath,
            Cost = Cost,
            NoImproveRuns = _noImprove,
        };
    }

    private int[] GenerateIndividual()
    {
        var individual = new List<int>();

        for (int i = 0; i < Matrix.Length; i++)
        {
            individual.Add(i);
        }

        return individual.AsRandom().ToArray();
    }

    private void GeneratePopulation(int[] individual)
    {
        if (individual.Length - 1 == Matrix.Length)
        {
            individual = individual.SkipLast(1).ToArray();
        }

        Population.Add(Individual.Create(individual));
        Population[^1].CalculateCost(Matrix);

        for (int i = 0; i < _populationSize - 1; i++)
        {
            individual = individual.AsRandom().ToArray();
            Population.Add(Individual.Create(individual));
            Population[^1].CalculateCost(Matrix);
        }
    }

    private void Selection()
    {
        Population = Population.OrderBy(i => i.Cost).ToList();

        if (Population.Count - _populationSize <= 0) return;

        Population.RemoveRange(_populationSize, Population.Count - _populationSize);
    }

    private void Selection(TspInput tspInput)
    {
        if (tspInput.TspGeneticInput.Individual.Count - 1 == Matrix.Length)
        {
            Population.Add(Individual.Create(tspInput.TspGeneticInput.Individual.SkipLast(1).ToArray()));
            Population[^1].CalculateCost(Matrix);
        }
        else
        {
            Population.Add(Individual.Create(tspInput.TspGeneticInput.Individual.ToArray()));
            Population[^1].CalculateCost(Matrix);
        }


        Population = Population.OrderBy(i => i.Cost).ToList();


        if (Population.Count - _populationSize <= 0) return;

        Population.RemoveRange(_populationSize, Population.Count - _populationSize);
    }

    private void Crossover()
    {
        int size = (int)(Population.Count * _crossoverProbability);
        for (int i = 0; i < size; i++)
        {
            int second;

            do
            {
                second = _rnd.Next(Population.Count);
            } while (i == second && i > second);

            OrderedCrossover(Population[i].Path, Population[second].Path);
        }
    }

    private void OrderedCrossover(int[] parent1, int[] parent2)
    {
        var child1 = new int[Matrix.Length];
        var child2 = new int[Matrix.Length];

        var parent1Vals = new bool[parent1.Length];
        var parent2Vals = new bool[parent2.Length];

        int start = -1;
        int end = -1;
        int size = parent1.Length;

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

        Population.Add(Individual.Create(child1));
        Population.Add(Individual.Create(child2));
    }

    private void Mutation()
    {
        for (var i = 0; i < Population.Count; i++)
        {
            double probability = _rnd.NextDouble();

            if (probability >= _mutationProbability) continue;

            int start = -1;
            int end = -1;
            GetTwoRandomPoints(ref start, ref end);
            Array.Reverse(Population[i].Path, start, end - start);
        }
    }

    public void SaveBestPopulation()
    {
        var currentBestPath = new List<int>();
        int currentBestCost = int.MaxValue;

        for (int i = 0; i < Population.Count; i++)
        {
            Population[i].CalculateCost(Matrix);

            if (currentBestCost <= Population[i].Cost) continue;

            currentBestCost = Population[i].Cost;
            currentBestPath = new(Population[i].Path);
        }

        if (Cost <= currentBestCost)
        {
            _noImprove++;
            return;
        }

        BestPath = currentBestPath;
        BestPath.Add(currentBestPath[0]);
        Cost = currentBestCost;
        Console.WriteLine($"Found new cost! {Cost}");
        _noImprove = 0;
    }

    private void GetTwoRandomPoints(ref int start, ref int end)
    {
        do
        {
            start = _rnd.Next(Matrix.Length);
            end = _rnd.Next(Matrix.Length);
        } while (start == end);

        if (end < start) (start, end) = (end, start);
    }

    private class Individual
    {
        public int[] Path { get; set; }
        public int Cost { get; set; } = -1;

        private Individual(int[] path)
        {
            Path = path;
        }

        public static Individual Create(int[] path)
        {
            return new Individual(path);
        }

        public void CalculateCost(ImmutableArray<ImmutableArray<int>> matrix)
        {
            Cost = 0;
            for (int i = 0; i < Path.Length - 1; i++)
            {
                Cost += matrix[Path[i]][Path[i + 1]];
            }

            Cost += matrix[Path[matrix.Length - 1]][Path[0]];
        }
    }
}

