using CraftConnect_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftConnect_Project.Services
{
    public class AuthService
    {
        private readonly List<User> users = new List<User>();

        public AuthService()
        {
            users.Add(new User("admin", "1234"));
        }

        public LoginResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                return new LoginResult(
                    false,
                    "Username and Password are required",
                    string.Empty
                );
            }

            foreach (User user in users)
            {
                if (user.GetUsername() == username &&
                    user.CheckPassword(password))
                {
                    return new LoginResult(
                        true,
                        "Login Successful",
                        user.GetUsername()
                    );
                }
            }

            return new LoginResult(
                false,
                "Invalid Username or Password",
                string.Empty
            );
        }
    }
}