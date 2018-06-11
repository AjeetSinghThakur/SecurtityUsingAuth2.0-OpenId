using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using SocialNetwork.Data.Models;

namespace SocialNetwork.Data.Repositories
{
    public class UserRepository : Repository, IUserRepository
    {
        public UserRepository(Func<IDbConnection> openConnection) : base(openConnection) { }

        public async Task<User> GetAsync(string username, string password)
        {
            using (var connection = OpenConnection())
            {
                var queryResult = await connection.QueryAsync<User>("select * from [Users] where [Username]=@username and [Password]=@password",
                    new { username, password });

                return queryResult.SingleOrDefault();
            }
        }

        public async Task<User> GetAsync(string username)
        {
            using (var connection = OpenConnection())
            {
                var queryResult = await connection.QueryAsync<User>("select * from [Users] where [Username]=@username",
                    new { username });

                return queryResult.SingleOrDefault();
            }
        }

        public async Task<IEnumerable<FriendRelation>> GetFriendsForAsync(User user)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<FriendRelation>("select * from [Friends] where [InitiaterId]=@userId or [FriendId]=@userId",
                    new { userId = user.Id });
            }
        }

        public async Task AddAsync(User user)
        {
            using (var connection = OpenConnection())
            {
                await connection.ExecuteAsync("insert into [Users]([Id], [Username], [Password]) values(@userId, @username, @password); ",
                    new { userId = user.Id, username = user.Username, password = user.Password });
            }
        }
    }

    public interface IUserRepository
    {
        Task<IEnumerable<FriendRelation>> GetFriendsForAsync(User user);
        Task AddAsync(User user);
        Task<User> GetAsync(string username, string password);
        Task<User> GetAsync(string username);
    }
}