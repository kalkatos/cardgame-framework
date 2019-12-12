using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	public class Match2 : MonoBehaviour
	{
		bool gameEnded;
		bool endCurrentPhase;
		public string[] turnPhases;
		public Action currentAction;
		//WARNING is it necessary to include priorities?
		Dictionary<string, List<CoroutineDelegate>> subroutines;

		void Start()
		{

		}

		public void RegisterForTrigger(string triggerTag, CoroutineDelegate callback)
		{
			if (subroutines.ContainsKey(triggerTag))
				subroutines[triggerTag].Add(callback);
			else
			{
				List<CoroutineDelegate> list = new List<CoroutineDelegate>();
				list.Add(callback);
				subroutines.Add(triggerTag, list);
			}
		}

		public void Unregister (CoroutineDelegate callback)
		{
			foreach (List<CoroutineDelegate> item in subroutines.Values)
			{
				item.Remove(callback);
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
			if (subroutines.ContainsKey("OnMatchSetup"))
			{ 
				for (int i = 0; i < subroutines["OnMatchSetup"].Count; i++)
				{
					yield return subroutines["OnMatchSetup"][i](null);
				}
			}
		}

		IEnumerator StartMatch()
		{
			if (subroutines.ContainsKey("OnMatchStarted"))
			{
				for (int i = 0; i < subroutines["OnMatchStarted"].Count; i++)
				{
					yield return subroutines["OnMatchStarted"][i](null);
				}
			}
		}

		IEnumerator StartTurn()
		{
			if (subroutines.ContainsKey("OnTurnStarted"))
			{
				for (int i = 0; i < subroutines["OnTurnStarted"].Count; i++)
				{
					yield return subroutines["OnTurnStarted"][i](null);
				}
			}
		}

		IEnumerator StartPhase(string phaseName)
		{
			if (subroutines.ContainsKey("OnPhaseStarted"))
			{
				for (int i = 0; i < subroutines["OnPhaseStarted"].Count; i++)
				{
					yield return subroutines["OnPhaseStarted"][i](phaseName);
				}
			}
		}

		IEnumerator EndPhase(string phaseName)
		{
			if (subroutines.ContainsKey("OnPhaseEnded"))
			{
				for (int i = 0; i < subroutines["OnPhaseEnded"].Count; i++)
				{
					yield return subroutines["OnPhaseEnded"][i](phaseName);
				}
			}
		}

		IEnumerator EndTurn()
		{
			if (subroutines.ContainsKey("OnTurnEnded"))
			{
				for (int i = 0; i < subroutines["OnTurnEnded"].Count; i++)
				{
					yield return subroutines["OnTurnEnded"][i](null);
				}
			}
		}

		IEnumerator EndMatch()
		{
			if (subroutines.ContainsKey("OnMatchEnded"))
			{
				for (int i = 0; i < subroutines["OnMatchEnded"].Count; i++)
				{
					yield return subroutines["OnMatchEnded"][i]();
				}
			}
		}
	}

	public class Action
	{
		public string actionName;
		public object actionObject;
	}

	public delegate IEnumerator CoroutineDelegate(params object[] args);
}