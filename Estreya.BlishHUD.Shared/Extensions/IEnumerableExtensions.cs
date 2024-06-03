namespace Estreya.BlishHUD.Shared.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

public static class IEnumerableExtensions
{
    public static IEnumerable<TResult> SelectWithIndex<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TSource>, TResult> selector)
    {
        return SelectWithIndex(source, (element, index, sourceList, first, last) => selector(element, index, sourceList));
    }

    public static IEnumerable<TResult> SelectWithIndex<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TSource>, bool, bool, TResult> selector)
    {
        var newList = new List<TResult>();
        var sourceList = source.ToList();
        for (int i = 0; i < sourceList.Count; i++)
        {
            var first = i == 0;
            var last = i == sourceList.Count - 1;
            newList.Add(selector(sourceList[i], i, source, first, last));
        }

        return newList.AsEnumerable();
    }

    public static IEnumerable<TResult> SelectManyWithIndex<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TSource>, IEnumerable<TResult>> selector)
    {
        return SelectManyWithIndex(source, (element, index, sourceList, first, last) => selector(element, index, sourceList));
    }

    public static IEnumerable<TResult> SelectManyWithIndex<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TSource>, bool, bool, IEnumerable<TResult>> selector)
    {
        var newList = new List<TResult>();
        var sourceList = source.ToList();
        for (int i = 0; i < sourceList.Count; i++)
        {
            var first = i == 0;
            var last = i == sourceList.Count - 1;
            newList.AddRange(selector(sourceList[i], i, source, first, last));
        }

        return newList.AsEnumerable();
    }

    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        HashSet<TKey> known = new HashSet<TKey>();
        return source.Where(element => known.Add(keySelector(element)));
    }

    public static IEnumerable<IEnumerable<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        return source
               .Select((x, i) => new
               {
                   Index = i,
                   Value = x
               })
               .GroupBy(x => x.Index / chunkSize)
               .Select(x => x.Select(v => v.Value));
    }

    public static T PickRandom<T>(this IEnumerable<T> source)
    {
        return source.PickRandom(1).Single();
    }

    public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
    {
        return source.Shuffle().Take(count);
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(x => Guid.NewGuid());
    }
}