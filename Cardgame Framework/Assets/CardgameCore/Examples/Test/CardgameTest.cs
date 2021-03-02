using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardgameCore;

public class CardgameTest : MonoBehaviour
{
	private void Awake()
	{
        Match.OnPhaseStarted += MainPhaseStarted;
        Match.OnMessageSent += UseActionOnMessage;
	}

	private void Start()
    {
        //List<Rule> rules = new List<Rule>();
        //rules.Add(new Rule("TestRule", TriggerLabel.OnPhaseStarted, null, 
        //    new Command[] { new Command(CommandType.SendMessage, "TestMessage"), new Command(CommandType.EndCurrentPhase) }));
        //rules.Add(new Rule("TestRule2", TriggerLabel.OnMessageSent, null, 
        //    new Command[] { new Command(CommandType.UseAction, "TestAction"), new Command(CommandType.EndTheMatch) }));
        //Match.StartMatch(rules);
        Match.StartMatch();
    }

	private void OnDestroy()
	{
        Match.OnPhaseStarted -= MainPhaseStarted;
        Match.OnMessageSent -= UseActionOnMessage;
    }

	private IEnumerator MainPhaseStarted ()
	{
        Debug.Log("Waiting 2s");
        yield return new WaitForSeconds(2f);
        yield return Match.SendMessage("TestMessage");
        yield return Match.EndCurrentPhase();
	}

    private IEnumerator UseActionOnMessage ()
	{
        yield return Match.UseAction("TestAction");
        yield return Match.EndTheMatch();
	}
}
