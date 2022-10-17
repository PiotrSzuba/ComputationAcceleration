using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Aop.RabbitMQ.Permutations;

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
}
