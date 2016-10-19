using System;
using System.Linq.Expressions;

namespace RedisMapper
{
    public interface IHashRepositoryMapper<T>
    {
        IHashRepositoryMapper<T> SetConstructor(Func<T> constructor);
        IHashRepositoryMapper<T> SetName(string name);
        IHashRepositoryMapper<T> MapId<TOut>(Expression<Func<T, TOut>> expression, bool autonumeric = false);
        IHashRepositoryMapper<T> Map<TOut>(Expression<Func<T, TOut>> expression, string fieldName = null);
    }
}
