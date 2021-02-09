using System;
using System.Collections.Generic;
using System.Threading;

namespace WarSimulator
{
    class Simulator
    {
        //Simulator constants
        private int REPORTRATE;
        private bool VERBOSE = false;
        private int GAMECOUNT;
        private int PLAYERCOUNT;
        private int SUITS; //Default 4
        private int CARDSPERSUIT; //Default 13 cards per suit

        //Simlation statistics
        private int[] gameLengths;

        public Simulator(int playercount, int gamecount, int suits=4, int cardspersuit=13)
        {
            //Initialize constants
            PLAYERCOUNT = playercount;
            GAMECOUNT = gamecount;
            gameLengths = new int[GAMECOUNT];
            SUITS = suits;
            CARDSPERSUIT = cardspersuit;
            if (GAMECOUNT == 1) VERBOSE = true;
            if (GAMECOUNT >= 100)
                REPORTRATE = GAMECOUNT / 100;
            else REPORTRATE = 1;

            if (PLAYERCOUNT > SUITS*CARDSPERSUIT)
            {
                throw new NotEnoughCardsInDeckException("There cannot be more players than cards in the deck");
            }
            if (PLAYERCOUNT < 2)
            {
                throw new InvalidPlayerCountException("There must be at least 2 players");
            }
        }
        public void Simulate()
        {
            if (GAMECOUNT < 100)
            {
                Console.WriteLine("Using 1 thread");
                SimulationWorker worker = new SimulationWorker(PLAYERCOUNT, GAMECOUNT, SUITS, CARDSPERSUIT, gameLengths, 0);
                Thread thread = new Thread(new ThreadStart(worker.Simulate));
                thread.Start();
                thread.Join();
            } else
            {
                int threadCount = Environment.ProcessorCount;
                Thread[] workers = new Thread[threadCount];
                int sliceSize = GAMECOUNT / threadCount;
                Console.WriteLine("Using {0} threads", threadCount);
                for (int i = 0; i < threadCount; i++)
                {
                    int games = sliceSize;
                    if (i == threadCount - 1) games = GAMECOUNT - (i * sliceSize);
                    SimulationWorker worker = new SimulationWorker(PLAYERCOUNT, games, SUITS, CARDSPERSUIT, gameLengths, i * sliceSize);
                    Thread thread = new Thread(new ThreadStart(worker.Simulate));
                    workers[i] = thread;
                    thread.Start();
                }

                for (int i = 0; i < threadCount; i++)
                {
                    workers[i].Join();
                }
                Console.WriteLine("Simulation completed");
            }
        }

        public void PrintStatistics()
        {
            Array.Sort(gameLengths);
            int mean = 0;
            for (int i = 0; i < GAMECOUNT; i++)
                mean += gameLengths[i];
            mean /= GAMECOUNT;
            int median = 0;
            if (GAMECOUNT % 2 == 0)
            {
                median = (gameLengths[GAMECOUNT / 2] + gameLengths[(GAMECOUNT / 2) - 1]) / 2;
            }
            else
                median = gameLengths[GAMECOUNT / 2];
            Console.WriteLine("Average game length of {0} simulated games: {1} (mean: {2})", GAMECOUNT, mean, median);

            int shortest = 0;
            foreach (var l in gameLengths)
            {
                if (l > 0)
                {
                    shortest = l;
                    break;
                }
            }
            int longest = gameLengths[GAMECOUNT - 1];

            Console.WriteLine("Shortest game: {0} rounds", shortest);
            Console.WriteLine("Longest game: {0} rounds", longest);
        }

        private class SimulationWorker
        {
            int PLAYERCOUNT;
            int SUITS;
            int CARDSPERSUIT;
            int NUMCARDS;
            int GAMES;
            bool VERBOSE = false;
            Random random = new Random();

            int[] gameLengths;
            int sliceStart;

            //Game variables
            List<Player> players;
            int remainingPlayers;

