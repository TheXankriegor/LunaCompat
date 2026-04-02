// ReSharper disable RedundantUsingDirective

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace LunaCompatCommon.Serializer
{
    internal static class TypeCache
    {
        #region Fields

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Cache = new ConcurrentDictionary<Type, PropertyInfo[]>();

        #endregion

        #region Non-Public Methods

        /// <summary>
        /// Returns the ordered array of serializable properties for <paramref name="type" />.
        /// A property is serializable when it is public, instance, readable, and writable.
        /// Results are cached after the first call.
        /// </summary>
        internal static PropertyInfo[] GetProperties(Type type)
        {
            return Cache.GetOrAdd(type, t =>
            {
                var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var result = new List<PropertyInfo>(props.Length);

                foreach (var prop in props)
                {
                    if (prop.CanRead && prop.CanWrite && prop.GetIndexParameters().Length == 0)
                        result.Add(prop);
                }

                return result.ToArray();
            });
        }

        #endregion
    }
}
