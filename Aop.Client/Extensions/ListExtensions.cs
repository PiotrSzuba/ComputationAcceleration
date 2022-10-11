using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aop.Client.Extensions;

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

    public static IEnumerable<IEnumerable<T>> GetPermutations<T>(this IList<T> list, int? length = null)
    {
        int len = length ?? list.Count ;
        if (length == 1) return list.Select(item => new T[] { item });

        return GetPermutations(list, len - 1)
            .SelectMany(t => list.Where(e => !t.Contains(e)),
                (item1, item2) => item1.Concat(new T[] { item2 }));
    }
}
