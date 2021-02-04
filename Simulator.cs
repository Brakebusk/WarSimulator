using System;
using System.Collections.Generic;
using System.Text;

namespace WarSimulator
{
    class Simulator
    {
        private int gameCount;
        private int playerCount;
        private Random random = new Random();

        //Simlation statistics
        private int[] gameLengths;


        //Game variables
        List<Player> players;
        int remainingPlayers;

        public Simulator(int playerCount, int gameCount)
        {
            this.playerCount = playerCount;
            this.gameCount = gameCount;
            this.gameLengths = new int[gameCount];
        }
        public void Simulate()
        {
            for (int g = 0; g < gameCount; g++)
            {
                Console.WriteLine("Simulating new game");
                InitGame();
                gameLengths[g] = SimulateGame();
                Console.WriteLine("Game lasted {0} rounds", gameLengths[g]);
            }
        }

        private int SimulateGame()
        {
            //Hands have already been dealt, run game until one wins
            int rounds = 0;
            while (remainingPlayers > 1)
            {
                Console.WriteLine("New round");
                rounds++;

                //Start by drawing cards, players without anyting to draw are eliminated
                Dictionary<byte, List<Player>> drawnCards = new Dictionary<byte, List<Player>>();
                for (int i = 0; i < remainingPlayers; i++)
                {
                    byte card = players[i].Draw();
                    if (card == 0)
                    {
                        Console.WriteLine("Player {0} out", players[i].id);
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

                //Find largest no-duplicate winner
                Player winner = null;
                byte largest = 0;
                List<byte> spoils = new List<byte>();
                foreach (KeyValuePair<byte, List<Player>> kvp in drawnCards)
                {
                    if (kvp.Value.Count == 1)
                    {
                        spoils.Add(kvp.Key);
                        if (kvp.Key > largest) largest = kvp.Key;
                        winner = kvp.Value[0];
                    }
                }
                if (winner != null) winner.AddToSpoils(spoils);

                    foreach (KeyValuePair<byte, List<Player>> kvp in drawnCards)
                {
                    //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                    Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value.Count);
                }

            }
            return rounds;
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
                    players[p].score += deck[i + p];
                }
            }
        }
    }

    class Player {
        public int id;
        private Random random = new Random();
        public Stack<byte> hand = new Stack<byte>();
        public List<byte> spoils = new List<byte>();
        public ushort score = 0;

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
    
    }
}
