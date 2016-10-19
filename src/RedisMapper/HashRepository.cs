using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisMapper
{
    /// <summary>
    /// Stores and retrieves objects as Redis hashes.
    /// </summary>
    public class HashRepository
    {
        private readonly Dictionary<Type, object> mappers;
        private readonly IDatabase database;

        /// <summary>
        /// Creates a new instance of a HashRepository.
        /// </summary>
        /// <param name="database">The Redis database</param>
        public HashRepository(IDatabase database)
        {
            this.mappers = new Dictionary<Type, object>();
            this.database = database;
        }

        /// <summary>
        /// Register a type mapper in a fluent manner.
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <param name="action">The mapper delegate</param>
        public void Register<T>(Action<IHashRepositoryMapper<T>> action) where T : class
        {
            var m = new HashRepositoryMapper<T>();
            action(m);
            this.mappers[typeof(T)] = m;
        }

        /// <summary>
        /// Unregister a type mapper.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Unregister<T>() where T : class
        {
            this.mappers.Remove(typeof(T));
        }

        /// <summary>
        /// Stores an object of a registered type.
        /// </summary>
        /// <typeparam name="T">The registered type</typeparam>
        /// <param name="obj">The object to store</param>
        /// <param name="expiration">Optional expiration in seconds</param>
        public async void StoreAsync<T>(T obj, int expiration = 0) where T : class
        {
            var mapper = GetMapper<T>();

            var dict = mapper.GetDictionary(obj);
            var id = mapper.GetId(obj);

            if (id == null && mapper.IdAutonumeric)
            {
                id = database.StringIncrement(mapper.GetSequenceKey()).ToString();
                mapper.SetId(obj, id);
            }
            var key = mapper.GetHashKey(id);
            if (expiration <= 0) await database.KeyPersistAsync(key);
            await database.HashSetAsync(key, dict.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray());
            if (expiration > 0) await database.KeyExpireAsync(key, new TimeSpan(0, 0, expiration));
            await database.SetAddAsync(mapper.GetIdSetKey(), id);
        }

        /// <summary>
        /// Retrieves an object of a registered type with the specified ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The ID of the of the object to retrieve</param>
        /// <returns>If the object with the specified ID is found, it is returned. Otherwise null.</returns>
        public async Task<T> RetrieveAsync<T>(RedisValue id) where T : class
        {
            var mapper = GetMapper<T>();
            var entries = await database.HashGetAllAsync(mapper.GetHashKey(id));
            
            // Removes from the "index" if not found
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
        
        /// <summary>
        /// Get all the IDs from the "index". Some IDs can actually reference expired objects.
        /// </summary>
        /// <typeparam name="T">The registered type</typeparam>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> GetIdsAsync<T>() where T : class
        {
            var mapper = GetMapper<T>();
            var key = mapper.GetIdSetKey();
            return await database.SetMembersAsync(key);
        }

        /* Private Methods */
        private HashRepositoryMapper<T> GetMapper<T>() where T : class
        {
            var tt = typeof(T);
            var mapper = mappers[tt];
            if (mapper == null)
                throw new InvalidOperationException($"The type '{tt.FullName}' was not mapped.");
            return (HashRepositoryMapper<T>)mapper;
        }
    }
}
