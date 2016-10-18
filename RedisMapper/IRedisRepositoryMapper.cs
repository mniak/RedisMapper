using System;
using System.Linq.Expressions;

namespace RedisMapper
{
    public interface IRedisRepositoryMapper<T>
    {
        IRedisRepositoryMapper<T> SetConstructor(Func<T> constructor);
        IRedisRepositoryMapper<T> SetName(string name);
        IRedisRepositoryMapper<T> MapId<TOut>(Expression<Func<T, TOut>> expression, bool autonumeric = false);
        IRedisRepositoryMapper<T> Map<TOut>(Expression<Func<T, TOut>> expression, string fieldName = null);
    }
}
