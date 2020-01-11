using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	
	public class Player : MonoBehaviour
	{
		// Receives and sends Actions to a Match
		
		public UserType userType;
		public PlayerRole playerRules;
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