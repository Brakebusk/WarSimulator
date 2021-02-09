using System;
using System.Collections.Generic;

namespace WarSimulator
{
    class Simulator
    {
        //Simulator constants
        private int reportRate;
        private bool verbose;
        private int gameCount;
        private int playerCount;
        private int suits; //Default 4
        private int cardsPerSuit; //Default 13 cards per suit
        private int numCards; //suits*cardsPerSuit-(suits*cardsPerSuit % playerCount)
        private Random random = new Random();

        //Simlation statistics
        private int[] gameLengths;


        //Game variables
        List<Player> players;
        int remainingPlayers;

        public Simulator(int playerCount, int gameCount, int suits=4, int cardsPerSuit=13, bool verbose=false)
        {
            this.playerCount = playerCount;
            this.gameCount = gameCount;
            this.gameLengths = new int[gameCount];
            this.suits = suits;
            this.cardsPerSuit = cardsPerSuit;
            this.verbose = verbose;
            if (gameCount >= 100)
                this.reportRate = gameCount / 100;
            else this.reportRate = 1;

            if (playerCount > suits*cardsPerSuit)
            {
                throw new NotEnoughCardsInDeckException("There cannot be more players than cards in the deck");
            }
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
            int roundsWithoutWinner = 0; //A game may end up in an unresolvable state depending on the total number of cards compared to players
            while (remainingPlayers > 1)
            {
                if (verbose) Console.WriteLine("New round");
                rounds++;

                //Start by drawing cards, players without anyting to draw are eliminated
                Dictionary<int, List<Player>> drawnCards = new Dictionary<int, List<Player>>();
                for (int i = 0; i < remainingPlayers; i++)
                {
                    int card = players[i].Draw();
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
                    } else
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
                    if (verbose) Console.WriteLine("Round winner: {0}", winner.id);
                    roundsWithoutWinner = 0;
                    winner.AddToSpoils(spoils);
                } else
                {
                    if (++roundsWithoutWinner > 100)
                    {
                        //Abort game. Likely cannot be resolved
                        return 0;
                    }
                    //No winner could be determined. Will happen if all remaining players end up in wars that cannot be resolved
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
            System.Diagnostics.Debug.Assert(players[0].CardCount() == numCards);
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
            Dictionary<int, List<Player>> champions = new Dictionary<int, List<Player>>();
            List<int>[] stakes = new List<int>[warPlayers.Count];
            for (int i = 0; i < warPlayers.Count; i++)
                stakes[i] = new List<int>();
            
            for (int p = 0; p < warPlayers.Count; p++)
            {
                int champion = warPlayers[p].Draw();
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

            List<int> deck = new List<int>(suits * cardsPerSuit);
            int skip = (suits * cardsPerSuit) % playerCount;
            //Fill deck with correct value cards
            for (int s = 0; s < suits; s++)
            {
                for (int n = 1; n < cardsPerSuit + 1; n++)
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
            for (int i = deck.Count-1; i > 0; i--)
            {
                j = random.Next(i);
                int t = deck[i];
                deck[i] = deck[j];
                deck[j] = t;
            }
            numCards = deck.Count;

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

    public class NotEnoughCardsInDeckException : Exception
    {
        public NotEnoughCardsInDeckException()
        {

        }

        public NotEnoughCardsInDeckException(string message) : base(message)
        {
        }
    }
}
