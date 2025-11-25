using System;           // Import basic system functionalities like Console, Math, etc.
using FinalProject;     // Import the WindowEngine namespace, which contains Game class and other related classes

namespace FinalProject
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Game game = new Game())
            {
                game.Run();
            }
        }
    }
}