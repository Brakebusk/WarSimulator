# WarSimulator

Simulates the card game War

## Running the simulation
```
Usage: WarSimulator.exe <n players> <n games> [-v] [-s <n>] [-c <n>]

Arguments:
  <n players> Number of players to simulate per game. Minimum 2
  <n games>   Number of games to simulate
  -v          Verbose flag. Prints detailed information when simulating each game
  -s <n>      Number of suits in a game's deck. Default 4
  -c <n>      Number of cards per suit. Default 13
  ```

## Game rules
- The deck is shuffled and each player is dealt an equal number of cards to their draw pile.
  - If the deck cannot be split evenly, the lowest card(s) are discarded so that it can be devided evenly.
- Each round, each player shows the top card of their draw pile.
  - The player with the highest card wins the round and wins all the other cards. Won cards are placed in the player's spoil heap.
  - If any cards are of equal value, "war" occurs between the players with the same value cards.
    - Each war participant puts fourth their three top cards as stake.
    - The fourth card is the champion and decides who win the war.
    - The victor gets all cards involved in the war.
    - There can be nested wars if any champions match.
  - If at any point a player's hand is empty, they must shuffle their spoil heap and place the cards in their draw pile.
    - If the spoil heap is also empty and the player is out of cards, they have lost
- If after a completed round one of the players possess all cards, they have won the game
