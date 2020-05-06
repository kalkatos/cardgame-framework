# CGEngine
# Cardgame Framework

This is a framework built in Unity that provides basic functionality for creating card and board games.

It works in an Observer pattern in which <b>Watchers</b> are created and assigned by a central controller object called <b>Match</b> to execute <b>Commands</b> when certain <b>Triggers</b> in a card or board game occur, like when a turn or turn phase starts, when a card enters or leaves a certain zone, etc. 

Some events (usually UI events) can also be set to fire when an user input is detected, like dragging or clicking a card.

Note: this Readme is a work in progress... New documentation is being added.

# Triggers

* <b>OnCardUsed</b> Comes with: "usedCard" : the ID of the card used. What "using" a card means is defined by the 
* <b>OnCardEnteredZone</b> Comes with: "movedCard" : the card moved; "targetZone" : the zone the card has just entered. If card is not defined, this trigger will be active for every card that enters the zone; “oldZone” : the zone the card has just left.
* <b>OnCardLeftZone</b> Comes with: "movedCard" : the card moved; "oldZone" : the zone the card has just left. If card is not defined, this trigger will be active for every card that leaves the zone.
* <b>OnMatchEnded</b> Comes with: "matchNumber" the number of that match
* <b>OnMatchSetup</b> Comes with: "matchNumber" the number of that match
* <b>OnMatchStarted</b> Comes with: "matchNumber" the number of that match
* <b>OnPhaseEnded</b> Comes with: “phase” the name of the phase that has just ended.
* <b>OnPhaseStarted</b> Comes with: “phase” the name of the phase that has just started.
* <b>OnTurnEnded</b> Comes with: “turnNumber” the number of the turn that has just ended.
* <b>OnTurnStarted</b> Comes with: “turnNumber” the number of the turn that has just started.
* <b>OnMessageSent</b> Comes with: “message” the name of the message that was just sent.
* <b>OnVariableChanged</b> Comes with: "variable" the name of the variable changed; "value" the new value set to that variable.
* <b>OnActionUsed</b> Comes with: “actionName” the name of the action that was just used.

# Commands

* <b>EndCurrentPhase</b> Finishes current phase of the turn immediatly.
* <b>EndTheMatch</b> Ends the current match immediatly.
* <b>EndSubphaseLoop</b> Finishes the subphase loop started by StartSubphaseLoop. Also finishes the regular phase where the subphase loop started.
* <b>UseAction (text actionName)</b> Sends a message from the UI to all watchers and modifiers listening stating that the actionName must be used.
* <b>SendMessage (text messageName)</b> Sends a message from the game to all watchers and modifiers listening with the messageName passed as parameter.
* <b>StartSubphaseLoop (text phase1, text phase2, …)</b> Starts a subphase sequence defined in the parameters. The sequence will loop forever until EndSubphaseLoop is called.
* <b>Shuffle (ZoneSelector zoneSelection)</b> Shuffles cards in zone.
* <b>UseCard (CardSelector cardSelection)</b> Sets a card as used to trigger rule activations.
* <b>MoveCardToZone (CardSelector cardSelection, ZoneSelector zoneSelection, [Additional Params])</b> Move all cards found in card selection to the zone specified in zone selection. If more than one zone is found in zoneSelection, the card moving will be executed for each zone if possible (useful for moving one card from the top of a deck to each player hand, for example).
* <b>Additional Parameters can be:</b><br>
    Bottom - moves the card(s) to the bottom of the zone instead of to the top;<br>
    Revealed - sets the card(s) as revealed to everyone regardless of the zone definition;<br>
    Hidden - sets the card(s) as hidden from everyone regardless of the zone definition;<br>
    (x, y) - for a grid zone, moves the cards to positions in grid.<br>
* <b>SetCardFieldValue (CardSelector cardSelection, text fieldName, number newValue)</b>	Changes the value of a field of a card.
* <b>SetVariable (text variableName, number value)</b>	Changes de value of a custom match variable.
* <b>AddTagToCard (Card cardSelection, text tag)</b> Adds a tag to a card.
* <b>RemoveTagFromCard (Card cardSelection, text tag)</b> Removes a tag from a card.
