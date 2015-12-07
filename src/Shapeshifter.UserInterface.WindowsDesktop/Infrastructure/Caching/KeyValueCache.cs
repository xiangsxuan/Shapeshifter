﻿namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Caching
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Interfaces;

    class KeyValueCache<TKey, TValue>: IKeyValueCache<TKey, TValue>
    {
        readonly IDictionary<TKey, TValue> data;

        public KeyValueCache()
        {
            data = new Dictionary<TKey, TValue>();
        }

        public TValue Get(TKey key)
        {
            return data.ContainsKey(key) ? data[key] : default(TValue);
        }

        public void Set(TKey key, TValue value)
        {
            if (data.ContainsKey(key))
            {
                data[key] = value;
            }
            else
            {
                data.Add(key, value);
            }
        }

        public TValue Thunkify(TKey argument, Func<TKey, TValue> method)
        {
            var cachedResult = Get(argument);
            if (!Equals(cachedResult, default(TValue)))
            {
                return cachedResult;
            }

            var result = method(argument);
            Set(argument, result);

            return result;
        }

        public async Task<TValue> ThunkifyAsync(TKey argument, Func<TKey, Task<TValue>> method)
        {
            var cachedResult = Get(argument);
            if (!Equals(cachedResult, default(TValue)))
            {
                return cachedResult;
            }

            var result = await method(argument);
            Set(argument, result);

            return result;
        }
    }
}