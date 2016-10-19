﻿using System;
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
            var repo = new HashRepository(RedisHelper.Instance.Database);
            repo.Register<User>(m =>
            {
                m.SetName("users");
                m.MapId(x => x.Id);
                m.Map(x => x.FirstName, "first_name");
                m.Map(x => x.LastName, "last_name");
            });

            repo.StoreAsync(new User()
            {
                Id = 8,
                FirstName = "Peter",
                LastName = "Parker",
            }, expiration: 30);

            var user = await repo.RetrieveAsync<User>(12);
            if (user != null)
                Console.WriteLine($"USER: {user.Id}\n\tNome={user.FirstName}\n\tSobrenome={user.LastName}");
            else
                Console.WriteLine("User not found!");

            var ids = await repo.GetIdsAsync<User>();
            var users = Task.WhenAll(ids.Select(x => repo.RetrieveAsync<User>(x)).ToList());
            foreach (var id in ids)
            {
                Console.WriteLine($"id => {id}");
            }

            /* end */
            Console.WriteLine("--END--");
        }
    }
}