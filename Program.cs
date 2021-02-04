using System;

namespace WarSimulator
{
    class Program
    {
        static int gameCount;
        static void Main(string[] args)
        {
            try
            {
                gameCount = int.Parse(args[0]);
            } catch (Exception e)
            {
                if (e is FormatException || e is IndexOutOfRangeException)
                    Usage();
                throw;
            }

            Console.WriteLine("Starting simulation of {0} games.", gameCount);
            Simulator simulator = new Simulator(gameCount);
            simulator.Simulate();
        }

        static void Usage()
        {
            Console.WriteLine("Usage: WarSimulator.exe <n games>");
            System.Environment.Exit(1);
        }
    }
}
