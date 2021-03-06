﻿using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisMapper
{
    /// <summary>
    /// Stores and retrieves instances of the informed type as Redis hashes.
    /// </summary>
    /// <typeparam name="T">The type of the objects to store. It must be a referece type.</typeparam>
    public class HashRepository<T> where T : class, new()
    {
        internal readonly IDatabase database;
        internal readonly HashMapping<T> mapping;

        internal HashRepository(IDatabase database, HashMapping<T> mapping)
        {
            this.database = database;
            this.mapping = mapping;
        }

        /// <summary>
        /// Stores an object of a registered type.
        /// </summary>
        /// <param name="obj">The object to store</param>
        /// <param name="expiration">Optional expiration in seconds</param>
        public async Task StoreAsync(T obj, int expiration = 0)
        {
            var entries = mapping.GetEntries(obj);
            var id = mapping.GetId(obj);

            if (id == null && mapping.IdAutonumeric)
            {
                id = database.StringIncrement(mapping.GetSequenceKey()).ToString();
                mapping.SetId(obj, id);
            }
            var key = mapping.GetHashKey(id);

            if (expiration <= 0) await database.KeyPersistAsync(key);
            await database.HashSetAsync(key, entries);
            if (expiration > 0) await database.KeyExpireAsync(key, new TimeSpan(0, 0, expiration));

            if (IndexById)
            {
                await database.SetAddAsync(mapping.GetIdSetKey(), id);
            }
        }

        /// <summary>
        /// Retrieves an object of a registered type with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the of the object to retrieve</param>
        /// <returns>If the object with the specified ID is found, it is returned. Otherwise null.</returns>
        public async Task<T> RetrieveAsync(RedisValue id)
        {
            var entries = await database.HashGetAllAsync(mapping.GetHashKey(id));

            if (!entries.Any() && IndexById)
            {
                // Removes from the index if not found
                await database.SetRemoveAsync(mapping.GetIdSetKey(), id);
                return default(T);
            }
            var entity = mapping.Parse(entries);
            mapping.SetId(entity, id);
            return entity;
        }

        /// <summary>
        /// Get all the IDs from the index. 
        /// Some IDs can actually reference expired objects.
        /// **Important:** If the value of the property `IndexById` is false, a NotSupportedException will be thrown.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<RedisValue>> GetIdsAsync()
        {
            if (!IndexById)
                throw new NotSupportedException("The IDs cannot be retrieved because the ID mapper was not set parameter `index=true.`");
            var key = mapping.GetIdSetKey();
            return await database.SetMembersAsync(key);
        }

        /// <summary>
        /// Deletes a hash entry by the ID asynchronously
        /// </summary>
        /// <param name="id">The ID to remove from the hash</param>
        /// <returns>The task</returns>
        public async Task DeleteByIdAsync(RedisValue id)
        {
            var tasks = new List<Task>() {
                database.KeyDeleteAsync(mapping.GetHashKey(id)),
            };
            if (IndexById) tasks.Add(database.SetRemoveAsync(mapping.GetIdSetKey(), id));
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Indicates if this repository is indexed by the id.
        /// This value is set in the mapper using paramenter `index` of the method `.MapId()`.
        /// </summary>
        public bool IndexById { get { return this.mapping.IndexById; } }
    }
}
