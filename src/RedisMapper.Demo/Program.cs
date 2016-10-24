using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisMapper.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            RunProgram();
            Console.ReadLine();
        }

        private static async void RunProgram()
        {
            await Task.Delay(0);
            var redisManager = new RepositoryHolder(RedisHelper.Instance.Database);
            redisManager.RegisterHash<User>(m =>
            {
                m.SetName("users");
                m.MapId(x => x.Id, index: true);
                m.Map(x => x.FirstName, "first_name");
                m.Map(x => x.LastName, "last_name");
                m.Map(x => x.Entries, "moreinfo");
            });
            var repo = redisManager.GetHashRepository<User>();

            await repo.StoreAsync(new User()
            {
                Id = 8,
                FirstName = "Peter",
                LastName = "Parker",
                Entries = new Dictionary<string, string>() {
                    { "wife_name", "Guadalupe" },
                    { "kids", 3.ToString()},
                },
            }, expiration: 30);

            var user = await repo.RetrieveAsync(8);
            if (user != null)
                Console.WriteLine($"USER: {user.Id}\n\tFirst Name:\t{user.FirstName}\n\tLast Name:\t{user.LastName}");
            else
                Console.WriteLine("User not found!");

            var ids = await repo.GetIdsAsync();
            var users = Task.WhenAll(ids.Select(x => repo.RetrieveAsync(x)).ToList());
            foreach (var id in ids)
            {
                Console.WriteLine($"id => {id}");
            }

            /* end */
            Console.WriteLine("--END--");
        }
    }
}
