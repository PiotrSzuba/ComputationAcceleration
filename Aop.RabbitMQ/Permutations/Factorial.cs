using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Aop.RabbitMQ.Permutations;

public class Factorial
{
	protected static long[] FactorialTable = new long[21];

	static Factorial()
	{
		FactorialTable[0] = 1; // To prevent divide by 0
		long f = 1;
		for (int i = 1; i <= 20; i++)
		{
			f *= i;
			FactorialTable[i] = f;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long GetFactorial(int val)
	{
		if (val > 20)
		{
			throw new OverflowException($"{nameof(Factorial)} only support a factorial value <= 20");
		}

		return FactorialTable[val];
	}
}