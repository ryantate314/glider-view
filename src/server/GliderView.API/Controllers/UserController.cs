using GliderView.API.Models;
using GliderView.Service;
using GliderView.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace GliderView.API.Controllers
{
    [Authorize]
    [Route("users")]
    public class UserController : Controller
    {
        private readonly UserService _service;
        private readonly ILogger<UserController> _logger;
        private readonly TokenGenerator _tokenGenerator;

        public UserController(UserService service, ILogger<UserController> logger, TokenGenerator tokenGenerator)
        {
            _service = service;
            _logger = logger;
            _tokenGenerator = tokenGenerator;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> LogIn([FromBody] LoginDto login)
        {
            if (login == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            User? user = await _service.ValidateUsernameAndPassword(login.EmailAddress!, login.Password!);

            if (user == null)
                return Unauthorized();

            // TODO: Add refresh token
            //Response.Cookies.Append("")

            var userDto = new
            {
                User = user,
                Token = _tokenGenerator.GenerateAuthToken(user)
            };
            return Ok(userDto);
        }

        [HttpPost]
        [Authorize(Scopes.CreateUser)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto user)
        {
            if (user == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            await _service.CreateUser(user.EmailAddress!, user.Name!, user.Role!.Value);

            return Ok();
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

            bool isValid = await _service.ValidateInvitation(dto.EmailAddress!, dto.Token!);

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
                user = await _service.BuildUser(invite, dto.EmailAddress!, dto.Password!);
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
    }
}
