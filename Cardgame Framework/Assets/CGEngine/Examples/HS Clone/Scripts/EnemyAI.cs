using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardGameFramework;

public class EnemyAI : MatchWatcher
{
	Command endPhaseCommand;
	Zone myHand;
	Zone myPlay;

	private void Start ()
	{
		myHand = GameObject.Find("EnemyHand").GetComponent<Zone>();
		myPlay = GameObject.Find("EnemyPlay").GetComponent<Zone>();
	}

	public override IEnumerator OnMatchStarted (int matchNumber)
	{
		endPhaseCommand = Match.Current.CreateCommand("UseAction(EndTurn)");
		yield return null;
	}

	public override IEnumerator OnPhaseStarted (string phase)
	{
		if (Match.Current.GetVariable("ActivePlayer").ToString() == "P2")
		{
			yield return new WaitForSeconds(0.7f);
			object c = Getter.Build("c(z:Hand&P2,f:Cost=1,x:1)");
			if (c != null && c is CardSelector)
			{
				Match.Current.UseCard((Card)((CardSelector)c).Get());
				Match.Current.UseZone(myPlay);
			}
			Match.Current.ExecuteCommand(endPhaseCommand);
		}
	}
}
