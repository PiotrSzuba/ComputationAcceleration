using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aop.RabbitMQ.Permutations;

public class PermutationMixOuelletSaniSinghHuttunen
{
	private long _indexFirst;
	private long _indexLastExclusive;
	private int[] _sortedValues;

	public PermutationMixOuelletSaniSinghHuttunen(int[] sortedValues, long indexFirst = -1, long indexLastExclusive = -1)
	{
		if (indexFirst == -1)
		{
			indexFirst = 0;
		}

		if (indexLastExclusive == -1)
		{
			indexLastExclusive = Factorial.GetFactorial(sortedValues.Length);
		}

		if (indexFirst >= indexLastExclusive)
		{
			throw new ArgumentException($"{nameof(indexFirst)} should be less than {nameof(indexLastExclusive)}");
		}

		_indexFirst = indexFirst;
		_indexLastExclusive = indexLastExclusive;
		_sortedValues = sortedValues;
	}

	public void ExecuteForEachPermutation(Action<int[]> action)
	{
		long index = _indexFirst;

		var permutationOuellet = new PermutationOuelletLexico3<int>(_sortedValues);

		permutationOuellet.GetValuesForIndex(index);
		action(permutationOuellet.Result);
		index++;

		int[] values = permutationOuellet.Result;
		while (index < _indexLastExclusive)
		{
			PermutationSaniSinghHuttunen.NextPermutation(values);
			action(values);
			index++;
		}
	}

	public static void ExecuteForEachPermutationMT(int[] sortedValues, Action<int[]> action)
	{
		int coreCount = Environment.ProcessorCount;
		long itemsFactorial = Factorial.GetFactorial(sortedValues.Length);
		long partCount = (long)Math.Ceiling((double)itemsFactorial / (double)coreCount);
		long startIndex = 0;

		var tasks = new List<Task>();

		for (int coreIndex = 0; coreIndex < coreCount; coreIndex++)
		{
			long stopIndex = Math.Min(startIndex + partCount, itemsFactorial);

			var mix = new PermutationMixOuelletSaniSinghHuttunen(sortedValues, startIndex, stopIndex);
			Task task = Task.Run(() => mix.ExecuteForEachPermutation(action));
			tasks.Add(task);

			if (stopIndex == itemsFactorial)
			{
				break;
			}

			startIndex = startIndex + partCount;
		}

		Task.WaitAll(tasks.ToArray());
	}
}
