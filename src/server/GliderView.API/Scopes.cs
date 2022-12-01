namespace GliderView.API
{
    public static class Scopes
    {
        public const string CreateUser = "user:create";

        public static class Roles
        {
            public static readonly IReadOnlyList<string> Admin = new string[]
            {
                CreateUser
            };
        }
       
    }
}
