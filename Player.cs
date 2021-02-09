using System;
using System.Collections.Generic;

class Player
{
    public int id;
    private Random random = new Random();
    public Stack<int> hand = new Stack<int>();
    public List<int> spoils = new List<int>();
    public int initialScore = 0;

    public Player(int id)
    {
        this.id = id;
    }

    public int Draw()
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
            int t = spoils[i];
            spoils[i] = spoils[j];
            spoils[j] = t;
        }
        for (int i = 0; i < spoils.Count; i++)
            hand.Push(spoils[i]);
        spoils.Clear();
    }

    public void AddToSpoils(List<int> cards)
    {
        spoils.AddRange(cards);
    }

    public void AddToSpoils(int card)
    {
        spoils.Add(card);
    }

    public int CardCount()
    {
        return hand.Count + spoils.Count;
    }

}