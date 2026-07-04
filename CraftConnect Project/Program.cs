using System;
using CraftConnect_Project.Models;
using CraftConnect_Project.Services;

namespace CraftConnect_Project
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AuthService authService = new AuthService();

            Console.WriteLine("=================================");
            Console.WriteLine(" CRAFTCONNECT LOGIN BACKEND ");
            Console.WriteLine("=================================");

            Console.Write("Username : ");
            string username = Console.ReadLine();

            Console.Write("Password : ");
            string password = Console.ReadLine();

            LoginResult result = authService.Login(username, password);

            Console.WriteLine();

            if (result.IsSuccess)
            {
                Console.WriteLine(result.Message);
                Console.WriteLine("Welcome " + result.Username);
                Console.WriteLine("Opening Dashboard...");
            }
            else
            {
                Console.WriteLine(result.Message);
            }

            Console.ReadLine();
        }
    }
}