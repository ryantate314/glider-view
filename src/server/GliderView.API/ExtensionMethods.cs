using System.Security.Claims;

namespace GliderView.API
{
    public static class ExtensionMethods
    {
        public static bool IsDevelopment(this IConfiguration configuration)
        {
            return configuration["ASPNETCORE_ENVIRONMENT"] == "Development";
        }

        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            string? stringId = user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            return stringId == null
                ? null
                : Guid.Parse(stringId);
        }
    }
}
