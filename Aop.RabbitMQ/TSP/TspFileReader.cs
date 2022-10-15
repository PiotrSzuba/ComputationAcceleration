using System.Collections.Immutable;
using System.Text;

namespace Aop.RabbitMQ.TSP;

public class TspFileReader
{
    private readonly string _path = Directory.GetCurrentDirectory() + @"\instances";
    private List<List<int>> Matrix { get; set; }

    public string Name { get; set; }
    public int CitiesAmmount { get; set; }
    public ImmutableArray<ImmutableArray<int>> ImMatrix { get; set; }
    public int OptimalValue { get; set; }

    public TspFileReader(string file)
    {
        var allFiles = Directory.GetFiles(_path);

        var filePath = GetFullPath(file);

        if (!allFiles.Contains(filePath))
        {
            throw new Exception("File doesnt exists");
        }


        var lines = File.ReadLines(filePath).ToList();

        Matrix = new List<List<int>>();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i == 0) Name = lines[i];
            else if (i == 1) CitiesAmmount = GetNumber(lines[i]);
            else if (lines[i].Length > 10) Matrix.Add(StringLineToVectorInt(lines[i]));
            else if (i == lines.Count - 1) OptimalValue = GetNumber(lines[i]);
            else throw new Exception("Reading file not fully handled");
        }

        if (Name == null)
            throw new Exception("File corrupted ?");

        RepairMatrix();
        ImMatrix = CreateImmutableArray(Matrix);
    }

    private static List<int> StringLineToVectorInt(string line)
    {
        var numbers = new List<int>();

        var strBuilder = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ' ' && strBuilder.Length > 0)
            {
                numbers.Add(GetNumber(strBuilder.ToString()));

                strBuilder.Clear();
                continue;
            }
            if (line[i] != ' ')
                strBuilder.Append(line[i]);
        }

        if (strBuilder.Length == 0) return numbers;

        numbers.Add(GetNumber(strBuilder.ToString()));

        return numbers;
    }

    public void PrintMatrix()
    {
        Matrix.Select(line => string.Join(' ', line.Select(number => number.ToString()).ToList()))
            .ToList()
            .ForEach(line => Console.WriteLine(line));
    }

    private static int GetNumber(string input)
    {
        if (int.TryParse(input, out int number))
            return number;
        else
            throw new Exception("{input} was not a number!");
    }

    private void RepairMatrix()
    {
        if (!Matrix.All(m => m.Count == Matrix.Count))
            throw new Exception("Matrix lacks column or row");

        for (int i = 0, j = 0; i < Matrix.Count; i++, j++)
        {
            Matrix[i][j] = -1;
        }
    }

    private string GetFullPath(string file)
    {
        var strBuilder = new StringBuilder();
        strBuilder.Append(_path);
        strBuilder.Append(@"\");
        strBuilder.Append(file);

        return strBuilder.ToString();
    }
    private static ImmutableArray<ImmutableArray<T>> CreateImmutableArray<T>(List<List<T>> list2d)
    {
        if (list2d == null)
            throw new ArgumentNullException(nameof(list2d));

        var builder1d = ImmutableArray.CreateBuilder<T>();
        var builder2d = ImmutableArray.CreateBuilder<ImmutableArray<T>>();

        int size = list2d.Count;

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < list2d[i].Count; j++)
            {
                builder1d.Add(list2d[i][j]);
            }
            builder2d.Add(builder1d.ToImmutableArray());
            builder1d.Clear();
        }

        return builder2d.ToImmutableArray();
    }
}
