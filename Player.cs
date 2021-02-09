using System;
using System.Collections.Generic;

class Player
{
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
        for (int i = spoils.Count - 1; i > 0; i--)
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