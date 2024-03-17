namespace Estreya.BlishHUD.Shared.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

public static class IEnumerableExtensions
{
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