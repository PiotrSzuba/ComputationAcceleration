using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aop.RabbitMQ.Permutations;

public class PermutationSaniSinghHuttunen
{
	public static bool NextPermutation(int[] numList)
	{
		var largestIndex = -1;
		for (var i = numList.Length - 2; i >= 0; i--)
		{
			if (numList[i] < numList[i + 1])
			{
				largestIndex = i;
				break;
			}
		}

		if (largestIndex < 0) return false;

		var largestIndex2 = -1;
		for (var i = numList.Length - 1; i >= 0; i--)
		{
			if (numList[largestIndex] < numList[i])
			{
				largestIndex2 = i;
				break;
			}
		}

		var tmp = numList[largestIndex];
		numList[largestIndex] = numList[largestIndex2];
		numList[largestIndex2] = tmp;

		for (int i = largestIndex + 1, j = numList.Length - 1; i < j; i++, j--)
		{
			tmp = numList[i];
			numList[i] = numList[j];
			numList[j] = tmp;
		}

		return true;
	}
}
