using GliderView.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Repositories
{
    public interface IUserRepository
    {
        Task CreateInvitation(Invitation invite);
        Task CreateUser(User user);
        Task<Invitation?> GetInvitationByToken(string token);
        Task<User?> GetUser(Guid userId);
        Task<User?> GetUserByEmail(string email);
        Task UpdateFailedLoginAttempts(User user);
        Task UpdatePassword(User user);
    }
}
