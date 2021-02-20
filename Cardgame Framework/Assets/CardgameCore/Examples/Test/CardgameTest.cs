using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardgameCore;

public class CardgameTest : MonoBehaviour
{
    private void Start()
    {
        List<Rule> rules = new List<Rule>();
        rules.Add(new Rule("TestRule", TriggerType.OnPhaseStarted, null, 
            new Command[] { new Command(CommandType.SendMessage, "TestMessage"), new Command(CommandType.EndCurrentPhase) }));
        rules.Add(new Rule("TestRule2", TriggerType.OnMessageSent, null, 
            new Command[] { new Command(CommandType.UseAction, "TestAction"), new Command(CommandType.EndTheMatch) }));
        Match.StartMatch(rules);
    }

}
