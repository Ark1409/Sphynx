﻿using Sphynx.Server.ChatRooms;
using Sphynx.Server.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.Database.Collections
{
    internal interface IUserCollection
    {
        SphynxUserInfo? GetUser(Guid userId);
        SphynxUserInfo? GetUser(string username);
        IEnumerable<SphynxUserInfo> GetAllUsers();
        void AddUser(SphynxUserInfo user);
        void UpdateUserName(Guid userId, string newUsername);
        void UpdateEmail(Guid userId, string newEmail);
        void EditUserInfo(Guid userId, SphynxUserInfo user);
        void DeleteUser(Guid userId);

        // Async methods

        Task<SphynxUserInfo?> GetUserAsync(Guid userId);
        Task<SphynxUserInfo?> GetUserAsync(string username);
        Task<IEnumerable<SphynxUserInfo>> GetAllUsersAsync();
        Task AddUserAsync(SphynxUserInfo user);
        Task UpdateUserNameAsync(Guid userId, string newUsername);
        Task UpdateEmailAsync(Guid userId, string newEmail);
        Task EditUserInfoAsync(Guid userId, SphynxUserInfo user);
        Task DeleteUserAsync(Guid userId);
    }
}