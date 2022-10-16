using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Aop.RabbitMQ.Extensions;

public static class ListExtensions
{
    public static IEnumerable<T> AsRandom<T>(this IList<T> list)
    {
        int[] indexes = Enumerable.Range(0, list.Count).ToArray();
        var generator = new Random();

        for (int i = 0; i < list.Count; ++i)
        {
            int position = generator.Next(i, list.Count);

            yield return list[indexes[position]];

            indexes[position] = indexes[i];
        }
    }

    public static List<List<int>> GetPermutations(this IList<int> list)
    {
        var perms = new List<List<int>>();

        var arr = list.ToArray();

        perms.Add(arr.ToList());

        while (NextPermutation(arr))
        {
            perms.Add(arr.ToList());
        }

        return perms;
    }

    private static bool NextPermutation(int[] numList)
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

    public static List<List<int>> GetPermutationFast(this IList<int> list)
    {
        var values = list.ToArray();
        List<int[]> result = new();
        ForAllPermutation(values, (vals) =>
        {
            result.Add(vals);
            return false;
        });

        return result.Select(x => x.ToList()).ToList();
    }

    //Fast permutation algorithm by EricOuellet
    //https://github.com/EricOuellet2
    private static bool ForAllPermutation<T>(T[] items, Func<T[], bool> funcExecuteAndTellIfShouldStop)
    {
        int countOfItem = items.Length;

        if (countOfItem <= 1)
        {
            return funcExecuteAndTellIfShouldStop(items);
        }

        var indexes = new int[countOfItem];

        if (funcExecuteAndTellIfShouldStop(items))
        {
            return true;
        }

        for (int i = 1; i < countOfItem;)
        {
            if (indexes[i] < i)
            {
                if ((i & 1) == 1)
                {
                    Swap(ref items[i], ref items[indexes[i]]);
                }
                else
                {
                    Swap(ref items[i], ref items[0]);
                }

                if (funcExecuteAndTellIfShouldStop(items))
                {
                    return true;
                }

                indexes[i]++;
                i = 1;
            }
            else
            {
                indexes[i++] = 0;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap<T>(ref T a, ref T b)
    {
        (b, a) = (a, b);
    }
}
