// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

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