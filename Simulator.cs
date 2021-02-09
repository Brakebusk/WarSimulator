using System;
using System.Collections.Generic;
using System.Text;

namespace WarSimulator
{
    class Simulator
    {
        private int reportRate;
        private bool verbose = false;
        private int gameCount;
        private int playerCount;
        private Random random = new Random();

        //Simlation statistics
        private int[] gameLengths;


        //Game variables
        List<Player> players;
        int remainingPlayers;

        public Simulator(int playerCount, int gameCount, bool verbose=false)
        {
            this.playerCount = playerCount;
            this.gameCount = gameCount;
            this.gameLengths = new int[gameCount];
            this.verbose = verbose;
            this.reportRate = gameCount / 100;
        }
        public void Simulate()
        {
            int p = 0;
            for (int g = 0; g < gameCount; g++)
            {
                if (g % reportRate == 0) Console.WriteLine("{0}% progress", p++);
                if (verbose) Console.WriteLine("Simulating new game");
                InitGame();
                gameLengths[g] = SimulateGame();
                if (verbose) Console.WriteLine("Game lasted {0} rounds", gameLengths[g]);
            }
        }

        public void PrintStatistics()
        {
            Array.Sort(gameLengths);
            for (int g = 0; g < gameLengths.Length; g++)
            {
                int sel = gameLengths[g];
                int sum = 1;
                while (g+sum < gameLengths.Length && gameLengths[g+sum] == sel)
                {
                    sum++;
                }
                Console.WriteLine("{0}: {1}", sel, sum);
                g += sum-1;
            }
        }

        private int SimulateGame()
        {
            //Hands have already been dealt, run game until one wins
            int rounds = 0;
            while (remainingPlayers > 1)
            {
                if (verbose) Console.WriteLine("New round");
                rounds++;

                //Start by drawing cards, players without anyting to draw are eliminated
                Dictionary<byte, List<Player>> drawnCards = new Dictionary<byte, List<Player>>();
                for (int i = 0; i < remainingPlayers; i++)
                {
                    byte card = players[i].Draw();
                    if (verbose) Console.WriteLine("Player {0} ({1} remaining) drew {2}", players[i].id, players[i].CardCount(), card);
                    if (card == 0)
                    {
                        if (verbose) Console.WriteLine("Player {0} out", players[i].id);
                        players.RemoveAt(i--);
                        remainingPlayers--;
                    } else
                    {
                        List<Player> same;
                        bool found = drawnCards.TryGetValue(card, out same);
                        if (!found) drawnCards.Add(card, new List<Player>());
                        drawnCards[card].Add(players[i]);
                    }
                }

                //Find largest no-duplicate winner and handle any wars if they are encountered
                Player winner = null;
                byte largest = 0;
                List<byte> spoils = new List<byte>();
                foreach (KeyValuePair<byte, List<Player>> kvp in drawnCards)
                {
                    if (kvp.Value.Count == 1)
                    {
                        spoils.Add(kvp.Key);
                        if (kvp.Key > largest)
                        {
                            largest = kvp.Key;
                            winner = kvp.Value[0];
                        }
                    } else
                    {
                        //Found war between kvp.Value.Count number of players
                        Player warWinner = War(kvp.Value);
                        if (warWinner == null)
                        {
                            //No players could play the war, just split the spoils
                            List<byte> ret = new List<byte>(1);
                            ret.Add(kvp.Key);
                            for (int p = 0; p < kvp.Value.Count; p++)
                                kvp.Value[p].AddToSpoils(ret);
                        } else
                        {
                            //Give war winner the war starting cards
                            List<byte> warSpoils = new List<byte>(kvp.Value.Count);
                            for (int c = 0; c < kvp.Value.Count; c++) warSpoils.Add(kvp.Key);
                            warWinner.AddToSpoils(warSpoils);
                        }
                    }
                }
                if (winner != null)
                {
                    if (verbose) Console.WriteLine("Round winner: {0}", winner.id);
                    winner.AddToSpoils(spoils);
                }

            }
            System.Diagnostics.Debug.Assert(players[0].CardCount() == 52);
            if (verbose) Console.WriteLine("Player {0} won the game", players[0].id);
            return rounds;
        }

