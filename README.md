# Cardgame Framework
A framework for Unity that implements a rules engine to ease up the development of card and board games with setup from minimal C# code, or via the editor with the help of a custom scripting language.
## How it Works
It implements a rules engine pattern, so a game is simplified down to a set of rules to be executed when a certain event happens in the game. Each rule has a trigger, a condition, and two effects to be executed, one for when the condition is true, and one for when the condition is false.

For example, let's consider a rule for shuffling cards at the start of the game:

```
Rule: Shuffle
Trigger: The game has started
Condition: None (always true)
True Effects: Shuffle all cards.
False Effects: None
```

In this case, when the game starts, the rule is triggered. Since the condition is always true (denoted by "None"), the true effect is executed, resulting in shuffling all the cards. The false effect is empty, indicating that no action needs to be taken when the condition is false.
By utilizing this breakdown into rules, developers can easily define the behavior and logic of their card or board game. Check the breakdown for Solitaire below.

<details>
<summary>Klondike Solitaire Implementation</summary>


This is how a game of Klondike Solitaire could be broken down into rules to be implemented by this framework.

### Rule 1
```
Rule: Preparation
Trigger: The game has started
Condition: None (always true)
True Effects:
	Shuffle all cards,
	Move one face-up card to the first column,
	Move one face-down and one face-up card to the second column,
	Move two face-down and one face-up card to the third column,
	Move three face-down and one face-up card to the fourth column,
	Move four face-down and one face-up card to the fifth column,
	Move five face-down and one face-up card to the sixth column,
	Move six face-down and one face-up card to the seventh column
False Effects: None
```
### Rule 2
```
Rule: Reveal Card
Trigger: The player clicked the deck
Condition: The deck has any card
True Effects: Move the top deck card to the revealed pile
False Effects: None
```
### Rule 3
```
Rule: Movement
Trigger: The player dropped a card on a column
Condition:
	The card were already revealed
	AND the card has different color and is one rank lower than the top card in the column
	OR the card is a king and the column is empty
True Effects: Move the dropped card and all cards on top of that card to the column
False Effects: None
```
### Rule 4
```
Rule: Foundations
Trigger: The player dropped a card on a foundation
Condition:
	The foundation is empty AND the card is an ace
	OR the card is from the same suit and one rank above the foundation’s top card
True Effects: Move the dropped card to the foundation
False Effects: None
```
### Rule 5
```
Rule: Reshuffle
Trigger: The player clicked the revealed pile
Condition: The deck is empty
True Effects: Move the cards in the revealed pile to the deck face-down
False Effects: None
```
### Rule 6
```
Rule: Reveal Next Card
Trigger: A card has moved out of a column
Condition:
	The column is not empty
	AND the top card is not revealed
True Effects: Reveal the top card of the column
False Effects: None
```
### Rule 7
```
Rule: Win Condition
Trigger: A card has moved to a foundation
Condition: All cards are in foundations
True Effects: End the game (Victory!)
False Effects: None
```
</details>

## The Scripting Language

WIP
