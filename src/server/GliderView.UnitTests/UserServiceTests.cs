using Castle.Core.Logging;
using GliderView.Service;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.UnitTests
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserRepository> _fakeUserRepository;
        private Mock<IFlightRepository> _fakeFlightRepository;
        private Mock<ILogger<UserService>> _fakeLogger;
        private UserService _service;

        [SetUp]
        public void SetUp()
        {
            _fakeUserRepository = new Mock<IUserRepository>();
            _fakeFlightRepository = new Mock<IFlightRepository>();
            _fakeLogger = new Mock<ILogger<UserService>>();

            _service = new UserService(
                _fakeUserRepository.Object,
                _fakeLogger.Object,
                new PasswordHasher<User>(),
                _fakeFlightRepository.Object
            );
        }

        [Test]
        public async Task BuildUser_HappyPath()
        {
            string email = "ryan@test.com";
            string password = "Passw0rd";
            string token = "testtokentesttokentesttoken";
            Guid userId = Guid.NewGuid();

            var invite = new Invitation()
            {
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                Token = token
            };

            _fakeUserRepository.Setup(x => x.GetInvitationByToken(token))
                .ReturnsAsync(() => new Invitation()
                {
                    UserId = userId,
                    ExpirationDate = DateTime.UtcNow.AddDays(1),
                    Token = token
                });

            _fakeUserRepository.Setup(x => x.GetUser(userId))
                .ReturnsAsync(() => new User()
                {
                    UserId = userId,
                    Email = email
                });

            // Execute
            User user = await _service.BuildUser(invite, email, password);

            // Assert
            Assert.That(user, Is.Not.Null);
            Assert.That(user.HashedPassword, Is.Not.Null);
        }

        [Test]
        public async Task VerifyEmailAndPassword_Success()
        {
            string password = "Passw0rd";
            string email = "ryan@test.com";

            const string hash = "AQAAAAIAAYagAAAAEICvJ7VnBW9kGYj788VtRjvGJ7pg14WVD4r2jy+z9MGpc+L5S+LjAn2fZbM8pNHAoQ==";
            _fakeUserRepository.Setup(x => x.GetUserByEmail(email))
                .ReturnsAsync(() => new User()
                {
                    Email = email,
                    HashedPassword = hash
                });

            // Execute
            User? user = await _service.ValidateUsernameAndPassword(email, password);

            // Assert
            Assert.That(user, Is.Not.Null);
        }


        [Test]
        public async Task VerifyEmailAndPassword_LockedOut()
        {
            string password = "Passw0rd";
            string email = "ryan@test.com";

            const string hash = "AQAAAAIAAYagAAAAEICvJ7VnBW9kGYj788VtRjvGJ7pg14WVD4r2jy+z9MGpc+L5S+LjAn2fZbM8pNHAoQ==";
            _fakeUserRepository.Setup(x => x.GetUserByEmail(email))
                .ReturnsAsync(() => new User()
                {
                    Email = email,
                    HashedPassword = hash,
                    IsLockedOut = true
                });

            // Execute
            User? user = await _service.ValidateUsernameAndPassword(email, password);

            // Assert
            Assert.That(user, Is.Null);
        }
    }
}