        private Player War(List<Player> warPlayers)
        {
            if (verbose)
            {
                Console.Write("New war between: [");
                for (int i = 0; i < warPlayers.Count; i++)
                {
                    if (i > 0) Console.Write(", ");
                    Console.Write(warPlayers[i].id);
                }
                Console.WriteLine("]");
            }
            Dictionary<byte, List<Player>> champions = new Dictionary<byte, List<Player>>();
            List<byte>[] stakes = new List<byte>[warPlayers.Count];
            for (int i = 0; i < warPlayers.Count; i++)
                stakes[i] = new List<byte>();
            
            for (int p = 0; p < warPlayers.Count; p++)
            {
                byte champion = warPlayers[p].Draw();
                if (verbose) Console.WriteLine("Player {0} has champion {1}", warPlayers[p].id, champion);
                if (champion > 0)
                {
                    List<Player> same;
                    bool found = champions.TryGetValue(champion, out same);
                    if (!found) champions.Add(champion, new List<Player>());
                    champions[champion].Add(warPlayers[p]);
                    stakes[p].Add(champion);
                } else
                {
                    //Player have no champion and is thus eliminated
                    //warPlayers.RemoveAt(p--);
                    continue;
                }
                //Draw stake
                for (int s = 0; s < 3; s++)
                {
                    byte stake = warPlayers[p].Draw();
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

            byte largest = 0;
            foreach (KeyValuePair<byte, List<Player>> kvp in champions)
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
                if (verbose) Console.WriteLine("Player {0} won the war", warWinner.id);
            } else
            {
                for (int i = 0; i < warPlayers.Count; i++)
                    warPlayers[i].AddToSpoils(stakes[i]);
            }
            return warWinner;
        }

        private void InitGame()
        {
            players = new List<Player>(playerCount);
            for (int i = 0; i < playerCount; i++)
                players.Add(new Player(i));

            remainingPlayers = playerCount;

            List<byte> deck = new List<byte>(52);
            int skip = 52 % playerCount;
            //Fill deck with correct value cards
            for (byte s = 0; s < 4; s++)
            {
                for (byte n = 1; n < 14; n++)
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
            for (int i = 51; i > 0; i--)
            {
                j = random.Next(i);
                byte t = deck[i];
                deck[i] = deck[j];
                deck[j] = t;
            }

            //Deal
            int p;
            for (int i = 0; i < deck.Count; i+=playerCount)
            {
                for (p = 0; p < playerCount; p++)
                {
                    players[p].hand.Push(deck[i + p]);
                    players[p].initialScore += deck[i + p];
                }
            }
        }
    }

    class Player {
        public int id;
        private Random random = new Random();
        public Stack<byte> hand = new Stack<byte>();
        public List<byte> spoils = new List<byte>();
        public int initialScore = 0;

        public Player(int id)
        {
            this.id = id;
        }

        public byte Draw()
        {
            //Draw a single cards from the hand, shuffle in spoils if needed
            if (hand.Count == 0)
            {
                if (spoils.Count == 0)
                    return 0;
                ShuffleInSpoils();
            }
            return hand.Pop();
        }

        private void ShuffleInSpoils()
        {
            //Hand is empty, shuffle spoils into hand
            int j;
            for (int i = spoils.Count-1; i > 0; i--)
            {
                j = random.Next(i);
                byte t = spoils[i];
                spoils[i] = spoils[j];
                spoils[j] = t;
            }
            for (int i = 0; i < spoils.Count; i++)
                hand.Push(spoils[i]);
            spoils.Clear();
        }

        public void AddToSpoils(List<byte> cards)
        {
            spoils.AddRange(cards);
        }

        public int CardCount()
        {
            return hand.Count + spoils.Count;
        }
    
    }
}
