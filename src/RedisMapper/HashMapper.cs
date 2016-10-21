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
        public HashRepositoryMapper<T> SetName(string name)
        {
            if (name.Contains(":"))
                throw new ArgumentException("The name of the hash has invalid chars", nameof(name));
            mapping.Name = name;
            return this;
        }
        public HashRepositoryMapper<T> MapId<TOut>(Expression<Func<T, TOut>> expression, bool autonumeric = false, bool index = false)
        {
            if (mapping.IdMapping != null)
                throw new InvalidOperationException("Only one ID field can be mapped.");

            var member = GetMemberInfo(expression);
            mapping.IdMapping = new HashFieldMapping(member);
            mapping.IdAutonumeric = autonumeric;
            mapping.IndexById = index;

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
