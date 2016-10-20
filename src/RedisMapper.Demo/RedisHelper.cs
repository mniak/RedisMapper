using StackExchange.Redis;
using System;

namespace RedisMapper.Demo
{
    internal class RedisHelper
    {
        private static Lazy<RedisHelper> _instance = new Lazy<RedisHelper>(() => new RedisHelper());
        public static RedisHelper Instance { get { return _instance.Value; } }

        private RedisHelper()
        {
            this.Connection = ConnectionMultiplexer.Connect("localhost");
            this.Database = this.Connection.GetDatabase();
        }
        public ConnectionMultiplexer Connection { get; private set; }
        public IDatabase Database { get; private set; }
    }
}
