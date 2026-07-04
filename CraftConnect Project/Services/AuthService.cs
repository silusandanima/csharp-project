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
            users.Add(new User
            {
                Username = "admin",
                Password = "1234"
            });
        }

        public LoginResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "Username and Password are required",
                    Username = string.Empty
                };
            }

            foreach (User user in users)
            {
                if (user.Username == username &&
                    user.Password == password)
                {
                    return new LoginResult
                    {
                        IsSuccess = true,
                        Message = "Login Successful",
                        Username = user.Username
                    };
                }
            }

            return new LoginResult
            {
                IsSuccess = false,
                Message = "Invalid Username or Password",
                Username = string.Empty
            };
        }
    }
}