using System;

namespace WarSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            int gameCount, playerCount, suits, cardsPerSuite;
            int threadCount = System.Environment.ProcessorCount;
            try
            {
                if (args[0] == "help") Help();
                playerCount = int.Parse(args[0]);
                gameCount = int.Parse(args[1]);
                
                //Parse optional arguments
                suits = 4;
                cardsPerSuite = 13;
                for (int a = 2; a < args.Length; a++)
                {
                    switch (args[a])
                    {
                        case "-s":
                            suits = int.Parse(args[++a]);
                            break;
                        case "-c":
                            cardsPerSuite = int.Parse(args[++a]);
                            break;
                        case "-t":
                            threadCount = int.Parse(args[++a]);
                            break;
                    }
                }
            } catch (Exception e)
            {
                if (e is FormatException || e is IndexOutOfRangeException)
                    Usage();
                throw;
            }


            Console.WriteLine("Starting simulation of {0} players playing {1} games using a deck with {2} suits and {3} cards per suit.", playerCount, gameCount, suits, cardsPerSuite);
            try
            {
                Simulator simulator = new Simulator(playerCount, gameCount, threadCount, suits, cardsPerSuite);
                simulator.Simulate();
                simulator.PrintStatistics();
            } catch (NotEnoughCardsInDeckException e)
            {
                Console.WriteLine("Failure: " + e.Message);
                System.Environment.Exit(1);
            } catch (InvalidPlayerCountException e)
            {
                Console.WriteLine("Failure: " + e.Message);
                System.Environment.Exit(1);
            }
        }

        static void Usage()
        {
            Console.WriteLine("Usage: WarSimulator.exe <n players> <n games> [-s <n>] [-c <n>] [-t <n>]");
            System.Environment.Exit(1);
        }

        static void Help()
        {
            string helpString = @"Usage: WarSimulator.exe <n players> <n games> [-s <n>] [-c <n>]
Optional arguments:
-s <n>: Sets number of suits in deck. Default 4.
-c <n>: Sets number of cards per suit in deck. Default 13.
-t <n>: Sets number of threads to use. Defaults to system logical core count.";
            Console.WriteLine(helpString);
            System.Environment.Exit(0);
        }
    }
}
