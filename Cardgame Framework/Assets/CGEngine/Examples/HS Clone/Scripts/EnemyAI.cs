using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class EnemyAI : MatchWatcher
{
	Command endPhaseCommand;
	
	public override IEnumerator OnMatchStarted (int matchNumber)
	{
		endPhaseCommand = Match.Current.CreateCommand("UseAction(EndTurn)");
		yield return null;
	}

	public override IEnumerator OnPhaseStarted (string phase)
	{
		if (Match.Current.GetVariable("ActivePlayer").ToString() == "P2")
		{
			yield return new WaitForSeconds(1f);
			Match.Current.ExecuteCommand(endPhaseCommand);
		}
	}
}
