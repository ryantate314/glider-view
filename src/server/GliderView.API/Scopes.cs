using GliderView.Service.Models;

namespace GliderView.API
{
    public static class Scopes
    {
        public const string CreateUser = "user:create";
        public const string ViewAllUsers = "user:viewall";
        public const string AssignPilots = "flight:assignPilot";
        public const string ManageFlights = "flight:manage";

        public static IReadOnlyList<string> AllScopes = new List<string>()
        {
            CreateUser,
            ViewAllUsers,
            AssignPilots,
            ManageFlights
        };

        public static class Roles
        {
            public static readonly IReadOnlyList<string> Admin = new string[]
            {
                CreateUser,
                ViewAllUsers,
                AssignPilots,
                ManageFlights
            };
        }

        public static IReadOnlyList<string> GetScopesForRole(char role)
        {
            if (role == User.ROLE_ADMIN)
            {
                return Roles.Admin;
            }
            return Array.Empty<string>();
        }

    }
}