            public SimulationWorker(int playercount, int games, int suits, int cardspersuit, int[] gameLengths, int sliceStart)
            {
                PLAYERCOUNT = playercount;
                SUITS = suits;
                CARDSPERSUIT = cardspersuit;
                GAMES = games;

                this.gameLengths = gameLengths;
                this.sliceStart = sliceStart;

            }
            public void Simulate()
            {
                for (int g = 0; g < GAMES; g++)
                {
                    if (VERBOSE) Console.WriteLine("Simulating new game");
                    InitGame();
                    gameLengths[sliceStart + g] = SimulateGame();
                    if (VERBOSE) Console.WriteLine("Game lasted {0} rounds", gameLengths[g]);
                }
            }

            private int SimulateGame()
            {
                //Hands have already been dealt, run game until one wins
                int rounds = 0;
                int roundsWithoutWinner = 0; //A game may end up in an unresolvable state depending on the total number of cards compared to players
                while (remainingPlayers > 1)
                {
                    if (VERBOSE) Console.WriteLine("New round");
                    rounds++;

                    //Start by drawing cards, players without anyting to draw are eliminated
                    Dictionary<int, List<Player>> drawnCards = new Dictionary<int, List<Player>>();
                    for (int i = 0; i < remainingPlayers; i++)
                    {
                        int card = players[i].Draw();
                        if (VERBOSE) Console.WriteLine("Player {0} ({1} remaining) drew {2}", players[i].id, players[i].CardCount(), card);
                        if (card == 0)
                        {
                            if (VERBOSE) Console.WriteLine("Player {0} out", players[i].id);
                            players.RemoveAt(i--);
                            remainingPlayers--;
                        }
                        else
                        {
                            List<Player> same;
                            bool found = drawnCards.TryGetValue(card, out same);
                            if (!found) drawnCards.Add(card, new List<Player>());
                            drawnCards[card].Add(players[i]);
                        }
                    }

                    //Find largest winner and handle any wars if they are encountered
                    Player winner = null;
                    int largest = 0;
                    List<int> spoils = new List<int>();
                    foreach (KeyValuePair<int, List<Player>> kvp in drawnCards)
                    {
                        if (kvp.Value.Count == 1)
                        {
                            spoils.Add(kvp.Key);
                            if (kvp.Key > largest)
                            {
                                largest = kvp.Key;
                                winner = kvp.Value[0];
                            }
                        }
                        else
                        {
                            //Found war between kvp.Value.Count number of players
                            for (int p = 0; p < kvp.Value.Count; p++)
                                spoils.Add(kvp.Key);
                            Player warWinner = War(kvp.Value);

                            if (warWinner != null && kvp.Key > largest)
                            {
                                winner = warWinner;
                                largest = kvp.Key;
                            }
                        }
                    }
                    if (winner != null)
                    {
                        if (VERBOSE) Console.WriteLine("Round winner: {0}", winner.id);
                        roundsWithoutWinner = 0;
                        winner.AddToSpoils(spoils);
                    }
                    else
                    {
                        //No round winner could be determined. Will happen if all remaining players end up in wars that cannot be resolved
                        if (++roundsWithoutWinner > 100)
                        {
                            //Abort game. Likely cannot be resolved
                            return 0;
                        }
                        foreach (KeyValuePair<int, List<Player>> kvp in drawnCards)
                        {
                            //Return drawn cards to players
                            for (int p = 0; p < kvp.Value.Count; p++)
                            {
                                kvp.Value[p].AddToSpoils(kvp.Key);
                            }
                        }
                    }

                }
                System.Diagnostics.Debug.Assert(players[0].CardCount() == NUMCARDS);
                if (VERBOSE) Console.WriteLine("Player {0} won the game", players[0].id);
                return rounds;
            }

