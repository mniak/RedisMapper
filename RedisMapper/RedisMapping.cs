using StackExchange.Redis;
using System;
using System.Reflection;

namespace RedisMapper
{
    internal class RedisMapping
    {
        private Func<object, RedisValue> innerGetValue;
        private Action<object, RedisValue> innerSetValue;
        private readonly MemberInfo memberInfo;

        public RedisMapping(MemberInfo member)
        {
            this.memberInfo = member;

            var pi = member as PropertyInfo;
            var fi = member as FieldInfo;

            if (pi != null)
            {
                this.innerGetValue = obj => ToRedisValue(pi.GetValue(obj));
                this.innerSetValue = (obj, val) => pi.SetValue(obj, ToCLRValue(val, pi.PropertyType));
            }
            else if (fi != null)
            {
                this.innerGetValue = obj => ToRedisValue(fi.GetValue(obj));
                this.innerSetValue = (obj, val) => fi.SetValue(obj, ToCLRValue(val, fi.FieldType));
            }
            else
            {
            }

        }

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
            else return value.ToString();
        }

        public RedisMapping(MemberInfo memberInfo, string fieldName) : this(memberInfo)
        {
            this.FieldName = fieldName;
        }

        public RedisValue FieldName { get; internal set; }
        public RedisValue GetValue(object entity)
        {
            return this.innerGetValue(entity);
        }
        public void SetValue(object entity, RedisValue value)
        {
            this.innerSetValue(entity, value);
        }
    }
}
