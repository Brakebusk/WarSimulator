using System;

namespace WarSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            int gameCount, playerCount;
            try
            {
                playerCount = int.Parse(args[0]);
                gameCount = int.Parse(args[1]);
            } catch (Exception e)
            {
                if (e is FormatException || e is IndexOutOfRangeException)
                    Usage();
                throw;
            }

            Console.WriteLine("Starting simulation of {0} players playing {1} games.", playerCount, gameCount);
            Simulator simulator = new Simulator(playerCount, gameCount);
            simulator.Simulate();
        }

        static void Usage()
        {
            Console.WriteLine("Usage: WarSimulator.exe <n players> <n games>");
            System.Environment.Exit(1);
        }
    }
}
