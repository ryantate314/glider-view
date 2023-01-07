using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GliderView.Service
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IFlightRepository _flightRepo;

        private const int MAX_FAILED_LOGIN_ATTEMPTS = 10;
        private const int INVITATION_EXPIRATION_MINUTES = 60 * 24 * 7;

        public UserService(
            IUserRepository userRepository,
            ILogger<UserService> logger,
            IPasswordHasher<User> passwordHasher,
            IFlightRepository flightRepo
        )
        {
            _userRepository = userRepository;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _flightRepo = flightRepo;
        }

        public async Task<User?> ValidateUsernameAndPassword(string email, string password)
        {
            Thread.Sleep(new Random().Next(50, 100));

            User? user = await _userRepository.GetUserByEmail(email);

            if (user == null)
            {
                _logger.LogInformation("User does not exist with email " + email);
                return null;
            }

            if (user.IsLockedOut)
            {
                _logger.LogInformation("User is locked out: " + email);
                return null;
            }

            PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword(user, user.HashedPassword, password);
            if (result != PasswordVerificationResult.Failed)
            {
                _logger.LogInformation("Successfully authenticated user " + email);
                if (user.FailedLoginAttempts > 0)
                {
                    user.FailedLoginAttempts = 0;
                    await _userRepository.UpdateFailedLoginAttempts(user);
                }
                return user;
            }
            else
            {
                _logger.LogInformation("Invalid password for user " + email + ". Failed login attempts: " + user.FailedLoginAttempts);
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts > MAX_FAILED_LOGIN_ATTEMPTS)
                    user.IsLockedOut = true;
                await _userRepository.UpdateFailedLoginAttempts(user);
                return null;
            }
        }

        public async Task<User> CreateUser(string email, string name, char role)
        {
            var user = new User()
            {
                Email = email,
                Name = name,
                Role = role
            };

            await _userRepository.CreateUser(user);

            return user;
        }

        public async Task<Invitation> GetNewInvitation(Guid userId)
        {
            var invite = new Invitation()
            {
                UserId = userId,
                Token = GenerateInvitationToken(),
                ExpirationDate = DateTime.UtcNow.Add(
                    TimeSpan.FromMinutes(INVITATION_EXPIRATION_MINUTES)
                )
            };
            await _userRepository.CreateInvitation(invite);

            _logger.LogInformation("Issued invitation token for user " + userId);
            return invite;
        }

        public async Task<User> BuildUser(Invitation invitation, string email, string password)
        {
            // Validate the token
            Invitation? invite = await _userRepository.GetInvitationByToken(invitation.Token);
            if (invite == null)
            {
                _logger.LogInformation("Invalid invitation token: " + invitation.Token);
                throw new InvalidOperationException("Token is not valid.");
            }

            User user = (await _userRepository.GetUser(invite.UserId))!;
            if (!String.Equals(email, user.Email))
            {
                _logger.LogInformation($"Email address does not match for token {invitation.Token}: {email}");
                throw new InvalidOperationException("Token is not valid.");
            }

            user.HashedPassword = _passwordHasher.HashPassword(user, password);

            await _userRepository.UpdatePassword(user);
            await _userRepository.DeleteInvitation(invitation.Token);

            return user;
        }

        public async Task<bool> ValidateInvitation(string email, string token)
        {
            Thread.Sleep(new Random().Next(50, 100));

            Invitation? invite = await _userRepository.GetInvitationByToken(token);
            if (invite == null)
            {
                _logger.LogInformation("Invalid invitation token: " + token);
                return false;
            }

            User user = (await _userRepository.GetUser(invite.UserId))!;
            if (!String.Equals(email, user.Email))
            {
                _logger.LogInformation($"Email address does not match for token {token}: {email}");
                return false;
            }

            _logger.LogInformation("Validated invitation token for user " + user.UserId);
            return true;
        }

        private string GenerateInvitationToken()
        {
            return Guid.NewGuid()
                .ToString()
                .Replace("-", "")
                .ToLower();
        }

        public Task<User> GetUser(Guid userId)
        {
            return _userRepository.GetUser(userId);
        }

        public async Task<bool> UpdatePassword(Guid userId, string currentPassword, string newPassword)
        {
            User? user = await _userRepository.GetUser(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User does not exist.");
            }

            if (String.IsNullOrEmpty(user.HashedPassword))
                throw new InvalidOperationException("User does not have a password.");

            if (_passwordHasher.VerifyHashedPassword(user, user.HashedPassword, currentPassword) == PasswordVerificationResult.Failed)
                return false;

            user.HashedPassword = _passwordHasher.HashPassword(user, newPassword);

            await _userRepository.UpdatePassword(user);

            return true;
        }

        public Task<IEnumerable<User>> GetUsers()
        {
            return _userRepository.GetUsers();
        }

        public Task<List<LogBookEntry>> GetLogBook(Guid pilotId)
        {
            return _flightRepo.GetLogBook(pilotId);
        }

    }
}
