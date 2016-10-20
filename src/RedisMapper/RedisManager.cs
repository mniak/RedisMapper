using StackExchange.Redis;
using System;
using System.Collections.Generic;
namespace RedisMapper
{
    public class RedisManager
    {
        private readonly IDatabase database;
        private readonly Dictionary<Type, object> hashMappings;

        /// <summary>
        /// Creates a new RedisManager.
        /// </summary>
        /// <param name="database">The redis database to use</param>
        public RedisManager(IDatabase database)
        {
            this.database = database;
            this.hashMappings = new Dictionary<Type, object>();
        }
        /// <summary>
        /// Register a hash mapper in a fluent manner.
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <param name="action">The mapper delegate</param>
        public void RegisterHash<T>(Action<HashRepositoryMapper<T>> mapperAction) where T : class
        {
            var mapping = new HashMapping<T>();
            mapperAction(new HashRepositoryMapper<T>(mapping));
            this.hashMappings[typeof(T)] = mapping;
        }

        /// <summary>
        /// Unregister a hash mapper.
        /// </summary>
        /// <typeparam name="T">The type to unregister</typeparam>
        public void UnregisterHash<T>() where T : class
        {
            this.hashMappings.Remove(typeof(T));
        }

        /// <summary>
        /// Gets a HashRepository for the registered type informed.
        /// </summary>
        /// <typeparam name="T">A registered type</typeparam>
        /// <returns>The HashRepository. Null if the type is not registered.</returns>
        public HashRepository<T> GetHashRepository<T>() where T : class
        {
            var mapping = this.hashMappings[typeof(T)] as HashMapping<T>;
            if (mapping == null)
                return null;
            var repo = new HashRepository<T>(database, mapping);
            return repo;
        }
    }
}
