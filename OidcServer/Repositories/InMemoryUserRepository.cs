using OidcServer.Models;

namespace OidcServer.Repositories
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _user = new List<User>()
        {
            new (){Name = "alice"},
            new (){Name = "bob"},
            new (){Name = "kien"}
        };

        public User? FindByUserName(string username)
        {
            return _user.FirstOrDefault(x => x.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }
}
