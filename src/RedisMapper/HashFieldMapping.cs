using StackExchange.Redis;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace RedisMapper
{
    internal class HashFieldMapping
    {
        private const char SEPARATOR = ':';

        private Func<object, object> innerGetValue;
        private Action<object, object> innerSetValue;
        private readonly MemberInfo memberInfo;
        private readonly bool isDictionary;
        private readonly Type underlyingType;
        private readonly Type dictValueType;

        public HashFieldMapping(MemberInfo member)
        {
            this.memberInfo = member;

            var pi = member as PropertyInfo;
            var fi = member as FieldInfo;

            if (pi != null)
            {
                this.isDictionary = IsTypeValidDictionary(pi.PropertyType);
                this.underlyingType = pi.PropertyType;
                this.innerGetValue = obj => pi.GetValue(obj);
                this.innerSetValue = (obj, val) => pi.SetValue(obj, val);
                this.dictValueType = GetDictionaryValueType(underlyingType);
            }
            else if (fi != null)
            {
                this.isDictionary = IsTypeValidDictionary(fi.FieldType);
                this.underlyingType = fi.FieldType;
                this.innerGetValue = obj => fi.GetValue(obj);
                this.innerSetValue = (obj, val) => fi.SetValue(obj, val);
                this.dictValueType = GetDictionaryValueType(underlyingType);
            }
            else
            {
            }

        }

        private Type GetDictionaryValueType(Type underlyingType)
        {
            var result = underlyingType.GetInterfaces()
                    .FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    ?.GenericTypeArguments[1];
            return result;
        }

        private bool IsTypeValidDictionary(Type type)
        {
            //var result = type.GetInterfaces()
            //    .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            //    .Select(i => i.GenericTypeArguments[0])
            //    .Any(i => i.GetTypeInfo().IsGenericType &&
            //        (typeof(KeyValuePair<,>).IsAssignableFrom(i.GetGenericTypeDefinition())
            //        || typeof(Tuple<,>).IsAssignableFrom(i.GetGenericTypeDefinition())));
            var result = type.GetInterfaces().Any(i => i == typeof(IDictionary));
            return result;
        }

        internal IEnumerable<HashEntry> GetEntries(object entity)
        {
            if (isDictionary)
            {
                var dict = this.innerGetValue(entity) as IDictionary;
                var prefix = GetPrefix();
                foreach (DictionaryEntry entry in dict)
                {
                    yield return new HashEntry(prefix + ToRedisValue(entry.Key), ToRedisValue(entry.Value));
                }
            }
            else
            {
                yield return new HashEntry(this.FieldName, this.GetValue(entity));
            }
        }

        private string GetPrefix()
        {
            return this.FieldName + SEPARATOR;
        }

        internal void ReadFromEntries(object entity, HashEntry[] entries)
        {
            if (isDictionary)
            {
                var dict = (IDictionary)Activator.CreateInstance(underlyingType);
                innerSetValue(entity, dict);

                var prefix = GetPrefix();
                var prefixLength = prefix.Length;
                var entriesDict =
                    entries.Where(e => e.Name.ToString().StartsWith(prefix))
                    .ToDictionary(e => e.Name.ToString().Substring(prefixLength), e => e.Value);

                foreach (var ed in entriesDict)
                {
                    dict[ed.Key] = ToCLRValue(ed.Value, dictValueType);
                }
            }
            else
            {
                this.SetValue(entity, entries.SingleOrDefault(x => x.Name == FieldName).Value);
            }
        }

        public HashFieldMapping(MemberInfo memberInfo, string fieldName) : this(memberInfo)
        {
            this.FieldName = fieldName;
        }

        public RedisValue FieldName { get; internal set; }
        public RedisValue GetValue(object entity)
        {
            return ToRedisValue(innerGetValue(entity));
        }
        public void SetValue(object entity, RedisValue value)
        {
            this.innerSetValue(entity, ToCLRValue(value, underlyingType));
        }

        /* Private Methods */
        private object ToCLRValue(RedisValue value, Type propertyType)
        {
            if (propertyType == typeof(RedisValue)) return value;
            else if (propertyType == typeof(bool)) return (bool)value;
            else if (propertyType == typeof(bool?)) return (bool?)value;
            else if (propertyType == typeof(int)) return (int)value;
            else if (propertyType == typeof(int?)) return (int?)value;
            else if (propertyType == typeof(long)) return (long)value;
            else if (propertyType == typeof(long?)) return (long?)value;
            else if (propertyType == typeof(double)) return (double)value;
            else if (propertyType == typeof(double?)) return (double?)value;
            else if (propertyType == typeof(string)) return (string)value;
            else if (propertyType == typeof(byte[])) return (byte[])value;
            else return value.ToString();
        }
        private RedisValue ToRedisValue(object value)
        {
            if (value is RedisValue) return (RedisValue)value;
            else if (value is bool) return (bool)value;
            else if (value is bool?) return value as bool?;
            else if (value is int) return (int)value;
            else if (value is int?) return value as int?;
            else if (value is long) return (long)value;
            else if (value is long?) return value as long?;
            else if (value is double) return (double)value;
            else if (value is double?) return value as double?;
            else if (value is string) return value as string;
            else if (value is byte[]) return (byte[])value;
            else if (value == null) return string.Empty;
            else return value.ToString();
        }
    }
}
