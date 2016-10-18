using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisMapper
{
    public class RedisRepository
    {
        private readonly Dictionary<Type, object> mappers;
        private readonly IDatabase database;

        public RedisRepository(IDatabase database)
        {
            this.mappers = new Dictionary<Type, object>();
            this.database = database;
        }

        public void Register<T>(Action<IRedisRepositoryMapper<T>> mapperAction)
        {
            var mapper = new RedisRepositoryMapper<T>();
            mapperAction(mapper);
            this.mappers[typeof(T)] = mapper;
        }
        public void Unregister<T>()
        {
            this.mappers.Remove(typeof(T));
        }

        private RedisRepositoryMapper<T> GetMapper<T>()
        {
            var tt = typeof(T);
            var mapper = mappers[tt];
            if (mapper == null)
                throw new InvalidOperationException($"The type '{tt.FullName}' was not mapped.");
            return (RedisRepositoryMapper<T>)mapper;
        }

        public async void SetAsync<T>(T entity, int expireAfter = 0)
        {
            var mapper = GetMapper<T>();

            var dict = mapper.GetDictionary(entity);
            var id = mapper.GetId(entity);

            if (id == null && mapper.IdAutonumeric)
            {
                id = database.StringIncrement(mapper.GetSequenceKey()).ToString();
                mapper.SetId(entity, id);
            }
            var key = mapper.GetHashKey(id);
            if (expireAfter <= 0) await database.KeyPersistAsync(key);
            await database.HashSetAsync(key, dict.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray());
            if (expireAfter > 0) await database.KeyExpireAsync(key, new TimeSpan(0, 0, expireAfter));
            await database.SetAddAsync(mapper.GetIdSetKey(), id);
        }

        public async Task<T> GetAsync<T>(RedisValue id)
        {
            var mapper = GetMapper<T>();
            var entries = await database.HashGetAllAsync(mapper.GetHashKey(id));
            if (!entries.Any())
            {
                await database.SetRemoveAsync(mapper.GetIdSetKey(), id);
                return default(T);
            }
            var dict = entries.ToDictionary(x => x.Name, x => x.Value);
            var entity = mapper.Parse(dict);
            mapper.SetId(entity, id);
            return entity;
        }

        public async Task<IEnumerable<RedisValue>> GetIdsAsync<T>()
        {
            var mapper = GetMapper<T>();
            var key = mapper.GetIdSetKey();
            return await database.SetMembersAsync(key);
        }
    }
}