            private Player War(List<Player> warPlayers)
            {
                if (VERBOSE)
                {
                    Console.Write("New war between: [");
                    for (int i = 0; i < warPlayers.Count; i++)
                    {
                        if (i > 0) Console.Write(", ");
                        Console.Write(warPlayers[i].id);
                    }
                    Console.WriteLine("]");
                }
                Dictionary<int, List<Player>> champions = new Dictionary<int, List<Player>>();
                List<int>[] stakes = new List<int>[warPlayers.Count];
                for (int i = 0; i < warPlayers.Count; i++)
                    stakes[i] = new List<int>();

                for (int p = 0; p < warPlayers.Count; p++)
                {
                    int champion = warPlayers[p].Draw();
                    if (VERBOSE) Console.WriteLine("Player {0} has champion {1}", warPlayers[p].id, champion);
                    if (champion > 0)
                    {
                        List<Player> same;
                        bool found = champions.TryGetValue(champion, out same);
                        if (!found) champions.Add(champion, new List<Player>());
                        champions[champion].Add(warPlayers[p]);
                        stakes[p].Add(champion);
                    }
                    else
                    {
                        //Player has no champion
                        continue;
                    }
                    //Draw stake
                    for (int s = 0; s < 3; s++)
                    {
                        int stake = warPlayers[p].Draw();
                        if (stake > 0)
                        {
                            stakes[p].Add(stake);
                        }
                        else break;
                    }
                }

                //Figure out winner
                //If there is another war, the winner of that war will take the spoils,
                //unless there are more than 2 players and two have another war because of mathcing cards with value X
                //and the third player has a card with value Y > X. In this case, the third player will take the spoils of
                //this war, while the others fight the next war independently
                Player warWinner = null;

                int largest = 0;
                foreach (KeyValuePair<int, List<Player>> kvp in champions)
                {
                    if (kvp.Value.Count > 1)
                    {
                        //Nested war
                        Player nestedWinner = War(kvp.Value);
                        if (nestedWinner != null && kvp.Key > largest)
                        {
                            largest = kvp.Key;
                            warWinner = nestedWinner;
                        }
                    }
                    else if (kvp.Key > largest)
                    {
                        largest = kvp.Key;
                        warWinner = kvp.Value[0];
                    }
                }
                if (warWinner != null)
                {
                    for (int i = 0; i < warPlayers.Count; i++)
                        warWinner.AddToSpoils(stakes[i]);
                    if (VERBOSE) Console.WriteLine("Player {0} won the war", warWinner.id);
                }
                else
                {
                    for (int i = 0; i < warPlayers.Count; i++)
                        warPlayers[i].AddToSpoils(stakes[i]);
                }
                return warWinner;
            }

            private void InitGame()
            {
                players = new List<Player>(PLAYERCOUNT);
                for (int i = 0; i < PLAYERCOUNT; i++)
                    players.Add(new Player(i));

                remainingPlayers = PLAYERCOUNT;

                List<int> deck = new List<int>(SUITS * CARDSPERSUIT);
                int skip = (SUITS * CARDSPERSUIT) % PLAYERCOUNT;
                //Fill deck with correct value cards
                for (int s = 0; s < SUITS; s++)
                {
                    for (int n = 1; n < CARDSPERSUIT + 1; n++)
                    {
                        if (skip > 0)
                        {
                            skip--;
                            continue;
                        }
                        deck.Add(n);
                    }
                }

                //Shuffle (fisher-yates)
                int j;
                for (int i = deck.Count - 1; i > 0; i--)
                {
                    j = random.Next(i);
                    int t = deck[i];
                    deck[i] = deck[j];
                    deck[j] = t;
                }
                NUMCARDS = deck.Count;

                //Deal
                int p;
                for (int i = 0; i < deck.Count; i += PLAYERCOUNT)
                {
                    for (p = 0; p < PLAYERCOUNT; p++)
                    {
                        players[p].hand.Push(deck[i + p]);
                        players[p].initialScore += deck[i + p];
                    }
                }
            }

        }
    }

    public class NotEnoughCardsInDeckException : Exception
    {
        public NotEnoughCardsInDeckException()
        {

        }

        public NotEnoughCardsInDeckException(string message) : base(message)
        {
        }
    }

    public class InvalidPlayerCountException : Exception
    {
        public InvalidPlayerCountException()
        {

        }

        public InvalidPlayerCountException(string message) : base(message)
        {

        }
    }
}
