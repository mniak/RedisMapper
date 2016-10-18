using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RedisMapper
{
    internal class RedisRepositoryMapper<T> : IRedisRepositoryMapper<T>
    {
        private const string PREFIX = "map_";

        string name;
        RedisMapping idMapping;
        Func<T> constructor;
        List<RedisMapping> mappings;

        public bool IdAutonumeric { get; private set; }

        public RedisRepositoryMapper()
        {
            this.mappings = new List<RedisMapping>();
            this.constructor = () => Activator.CreateInstance<T>();
        }
        public IRedisRepositoryMapper<T> SetConstructor(Func<T> constructor)
        {
            this.constructor = constructor;
            return this;
        }
        public IRedisRepositoryMapper<T> SetName(string name)
        {
            this.name = name;
            return this;
        }

        public IRedisRepositoryMapper<T> MapId<TOut>(Expression<Func<T, TOut>> expression, bool autonumeric = false)
        {
            var member = GetMemberInfo(expression);
            this.idMapping = new RedisMapping(member);
            this.IdAutonumeric = autonumeric;
            return this;
        }

        public IRedisRepositoryMapper<T> Map<TOut>(Expression<Func<T, TOut>> expression, string fieldName = null)
        {
            var member = GetMemberInfo(expression);
            var mapping = new RedisMapping(member, fieldName ?? member.Name);
            mappings.Add(mapping);
            return this;
        }

        private static MemberInfo GetMemberInfo<TOut>(Expression<Func<T, TOut>> expression)
        {
            var me = expression.Body as MemberExpression;
            var member = me?.Member;
            if (member == null)
                throw new ArgumentException("Supplied expression must be a MemberExpression", nameof(expression));
            return member;
        }



        public IDictionary<RedisValue, RedisValue> GetDictionary(T entity)
        {
            var result = mappings.ToDictionary(
                x => x.FieldName,
                x => x.GetValue(entity)
                );
            return result;
        }
        public T Parse(Dictionary<RedisValue, RedisValue> dict)
        {
            var entity = constructor();
            foreach (var mapping in mappings)
            {
                var val = dict[mapping.FieldName];
                mapping.SetValue(entity, val);
            }
            return entity;
        }

        public string GetKey()
        {
            return this.name;
        }

        public string GetId(T entity)
        {
            return this.idMapping.GetValue(entity);
        }
        public void SetId(T entity, RedisValue id)
        {
            this.idMapping.SetValue(entity, id);
        }
        public string GetHashKey(T entity)
        {
            var id = GetId(entity);
            return GetHashKey(id);
        }

        public string GetHashKey(RedisValue id)
        {
            var key = this.GetKey();
            return $"{PREFIX}{key}:{id}";
        }
        public string GetIdSetKey()
        {
            var key = this.GetKey();
            return $"{PREFIX}{key}_ids";
        }
        public string GetSequenceKey()
        {
            var key = this.GetKey();
            return $"{PREFIX}{key}_seq";
        }
    }
}
