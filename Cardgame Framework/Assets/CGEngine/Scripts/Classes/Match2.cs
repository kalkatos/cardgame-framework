using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	public delegate IEnumerator MatchSubroutine(params object[] args);

	public class Match2 : MonoBehaviour
	{
		public static Match2 Current { get; private set; }

		bool gameEnded;
		bool endCurrentPhase;
		public string[] turnPhases;
		public Action currentAction;
		//WARNING is it necessary to include priorities?
		Dictionary<string, List<MatchSubroutine>> subroutines;
		Dictionary<string, List<MatchSubroutine>> Subroutines
		{ get { if (subroutines == null) subroutines = new Dictionary<string, List<MatchSubroutine>>(); return subroutines; } }

		void Awake()
		{
			Debug.Log("Match2 online!");
			Current = this;
		}

		public void RegisterForTrigger(string triggerTag, MatchSubroutine subroutine)
		{
			if (Subroutines.ContainsKey(triggerTag))
				Subroutines[triggerTag].Add(subroutine);
			else
			{
				List<MatchSubroutine> list = new List<MatchSubroutine>();
				list.Add(subroutine);
				Subroutines.Add(triggerTag, list);
			}
		}

		public void Unregister (MatchSubroutine subroutine)
		{
			foreach (List<MatchSubroutine> item in Subroutines.Values)
			{
				item.Remove(subroutine);
			}
		}

		IEnumerator MatchLoop()
		{
			yield return MatchSetup();
			yield return StartMatch();
			while (!gameEnded)
			{
				yield return StartTurn();
				for (int i = 0; i < turnPhases.Length; i++)
				{
					endCurrentPhase = false;
					currentAction = null;
					yield return StartPhase(turnPhases[i]);
					while (!endCurrentPhase)
						yield return currentAction;
					yield return EndPhase(turnPhases[i]);
				}
				yield return EndTurn();
				//activePlayer = GetNextPlayer();
			}
			yield return EndMatch();
		}

		IEnumerator MatchSetup()
		{
			MessageBus.Send("OnMatchSetup");
			if (Subroutines.ContainsKey("OnMatchSetup"))
			{ 
				for (int i = 0; i < Subroutines["OnMatchSetup"].Count; i++)
				{
					yield return Subroutines["OnMatchSetup"][i](null);
				}
			}
		}

		IEnumerator StartMatch()
		{
			if (Subroutines.ContainsKey("OnMatchStarted"))
			{
				for (int i = 0; i < Subroutines["OnMatchStarted"].Count; i++)
				{
					yield return Subroutines["OnMatchStarted"][i](null);
				}
			}
		}

		IEnumerator StartTurn()
		{
			if (Subroutines.ContainsKey("OnTurnStarted"))
			{
				for (int i = 0; i < Subroutines["OnTurnStarted"].Count; i++)
				{
					yield return Subroutines["OnTurnStarted"][i](null);
				}
			}
		}

		IEnumerator StartPhase(string phaseName)
		{
			if (Subroutines.ContainsKey("OnPhaseStarted"))
			{
				for (int i = 0; i < Subroutines["OnPhaseStarted"].Count; i++)
				{
					yield return Subroutines["OnPhaseStarted"][i](phaseName);
				}
			}
		}

		IEnumerator EndPhase(string phaseName)
		{
			if (Subroutines.ContainsKey("OnPhaseEnded"))
			{
				for (int i = 0; i < Subroutines["OnPhaseEnded"].Count; i++)
				{
					yield return Subroutines["OnPhaseEnded"][i](phaseName);
				}
			}
		}

		IEnumerator EndTurn()
		{
			if (Subroutines.ContainsKey("OnTurnEnded"))
			{
				for (int i = 0; i < Subroutines["OnTurnEnded"].Count; i++)
				{
					yield return Subroutines["OnTurnEnded"][i](null);
				}
			}
		}

		IEnumerator EndMatch()
		{
			if (Subroutines.ContainsKey("OnMatchEnded"))
			{
				for (int i = 0; i < Subroutines["OnMatchEnded"].Count; i++)
				{
					yield return Subroutines["OnMatchEnded"][i]();
				}
			}
		}
	}

	public class Action
	{
		public string actionName;
		public object actionObject;
	}
}