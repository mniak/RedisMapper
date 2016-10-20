using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisMapper
{
    internal class HashMapping<T>
    {
        public HashMapping()
        {
            this.Mappings = new List<HashFieldMapping>();
            this.constructor = () => Activator.CreateInstance<T>();
        }

        public IDictionary<RedisValue, RedisValue> GetDictionary(T entity)
        {
            var result = Mappings.ToDictionary(
                x => x.FieldName,
                x => x.GetValue(entity)
                );
            return result;
        }
        public T Parse(Dictionary<RedisValue, RedisValue> dict)
        {
            var entity = constructor();
            foreach (var mapping in Mappings)
            {
                var val = dict[mapping.FieldName];
                mapping.SetValue(entity, val);
            }
            return entity;
        }

        public string GetKey()
        {
            return this.Name;
        }

        public string GetId(T entity)
        {
            return this.IdMapping.GetValue(entity);
        }
        public void SetId(T entity, RedisValue id)
        {
            this.IdMapping.SetValue(entity, id);
        }
        public string GetHashKey(T entity)
        {
            var id = GetId(entity);
            return GetHashKey(id);
        }
        public string GetHashKey(RedisValue id)
        {
            var key = this.GetKey();
            return $"{key}:{id}";
        }
        public string GetIdSetKey()
        {
            var key = this.GetKey();
            return $"{key}_ids";
        }
        public string GetSequenceKey()
        {
            var key = this.GetKey();
            return $"{key}_seq";
        }

        public string Name { get; internal set; }
        public HashFieldMapping IdMapping { get; internal set; }
        public Func<T> constructor { get; internal set; }
        public List<HashFieldMapping> Mappings { get; internal set; }
        public bool IdAutonumeric { get; internal set; }
    }
}
