using System;
using UnityEngine;
using UnityEngine.Events;

namespace CardgameFramework
{
	public class TriggerWatcher : MonoBehaviour
	{
		[SerializeField] private TriggerConditionEventTrio[] trios;

		private void Awake ()
		{
			for (int i = 0; i < trios.Length; i++)
				trios[i].Setup($"TriggerWatcher({name})");
			Match.OnMatchStarted.AddListener(ReplaceReferencesToThis, $"TriggerWatcher({name})");
		}

		private void OnDestroy ()
		{
			Match.OnMatchStarted.RemoveListener(ReplaceReferencesToThis);
		}

		private void ReplaceReferencesToThis (int matchNumber)
		{
			for (int i = 0; i < trios.Length; i++)
			{
				if (trios[i].condition.Contains("$this"))
				{
					if (TryGetComponent(out Card card))
						trios[i].condition = trios[i].condition.Replace("$this", card.id);
					else if (TryGetComponent(out Zone zone))
						trios[i].condition = trios[i].condition.Replace("$this", zone.id);
					trios[i].conditionObj = new NestedConditions(trios[i].condition);
				}
			}
		}

	}

	[Serializable]
	internal class TriggerConditionEventTrio
	{
		[SerializeField] internal TriggerLabel trigger;
		[SerializeField] internal string condition;
		[SerializeField] internal UnityEvent triggeredEvent;
		
		internal NestedConditions conditionObj;

		internal void Setup (string origin)
		{
			conditionObj = new NestedConditions(condition);
			switch (trigger)
			{
				case TriggerLabel.OnMatchStarted:
					Match.OnMatchStarted.AddListener(IntBoolDelegate, IntDelegate, origin);
					break;
				case TriggerLabel.OnMatchEnded:
					Match.OnMatchEnded.AddListener(IntBoolDelegate, IntDelegate, origin);
					break;
				case TriggerLabel.OnTurnStarted:
					Match.OnTurnStarted.AddListener(IntBoolDelegate, IntDelegate, origin);
					break;
				case TriggerLabel.OnTurnEnded:
					Match.OnTurnEnded.AddListener(IntBoolDelegate, IntDelegate, origin);
					break;
				case TriggerLabel.OnPhaseStarted:
					Match.OnPhaseStarted.AddListener(StringBoolDelegate, StringDelegate, origin);
					break;
				case TriggerLabel.OnPhaseEnded:
					Match.OnPhaseEnded.AddListener(StringBoolDelegate, StringDelegate, origin);
					break;
				case TriggerLabel.OnCardUsed:
					Match.OnCardUsed.AddListener(CardBoolDelegate, CardDelegate, origin);
					break;
				case TriggerLabel.OnZoneUsed:
					Match.OnZoneUsed.AddListener(ZoneBoolDelegate, ZoneDelegate, origin);
					break;
				case TriggerLabel.OnCardEnteredZone:
					Match.OnCardEnteredZone.AddListener(CZZBoolDelegate, CZZDelegate, origin);
					break;
				case TriggerLabel.OnCardLeftZone:
					Match.OnCardLeftZone.AddListener(CZBoolDelegate, CZDelegate, origin);
					break;
				case TriggerLabel.OnMessageSent:
					Match.OnMessageSent.AddListener(TwoStringBoolDelegate, TwoStringDelegate, origin);
					break;
				case TriggerLabel.OnActionUsed:
					Match.OnActionUsed.AddListener(TwoStringBoolDelegate, TwoStringDelegate, origin);
					break;
				case TriggerLabel.OnVariableChanged:
					Match.OnVariableChanged.AddListener(FourStringBoolDelegate, FourStringDelegate, origin);
					break;
				case TriggerLabel.OnRuleActivated:
					Match.OnRuleActivated.AddListener(RuleBoolDelegate, RuleDelegate, origin);
					break;
				default:
					break;
			}
		}

		private void InvokeEvent () => triggeredEvent.Invoke();

		private void IntDelegate (int value) => InvokeEvent();
		private void StringDelegate (string str) => InvokeEvent();
		private void CardDelegate (Card card, string addInfo) => InvokeEvent();
		private void ZoneDelegate (Zone zone, string addInfo) => InvokeEvent();
		private void CZZDelegate (Card card, Zone newZone, Zone oldZone, string addInfo) => InvokeEvent();
		private void CZDelegate (Card card, Zone oldZone, string addInfo) => InvokeEvent();
		private void TwoStringDelegate (string str1, string str2) => InvokeEvent();
		private void FourStringDelegate (string str1, string str2, string str3, string str4) => InvokeEvent();
		private void RuleDelegate (Rule rule) => InvokeEvent();

		private bool IntBoolDelegate (int value) => conditionObj.Evaluate();
		private bool StringBoolDelegate (string str) => conditionObj.Evaluate();
		private bool CardBoolDelegate (Card card, string addInfo) => conditionObj.Evaluate();
		private bool ZoneBoolDelegate (Zone zone, string addInfo) => conditionObj.Evaluate();
		private bool CZZBoolDelegate (Card card, Zone newZone, Zone oldZone, string addInfo) => conditionObj.Evaluate();
		private bool CZBoolDelegate (Card card, Zone oldZone, string addInfo) => conditionObj.Evaluate();
		private bool TwoStringBoolDelegate (string str1, string str2) => conditionObj.Evaluate();
		private bool FourStringBoolDelegate (string str1, string str2, string str3, string str4) => conditionObj.Evaluate();
		private bool RuleBoolDelegate (Rule rule) => conditionObj.Evaluate();
	}
}