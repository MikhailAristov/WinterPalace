# Winter Palace
*Winter Palace* is an unofficial video game clone of the 2012 bluffing/deduction card game *[Love Letter](https://www.alderac.com/tempest/love-letter/)* by Seji Kanai. If you are familiar with the card game, you will find the mechanics of *Winter Palace* essentially identical, because, to the best of my knowledge, game mechanics and ideas are not copyrightable. Kanai's original art, however, is, so my version is a complete reskin that ***does not contain any materials copyrighted by anyone but myself***. Consequently, while the original game was set in the Tempest universe, inspired by Western European royal courts, *Winter Palace* is set in St. Petersburg in the first half of the 18th century (hence the [title](https://en.wikipedia.org/wiki/Winter_Palace)).

You can download the [Windows installer](https://drive.google.com/open?id=1oHcPQ6AoOIXQUOGxQrMGoR9Ef1nuP0NK) for the game from Google Drive for free. If you like my game, please consider buying a copy of the original *Love Letter* from your friendly local gaming store. :-)

## Basics
While *Love Letter* was a competitive card game for two to four players, *Winter Palace* is a single-player video game that aims to simulate a four-player game of *Love Letter* with three artificially intelligent opponents (in fact, the entire project was essentially my exercise in game AI programming). Your goal is to knock all other players out of the game before the deck runs empty or, failing that, to have the highest-valued card in your hand when that happens.

At the start of the game, all 16 cards (see below) are shuffled into the deck on the left of the screen. Each of the four players draws a card at the start of the round, then a random player takes the first turn. On their turn, every player draws a card from the top of the deck and plays either it, or the one from their hand face up, resolving its effects (see below). The card they played then goes to the discard pile on the right, while the other one remains with the player. The next active player takes their turn the same way, and so on, until only one player is standing or until one last card remains in the deck.

If all other players are knocked out, the last one wins the round. If only one or no cards remain to be drawn from the deck, all remaining players reveal their hands, and the one with the highest-valued card takes the round. In the unlikely event of a tie, the player with the higher total value of drawn and discarded cards (which the game keeps track of) wins. The winner gains a victory token and has the first turn in the next round, which starts automatically. Unlike in the original card game, there is no limit on how many victory tokens can be gained.

## User Interface
The entire game is played with just the mouse and its left button. During other players' turns, you don't have to do anything except watching and analyzing their moves. On your turn, the game will automatically draw a second card for you, after which you must left-click on the one you want to play (if the card is highlighted red, you are forbidden from playing it by the rules!). If the card must be played against another player, you must then click on that player's face-down card. If your card is a Guard, you will be additionally prompted to select the card you think your target has in their hand. Be careful with your clicks: once you commit to a card, you cannot undo it!

You can quit the game at any time by left-clicking on the big "X" button in the top-right corner of the screen.

## Cards
Each card in the game has a numeric value from 1 to 8 and a special ability that may or may not target another player. The deck consists of 16 cards in total, so values 1 to 5 appear in it more than once. For history buffs out there, each card in *Winter Palace* also represents a historical persona of that period and place.

1. **Guard** (x5): When you play a Guard against another player and guess the card (value) they have in their hand correctly, they are knocked out of the round; nothing happens if you guess incorrectly. You may not guess "Guard", as that would be too easy.

  Portrait: Private Yekimenko of the [Semyonovsky Lifeguard Regiment](https://en.wikipedia.org/wiki/Semyonovsky_Regiment), by [Gerhardt Wilhelm von Reutern](https://en.wikipedia.org/wiki/Gerhardt_Wilhelm_von_Reutern) ([source](https://commons.wikimedia.org/wiki/File:Рядовой_лейб-гвардии_Семёновского_полка_Екименко,_1832_г.jpg)).

2. **Priest** (x2): When you play a Priest against another player, they reveal their hand to you (and only to you).

  Portrait: Fyodor Yakovlevich Dubyansky, protopriest of the Russian Orthodox Church and personal confessor of Empresses Catherine and Elizabeth, by [Alexey Petrovich Antropov](https://en.wikipedia.org/wiki/Aleksey_Antropov) ([source](https://commons.wikimedia.org/wiki/File:Portrait_of_Father_Fyodor_Dubyansky.jpg)).

3. **Baron** (x2): When you play a Baron against another player, you reveal your hands to each other, and whoever has the lower-valued card is immediately knocked out; on a tie, nothing happens.

  Portrait: [Pyotr Pavlovich Shafirov](https://en.wikipedia.org/wiki/Peter_Shafirov), diplomat and postmaster general of the Russian Empire, by an unknown artist ([source](https://commons.wikimedia.org/wiki/File:Shafirov.jpg)).

4. **Freylina** (x2): When you play a Freylina, you are protected from all other card effects until the start of your next turn.

  Portrait: Yekaterina Dmitrievna Golitsyna, lady-in-waiting of the Imperial Court of Russia, by [Louis-Michel van Loo](https://en.wikipedia.org/wiki/Louis-Michel_van_Loo) ([source](https://commons.wikimedia.org/wiki/File:Louis-Michel_van_Loo_Princess_Ekaterina_Dmitrievna_Golitsyna.jpg)).

5. **Knyaz** (x2): When you play a Knyaz against another player, they immediately discard their current hand and draw another card from the deck. You may also play the Knyaz against yourself to cycle your hand.

  Portrait: [Alexander Danilovich Menshikov](https://en.wikipedia.org/wiki/Alexander_Danilovich_Menshikov), Russian statesman and Emperor Peter's right-hand man, by [Michiel van Musscher](https://en.wikipedia.org/wiki/Michiel_van_Musscher) ([source](https://commons.wikimedia.org/wiki/File:Menshikov_1698_01.jpg)).

6. **Tsar** (x1): When you play the Tsar against another player, you swap hands with them.

  Portrait: [Peter the Great](https://en.wikipedia.org/wiki/Peter_the_Great) (Pyotr Alexeyevich Romanov), first Emperor and Autocrat of All Russias, by [Paul Delaroche](https://en.wikipedia.org/wiki/Paul_Delaroche) ([source](https://commons.wikimedia.org/wiki/File:Peter_der-Grosse_1838.jpg)).

7. **Tsaritsa** (x1): You must discard the Tsaritsa if the other card in your hand is the Tsar or a Knyaz.

  Portrait: [Catherine I](https://en.wikipedia.org/wiki/Catherine_I_of_Russia) (born Marta Samuilovna Skavronskaya), Peter's wife, later the first Empress of Russia, by [Jean-Marc Nattier](https://en.wikipedia.org/wiki/Jean-Marc_Nattier) ([source](https://commons.wikimedia.org/wiki/File:Catherine_I_of_Russia_by_Nattier.jpg)).

8. **Tsarevna** (x1): If you discard the Tsarevna for any reason, you are immediately knocked out of the round.

  Portrait: [Elizabeth](https://en.wikipedia.org/wiki/Elizabeth_of_Russia) (Yelizaveta Petrovna Romanova), daughter of Peter and Catherine, later Empress and Autocrat of All Russias, by an unknown artist ([source](https://commons.wikimedia.org/wiki/File:Elizabeth_of_Russia_(Rostov_museum\).jpeg)).

If you like my card designs, you can also download them in a [single ZIP file](https://drive.google.com/open?id=1shBgYLNqtjkUHKxYrGRBY-0yrPAqrnBN).

## Difficulties
Here is where it gets really interesting. The game starts in the "casual" mode, where the opponent AI is just smart enough to avoid the most glaring gameplay mistakes (like accidentally discarding the Tsarevna, or targeting a player protected by a Freylina). This mode is most useful to get your first bearings and to get accustomed to the cards and the interface.

When in the "casual" mode, you can left-click on the big brain symbol in the top-left corner of the screen to go into the "brainy" mode, where the AI plays on the utmost level of ability that I have, so far, been able to squeeze out of my understanding of game theory, discrete probabilistic prediction and filtering, and dual utility-based decision-making. If you see the opponents in the "brainy" mode make a stupid move, congratulations: you have made sure that there is no smart move to make in their situation. That, or I am just not as good at game AI programming as I claim to be.

You can go back to the "casual" mode at any time by clicking on the casually-strolling figure in the top-left corner. However, *Winter Palace* is really meant to be played in the "brainy" mode.