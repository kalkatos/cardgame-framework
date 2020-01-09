using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	public class Player : MonoBehaviour
	{
		// Receives and sends Actions to a Match

		/*
		match : Match
		currentRules : PlayerRules

		cardsRevealedToSelf : Card[] //pointers

		id : string //starts with “p”
		resourcePools : string[]
		score : int
		zones : Zone[]  //pointers
		activeModifiers : Modifier[]

		possibleActions : Action[]
		*/

		public UserType userType;
		public PlayerRules playerRules;
		public Deck deck;
		public string id; //starts with “p”
		public string actionChosen;
		public Card cardChosen;
		List<Modifier> modifiers;
		public List<Modifier> Modifiers { get { if (modifiers == null) modifiers = new List<Modifier>(); return modifiers; } }

		private List<string> possibleActions;
		public List<string> PossibleActions
		{
			get
			{
				if (possibleActions == null)
					possibleActions = new List<string>();
				return possibleActions;
			}
		}

		public void OnDrawGizmos()
		{
			Gizmos.DrawIcon(transform.position, "../CGEngine/DefaultSprites/icon-player.png", true);
		}
	}
}