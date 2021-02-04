using System;
using System.Collections.Generic;
using System.Text;

namespace WarSimulator
{
    class Simulator
    {
        private int gameCount;
        private Random random = new Random();

        //Game variables
        private byte[] p1Hand;
        private byte[] p2Hand;
        public Simulator(int gameCount)
        {
            this.gameCount = gameCount;
        }
        public void Simulate()
        {
            for (int g = 0; g < gameCount; g++)
            {
                InitGame();
                Console.WriteLine("[{0}]", string.Join(", ", p1Hand));
                Console.WriteLine("[{0}]", string.Join(", ", p2Hand));
            }
        }

        private void InitGame()
        {
            p1Hand = new byte[52];
            p2Hand = new byte[52];

            byte[] deck = new byte[52];
            int c = 0;
            //Fill deck with correct value cards
            for (byte s = 0; s < 4; s++)
            {
                for (byte n = 1; n < 14; n++)
                {
                    deck[c++] = n;
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
            for (int i = 0; i < 26; i++)
            {
                p1Hand[i] = deck[i];
                p2Hand[i] = deck[26 + i];
            }
        }
    }
}
