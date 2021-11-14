using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard;

static class EnumerableExtensions
{
    public static TSource MaxElement<TSource, R>(this IEnumerable<TSource> container, Func<TSource, R> valuingFoo) where R : IComparable
    {
        var enumerator = container.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new ArgumentException("Container is empty!");

        var maxElem = enumerator.Current;
        var maxVal = valuingFoo(maxElem);

        while (enumerator.MoveNext())
        {
            var currVal = valuingFoo(enumerator.Current);

            if (currVal.CompareTo(maxVal) > 0)
            {
                maxVal = currVal;
                maxElem = enumerator.Current;
            }
        }

        return maxElem;
    }

    public static TSource MinElement<TSource, R>(this IEnumerable<TSource> container, Func<TSource, R> valuingFoo) where R : IComparable
    {
        var enumerator = container.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new ArgumentException("Container is empty!");

        var maxElem = enumerator.Current;
        var maxVal = valuingFoo(maxElem);

        while (enumerator.MoveNext())
        {
            var currVal = valuingFoo(enumerator.Current);

            if (currVal.CompareTo(maxVal) < 0)
            {
                maxVal = currVal;
                maxElem = enumerator.Current;
            }
        }

        return maxElem;
    }
}