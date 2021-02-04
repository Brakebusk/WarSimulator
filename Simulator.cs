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
        private const ushort TARGETSCORE = 364; //[1..13]*4

        //Simlation statistics
        private int[] gameLengths;


        //Game variables
        Player[] players;

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
                InitGame();
                gameLengths[g] = SimulateGame();
            }
        }

        private int SimulateGame()
        {
            //Hands have already been dealt, run game until one wins
            int rounds = 0;
            bool existsWinner = false;
            while (!existsWinner)
            {
                rounds++;

                byte[] drawnCards = new byte[playerCount];
                for (int i = 0; i < playerCount; i++)
                {
                    try
                    {
                        drawnCards[i] = players[i].Draw();
                    } catch (RanOut e)
                    {

                    }
                }



            }
            return rounds;
        }

        private void InitGame()
        {
            players = new Player[playerCount];

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
                for (p = 0; i < playerCount; p++)
                {
                    players[p].hand.Push(deck[i + p]);
                }
            }
        }
    }

    class Player {
        private Random random = new Random();
        public Stack<byte> hand;
        public List<byte> spoils;
        public ushort score;

        public byte Draw()
        {
            //Draw a single cards from the hand, shuffle in spoils if needed
            if (hand.Count == 0)
            {
                if (spoils.Count == 0)
                    throw new RanOut();
                ShuffleInSpoils();
            }
            return hand.Pop();
        }

        private void ShuffleInSpoils()
        {
            //Hand is empty, shuffle spoils into hand
            int j;
            for (int i = spoils.Count; i > 0; i--)
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
    
    }

    public class RanOut : Exception
    {
        //Thrown when player should draw a card, but both hand and spoils pile is empty
        public RanOut() { }
    }
}
