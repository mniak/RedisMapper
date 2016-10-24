using System.Collections.Generic;

namespace RedisMapper.Demo
{
    internal class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<string> Strings { get; set; }
        public Dictionary<string, string> Entries { get; set; }
    }
}
