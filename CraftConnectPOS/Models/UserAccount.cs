using System.Collections.ObjectModel;

namespace CraftConnectPOS.Models
{
    public class UserAccount
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

        // Static session-only collection of user accounts for sharing state between Login and Signup.
        // Once Sanuk integrates Users into MockDataStore, this can easily redirect or merge.
        public static ObservableCollection<UserAccount> SessionUsers { get; } = new ObservableCollection<UserAccount>
        {
            new UserAccount { Username = "admin", Password = "demo123", Role = "Business Owner" }
        };
    }
}
