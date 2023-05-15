using Dapper;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Data
{
    public class UserRepository : SqlRepository, IUserRepository
    {
        public UserRepository(string connectionString)
            : base(connectionString)
        {

        }

        public async Task<User?> GetUser(Guid userId)
        {
            const string sql = @"
SELECT
    U.UserGuid AS UserId
    , U.Name
    , U.Email
    , U.Role
    , U.HashedPassword
    , U.FailedLoginAttempts
    , U.IsLocked
FROM dbo.[User] U
WHERE U.UserGuid = @userId
    AND U.IsDeleted = 0;
";
            using (var con = GetOpenConnection())
            {
                return await con.QueryFirstOrDefaultAsync<User>(sql, new { userId });
            }
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            const string sql = @"
SELECT
    U.UserGuid AS UserId
    , U.Email AS Email
    , U.Name
    , U.Role
    , U.HashedPassword
    , U.FailedLoginAttempts
    , U.IsLocked
FROM dbo.[User] U
WHERE U.Email = @email
    AND U.IsDeleted = 0;
";
            using (var con = GetOpenConnection())
            {
                return await con.QueryFirstOrDefaultAsync<User>(sql, new { email });
            }
        }
        public async Task<IEnumerable<User>> GetUsers()
        {
            const string sql = @"
SELECT
    U.UserGuid AS UserId
    , U.Email AS Email
    , U.Name
    , U.Role
    , U.HashedPassword
    , U.FailedLoginAttempts
    , U.IsLocked
FROM dbo.[User] U
WHERE U.IsDeleted = 0;
";
            using (var con = GetOpenConnection())
                return (await con.QueryAsync<User>(sql))
                    .ToList();
        }

        public async Task CreateUser(User user)
        {
            const string sql = @"
IF EXISTS (
    SELECT 1
    FROM [User]
    WHERE Email = @email
        AND IsDeleted = 0
)
    THROW 51000, 'User already exists with that email.', 1;

INSERT INTO [User] (
    UserGuid
    , Email
    , Name
    , Role
)
VALUES (
    @userId
    , @email
    , @name
    , @role
)
";
            using (var con = GetOpenConnection())
            {
                var args = new
                {
                    user.UserId,
                    user.Role,
                    user.Email,
                    user.Name
                };
                await con.ExecuteAsync(sql, args);
            }
        }

        public async Task CreateInvitation(Invitation invite)
        {
            const string sql = @"
DECLARE @userId INT = (
    SELECT
        U.UserId
    FROM [User] U
    WHERE U.UserGuid = @userGuid
);

BEGIN TRAN

BEGIN TRY

    UPDATE dbo.Invitation
        SET IsDeleted = 0
    WHERE UserId = @userId
        AND IsDeleted = 0;

    INSERT INTO dbo.Invitation (
        UserId
        , Token
        , ExpirationDate
    )
    VALUES (
        @userId
        , @token
        , @expirationDate
    )

    COMMIT TRAN;

END TRY
BEGIN CATCH
    ROLLBACK TRAN;
    THROW;
END CATCH
";
            using (var con = GetOpenConnection())
            {
                var args = new
                {
                    userGuid = invite.UserId,
                    invite.Token,
                    invite.ExpirationDate
                };
                await con.ExecuteAsync(sql, args);
            }
        }

        public async Task<Invitation?> GetInvitationByToken(string token)
        {
            const string sql = @"
SELECT
    U.UserGuid as UserId
    , I.Token
    , I.ExpirationDate
FROM dbo.Invitation I
    JOIN dbo.[User] U
        ON I.UserId = U.UserId
WHERE I.Token = @token
    AND I.IsDeleted = 0
    AND U.IsDeleted = 0;
";
            using (var con = GetOpenConnection())
            {
                return await con.QueryFirstOrDefaultAsync<Invitation>(sql, new { token });
            }
        }

        public async Task UpdateFailedLoginAttempts(User user)
        {
            const string sql = @"
UPDATE dbo.[User]
    SET FailedLoginAttempts = @failedLoginAttempts
        , IsLocked = @isLockedOut
        , ModifiedDate = CURRENT_TIMESTAMP
    WHERE UserGuid = @userId
        AND IsDeleted = 0;
";
            using (var con = GetOpenConnection())
            {
                await con.ExecuteAsync(sql, new { user.UserId, user.IsLockedOut, user.FailedLoginAttempts });
            }
        }

        public async Task UpdatePassword(User user)
        {
            const string sql = @"
UPDATE dbo.[User]
    SET HashedPassword = @hashedPassword
        , FailedLoginAttempts = 0
        , IsLocked = 0
        , ModifiedDate = CURRENT_TIMESTAMP
WHERE UserGuid = @userId
    AND IsDeleted = 0;
";
            using (var con = GetOpenConnection())
            {
                await con.ExecuteAsync(sql, new { user.UserId, user.HashedPassword });
            }
        }

        public async Task DeleteInvitation(string token)
        {
            const string sql = @"
UPDATE dbo.Invitation
    SET IsDeleted = 1
WHERE Token = @token
    AND IsDeleted = 0;
";
            using (var con = GetOpenConnection())
            {
                await con.ExecuteAsync(sql, new { token });
            }
        }

        public async Task DeleteUser(Guid userId)
        {
            const string sql = @"
UPDATE dbo.[User]
    SET IsDeleted = 1
    , ModifiedDate = CURRENT_TIMESTAMP
WHERE UserGuid = @userId
    AND IsDeleted = 0;
";
            using (var con = GetOpenConnection())
            {
                await con.ExecuteAsync(sql, new { userId });
            }
        }
    }
}
