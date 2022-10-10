using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CitySimulation.Tools
{
    public static class Extentions
    {
        public static T Pop<K, T>(this Dictionary<K, HashSet<T>> dict, K key)
        {
            var result = dict[key].Pop();

            if (dict[key].Count == 0)
            {
                dict.Remove(key);
            }

            return result;
        }

        public static T Pop<T>(this HashSet<T> hashSet)
        {
            var item = hashSet.First();
            hashSet.Remove(item);
            return item;
        }

        public static void Remove<K, T>(this Dictionary<K, HashSet<T>> dict, K key, T value)
        {
            dict[key].Remove(value);

            if (dict[key].Count == 0)
            {
                dict.Remove(key);
            }

        }


        public static T GetOrSetDefault<K, T>(this Dictionary<K,T> dict, K key, T defaultValue)
        {
            if (dict.TryAdd(key, defaultValue))
            {
                return defaultValue;
            }
            else
            {
                return dict[key];
            }
        }

        public static T GetOrSetDefault<K, T>(this Dictionary<K, T> dict, K key, Func<T> defaultValueProducer)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                T defaultValue = defaultValueProducer();
                dict.Add(key, defaultValue);
                return defaultValue;
            }
        }


        public static TValue GetValueOrDefaultWithProvider<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValue)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            TValue? value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue();
        }

        public static int GetMaxIndex<T>(this List<T> list, Func<T, double> selector)
        {
            double max = double.MinValue;
            int index = -1;

            for (int i = 0; i < list.Count; i++)
            {
                double val = selector(list[i]);
                if (val > max)
                {
                    max = val;
                    index = i;
                }
            }

            return index;
        }
        public static double NextDouble(this Random random, (double, double) range)
        {
            return random.NextDouble() * (range.Item2 - range.Item1) + range.Item1;
        }
        public static double NextDouble(this Random random, double from, double to)
        {
            return random.NextDouble() * (to - from) + from;
        }
        //public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
        //{
        //    return source.MinBy(selector, null);
        //}

        //public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer)
        //{
        //    if (source == null) throw new ArgumentNullException("source");
        //    if (selector == null) throw new ArgumentNullException("selector");
        //    comparer ??= Comparer<TKey>.Default;

        //    using (var sourceIterator = source.GetEnumerator())
        //    {
        //        if (!sourceIterator.MoveNext())
        //        {
        //            throw new InvalidOperationException("Sequence contains no elements");
        //        }
        //        var min = sourceIterator.Current;
        //        var minKey = selector(min);
        //        while (sourceIterator.MoveNext())
        //        {
        //            var candidate = sourceIterator.Current;
        //            var candidateProjected = selector(candidate);
        //            if (comparer.Compare(candidateProjected, minKey) < 0)
        //            {
        //                min = candidate;
        //                minKey = candidateProjected;
        //            }
        //        }
        //        return min;
        //    }
        //}

        public static IEnumerable<(int, T)> Number<T>(this IEnumerable<T> source, int start = 0)
        {
            return Enumerable.Range(start, source.Count()).Zip(source);
        }

        public static List<T> PopItems<T>(this List<T> source, Predicate<T> predicate)
        {
            var sublist = source.Where(x => predicate(x)).ToList();
            source.RemoveAll(predicate);
            return sublist;
        }
        public static List<T> PopItems<T>(this List<T> source, int count, int index = 0)
        {
            int min = Math.Min(source.Count - index, count);
            var sublist = source.GetRange(index, min);
            source.RemoveRange(index, min);
            return sublist;
        }

        public static T GetRandom<T>(this IEnumerable<T> source, Random rand)
        {
            return source.Skip(rand.Next(source.Count())).First();
        }

        public static T RandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, double> weightSelector)
        {
            double totalWeight = sequence.Sum(weightSelector);
            // The weight we are after...
            double itemWeightIndex = (float)new Random().NextDouble() * totalWeight;
            double currentWeightIndex = 0;

            foreach (var item in from weightedItem in sequence select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
            {
                currentWeightIndex += item.Weight;

                // If we've hit or passed the weight we are after for this item then it's the one we want....
                if (currentWeightIndex >= itemWeightIndex)
                    return item.Value;

            }

            return default(T);

        }

        public static T GetRandomOrNull<T>(this IEnumerable<T> source, Random rand)
        {
            if (source.Any())
            {
                return source.Skip(rand.Next(source.Count())).First();
            }

            return default(T);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rand)
        {
            return source.OrderBy(x => rand.Next());
        }

        public static R ConvertArray<T, R>(this T[] array, Func<T[], R> func)
        {
            return func(array);
        }
    }
}
