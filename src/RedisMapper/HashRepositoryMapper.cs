using System;
using System.Linq.Expressions;
using System.Reflection;

namespace RedisMapper
{
    public class HashRepositoryMapper<T> where T : class
    {
        private readonly HashMapping<T> mapping;

        internal HashRepositoryMapper(HashMapping<T> mapping)
        {
            this.mapping = mapping;
        }
        public HashRepositoryMapper<T> SetConstructor(Func<T> constructor)
        {
            this.mapping.constructor = constructor;
            return this;
        }
        public HashRepositoryMapper<T> SetName(string name)
        {
            mapping.Name = name;
            return this;
        }
        public HashRepositoryMapper<T> MapId<TOut>(Expression<Func<T, TOut>> expression, bool autonumeric = false, bool index = false)
        {
            var member = GetMemberInfo(expression);
            this.mapping.IdMapping = new HashFieldMapping(member);
            this.mapping.IdAutonumeric = autonumeric;
            this.mapping.IndexById = index;
            return this;
        }
        public HashRepositoryMapper<T> Map<TOut>(Expression<Func<T, TOut>> expression, string fieldName = null)
        {
            var member = GetMemberInfo(expression);
            var mapping = new HashFieldMapping(member, fieldName ?? member.Name);
            this.mapping.Mappings.Add(mapping);
            return this;
        }

        /* Private Methods */
        private static MemberInfo GetMemberInfo<TOut>(Expression<Func<T, TOut>> expression)
        {
            var me = expression.Body as MemberExpression;
            var member = me?.Member;
            if (member == null)
                throw new ArgumentException("Supplied expression must be a MemberExpression", nameof(expression));
            return member;
        }
    }
}
