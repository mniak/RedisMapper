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
            this.Fields = new List<HashFieldMapping>();
        }

        public HashEntry[] GetEntries(T entity)
        {
            var result = Fields
                .SelectMany(f => f.GetEntries(entity))
                .ToArray();
            return result;
        }
        public T Parse(HashEntry[] entries)
        {
            var entity = Activator.CreateInstance<T>();
            foreach (var mapping in Fields)
            {
                mapping.ReadFromEntries(entity, entries);
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
        public List<HashFieldMapping> Fields { get; internal set; }
        public bool IdAutonumeric { get; internal set; }
        public bool IndexById { get; internal set; }
    }
}
