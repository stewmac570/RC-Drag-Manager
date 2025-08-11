using System.Collections.Generic;

namespace RCDragManagerProd
{
    internal static class DictEx
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dict,
            TKey key,
            TValue defaultValue = default)
        {
            if (dict == null) return defaultValue;
            return dict.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
