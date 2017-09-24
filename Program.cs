using System;

namespace Fencing
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Fencing game = new Fencing())
            {
                game.Run();
            }
        }
    }
#endif
}

