using GliderView.API.Models;
using GliderView.Service;
using GliderView.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using System.Security.Claims;

namespace GliderView.API.Controllers
{
    [Authorize]
    [Route("users")]
    public class UserController : Controller
    {
        private readonly UserService _service;
        private readonly ILogger<UserController> _logger;
        private readonly TokenGenerator _tokenGenerator;
        private readonly TokenValidator _tokenValidator;

        private readonly bool _isDev;

        private const string REFRESH_TOKEN_COOKIE = "X-Refresh-Token";

        public UserController(
            UserService service,
            ILogger<UserController> logger,
            TokenGenerator tokenGenerator,
            TokenValidator tokenValidator,
            IConfiguration config
        )
        {
            _service = service;
            _logger = logger;
            _tokenGenerator = tokenGenerator;
            _tokenValidator = tokenValidator;

            _isDev = config.IsDevelopment();
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> LogIn([FromBody] LoginDto login)
        {
            if (login == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            User? user = await _service.ValidateUsernameAndPassword(login.Email!, login.Password!);

            if (user == null)
                return Unauthorized();

            AddRefreshToken(
                _tokenGenerator.GenerateRefreshToken(user)
            );

            Token token = _tokenGenerator.GenerateAuthToken(user, Scopes.GetScopesForRole(user.Role));

            var userDto = new
            {
                User = user,
                Token = token,
                Scopes = Scopes.GetScopesForRole(user.Role)
            };
            return Ok(userDto);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            string? refreshToken = GetRefreshToken();

            if (String.IsNullOrEmpty(refreshToken))
            {
                _logger.LogDebug("Missing refresh token");
                return Unauthorized();
            }

            if (!_tokenValidator.TryValidateToken(refreshToken, out ClaimsPrincipal? claims))
                return Unauthorized();

            Guid userId = _tokenValidator.GetUserId(claims!);

            User user = await _service.GetUser(userId);

            AddRefreshToken(
                _tokenGenerator.GenerateRefreshToken(user)
            );

            Token token = _tokenGenerator.GenerateAuthToken(user, Scopes.GetScopesForRole(user.Role));

            var userDto = new
            {
                User = user,
                Token = token,
                Scopes = Scopes.GetScopesForRole(user.Role)
            };
            return Ok(userDto);
        }

        private string? GetRefreshToken()
        {
            return Request.Cookies[REFRESH_TOKEN_COOKIE];
        }

        private void AddRefreshToken(Token token)
        {
            Response.Cookies.Append(
                REFRESH_TOKEN_COOKIE,
                token.Value,
                new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = !_isDev,
                    SameSite = SameSiteMode.Strict,
                    Expires = token.ValidTo
                }
            );
        }

        [HttpPost]
        [Authorize(Scopes.CreateUser)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            User user = await _service.CreateUser(dto.Email!, dto.Name!, dto.Role!.Value);

            return Ok(user);
        }

        [HttpDelete("{userId}")]
        [Authorize(Scopes.CreateUser)]
        public async Task<IActionResult> DeleteUser([FromRoute] Guid userId)
        {
            await _service.DeleteUser(userId);

            return NoContent();
        }

        [HttpGet]
        [Authorize(Scopes.ViewAllUsers)]
        public async Task<IActionResult> GetAll()
        {
            IEnumerable<User> users = await _service.GetUsers();

            return Ok(users);
        }

        [HttpGet("{userId}/invitation")]
        [Authorize(Scopes.CreateUser)]
        public async Task<IActionResult> GetInvitationToken([FromRoute] Guid? userId)
        {
            if (userId == null)
                return BadRequest();

            Invitation invite = await _service.GetNewInvitation(userId.Value);

            return Ok(invite);
        }

        [AllowAnonymous]
        [HttpPost("validate-invitation")]
        public async Task<IActionResult> ValidateInvitation([FromBody] ValidateInvitationDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            bool isValid = await _service.ValidateInvitation(dto.Email!, dto.Token!);

            if (isValid)
                return Ok();
            else
                return Unauthorized();
        }

        [AllowAnonymous]
        [HttpPost("onboarding")]
        public async Task<IActionResult> BuildUser([FromBody] BuildUserDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            var invite = new Invitation()
            {
                Token = dto.Token!
            };

            User user;
            try
            {
                user = await _service.BuildUser(invite, dto.Email!, dto.Password!);
            }
            catch (InvalidOperationException)
            {
                return Unauthorized();
            }

            // TODO: Add refresh token

            var userDto = new
            {
                User = user,
                Token = _tokenGenerator.GenerateAuthToken(user)
            };
            return Ok(userDto);
        }

        [Authorize(Scopes.CreateUser)]
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            await _service.UpdateUser(user);

            var updatedUser = await _service.GetUser(user.UserId);

            return Ok(updatedUser);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public IActionResult LogOut()
        {
            if (GetRefreshToken() != null)
                Response.Cookies.Delete(REFRESH_TOKEN_COOKIE);

            return Ok();
        }

        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId = User.GetUserId()!.Value;

            bool passwordCorrect = await _service.UpdatePassword(userId, dto.CurrentPassword, dto.NewPassword);

            if (!passwordCorrect)
                return Unauthorized();

            return Ok();
        }

        [HttpGet("{userId}/logbook")]
        public async Task<IActionResult> GetLogbook([FromRoute] Guid? userId)
        {
            // TODO: Authorize this user to this UserId

            userId = User.GetUserId()!.Value;

            List<LogBookEntry> logEntries = await _service.GetLogBook(userId.Value);

            return Ok(logEntries);
        }

        
    }
}
