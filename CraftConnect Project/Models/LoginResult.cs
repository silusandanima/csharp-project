using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftConnect_Project.Models
{
    public class LoginResult
    {
        private bool isSuccess;
        private string message;
        private string username;

        public bool IsSuccess
        {
            get { return isSuccess; }
        }

        public string Message
        {
            get { return message; }
        }

        public string Username
        {
            get { return username; }
        }

        public LoginResult(bool isSuccess, string message, string username)
        {
            this.isSuccess = isSuccess;
            this.message = message;
            this.username = username;
        }
    }
}