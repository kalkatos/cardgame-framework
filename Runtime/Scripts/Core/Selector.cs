using System.Collections;
using System.Collections.Generic;

namespace CardgameFramework
{
	public abstract class Selector<T> : Getter
	{
		protected List<SelectionParameter<T>> parameters;
		protected List<T> pool;
		protected int quantity = int.MaxValue;
		protected Getter topQuantityGetter;
		protected Getter bottomQuantityGetter;
		protected List<T> hardSelectionPool;
		protected bool selectAll => parameters == null || parameters.Count == 0;

		public override object Get ()
		{
			if (selectAll)
				return pool;
			List<T> selected = new List<T>();
			if (topQuantityGetter != null)
				quantity = (int)(float)topQuantityGetter.Get();
			else if (bottomQuantityGetter != null)
				quantity = (int)(float)bottomQuantityGetter.Get();
			for (int i = 0; i < pool.Count && selected.Count < quantity; i++)
			{
				T obj = pool[i];
				if (IsAMatch(obj))
					selected.Add(obj);
			}
			return selected;
		} 

		public int GetSelectionCount ()
		{
			if (selectAll) return pool.Count;
			int counter = 0;
			for (int i = 0; i < pool.Count; i++)
			{
				T obj = pool[i];
				if (IsAMatch(obj))
					counter++;
			}
			return counter;
		}

		internal virtual bool IsAMatch (T obj)
		{
			for (int i = 0; i < parameters.Count; i++)
				if (!parameters[i].IsAMatch(obj))
					return false;
			return true;
		}

		internal void SetPool (List<T> newPool)
		{
			pool = newPool;
		}

		internal static bool Contains (string id, CardSelector selector)
		{
			List<Card> selection = (List<Card>)selector.Get();
			foreach (Card item in selection)
				if (item.id == id)
					return true;
			return false;
		}

		internal static bool Contains (string id, ZoneSelector selector)
		{
			List<Zone> selection = (List<Zone>)selector.Get();
			foreach (Zone item in selection)
				if (item.id == id)
					return true;
			return false;
		}

		internal static bool Contains (string id, RuleSelector selector)
		{
			List<Rule> selection = (List<Rule>)selector.Get();
			foreach (Rule item in selection)
				if (item.id == id)
					return true;
			return false;
		}

		internal static bool Contains (Selector<T> left, Selector<T> right)
		{
			List<T> leftSelection = (List<T>)left.Get();
			int matches = 0;
			foreach (T item in leftSelection)
				if (right.IsAMatch(item))
					matches++;
			return matches == leftSelection.Count;
		}
	}

	public class ZoneSelector : Selector<Zone>
	{
		public ZoneSelector ()
		{
			parameters = new List<SelectionParameter<Zone>>();
		}

		internal ZoneSelector (string selectionClause, List<Zone> pool)
		{
			builderStr = selectionClause;
			//if (pool == null)
			//	pool = Match.GetAllZones();
			this.pool = pool;
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionParameter<Zone>> parsToAdd = new List<SelectionParameter<Zone>>();

			if (clauseBreakdown[0] == "zone" || clauseBreakdown[0] == "z" || clauseBreakdown[0] == "allzones")
			{
				if (clauseBreakdown.Length == 1)
				{
					return;
				}
				for (int i = 1; i < clauseBreakdown.Length; i++)
				{
					char firstChar = clauseBreakdown[i][0];
					string sub = clauseBreakdown[i].Substring(1);
					if (sub[0] == ':')
						sub = sub.Substring(1);
					switch (firstChar)
					{
						case 'i':
							if (Match.HasVariable(sub))
								parsToAdd.Add(new MatchStringZoneVariableParameter(sub));
							else
								parsToAdd.Add(new ZoneIDParameter(sub));
							break;
						case 't':
							parsToAdd.Add(new ZoneTagParameter(sub));
							break;
						case 'c':
							parsToAdd.Add(new ZoneByCardsParameter(new CardSelector(sub, Match.GetAllCards())));
							break;
					}
				}
			}
			parameters = parsToAdd;
		}

		public ZoneSelector ByVariable (string variable)
		{
			parameters.Add(new MatchStringZoneVariableParameter(variable));
			return this;
		}

		public ZoneSelector ByID (string id)
		{
			parameters.Add(new ZoneIDParameter(id));
			return this;
		}

		public ZoneSelector ByTags (string tags)
		{
			parameters.Add(new ZoneTagParameter(tags));
			return this;
		}

		public ZoneSelector ByCardSelection (CardSelector cardSelector)
		{
			parameters.Add(new ZoneByCardsParameter(cardSelector));
			return this;
		}
	}

	internal class RuleSelector : Selector<Rule>
	{
		internal RuleSelector (string selectionClause, List<Rule> pool)
		{
			builderStr = selectionClause;
			//if (pool == null)
			//	pool = Match.GetAllRules();
			this.pool = pool;
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionParameter<Rule>> parsToAdd = new List<SelectionParameter<Rule>>();

			if (clauseBreakdown[0] == "rule" || clauseBreakdown[0] == "r" || clauseBreakdown[0] == "allrules")
			{
				if (clauseBreakdown.Length == 1)
					return;

				for (int i = 1; i < clauseBreakdown.Length; i++)
				{
					char firstChar = clauseBreakdown[i][0];
					string sub = clauseBreakdown[i].Substring(1);
					if (sub[0] == ':')
						sub = sub.Substring(1);

					switch (firstChar)
					{
						case 'i':
							if (Match.HasVariable(sub))
								parsToAdd.Add(new MatchStringRuleVariableParameter(sub));
							else
								parsToAdd.Add(new RuleIDParameter(sub));
							break;
						case 't':
							parsToAdd.Add(new RuleTagParameter(new NestedStrings(sub)));
							break;
					}
				}
			}
			parameters = parsToAdd;
		}
	}

	public class CardSelector : Selector<Card>
	{
		public CardSelector ()
		{
			parameters = new List<SelectionParameter<Card>>();
		}

		internal CardSelector (string selectionClause, List<Card> pool)
		{
			builderStr = selectionClause;
			//if (pool == null)
			//	pool = Match.GetAllCards();
			this.pool = pool;
			if (pool != null)
				pool.Sort(CompareCardsByIndexIncreasing);
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionParameter<Card>> parsToAdd = new List<SelectionParameter<Card>>();

			if (clauseBreakdown[0] == "c" || clauseBreakdown[0] == "allcards" || clauseBreakdown[0] == "nc")
			{
				if (clauseBreakdown.Length == 1)
					return;

				for (int i = 1; i < clauseBreakdown.Length; i++)
				{
					char firstChar = clauseBreakdown[i][0];
					string sub = clauseBreakdown[i].Substring(1);
					if (sub[0] == ':')
						sub = sub.Substring(1);

					switch (firstChar)
					{
						case 'i':
							if (Match.HasVariable(sub))
								parsToAdd.Add(new MatchStringVariableParameter(sub));
							else
								parsToAdd.Add(new CardIDParameter(sub));
							break;
						case 'z':
							if (Match.HasVariable(sub))
								parsToAdd.Insert(0, new CardZoneIDParameter(sub));
							else
								parsToAdd.Insert(0, new CardZoneTagParameter(sub));
							break;
						case 't':
							parsToAdd.Add(new CardTagParameter(sub));
							break;
						//case 'r':
						//	parsToAdd.Add(new CardRuleTagCard(new NestedStrings(sub)));
						//	break;
						case 'f':
							parsToAdd.Add(new CardFieldParameter(sub));
							break;
						case 'x':
							topQuantityGetter = Build(sub);
							break;
						case 'b':
							bottomQuantityGetter = Build(sub);
							break;
						//case 's':
						//	parsToAdd.Add(new CardZoneSlotCard(Build(sub)));
						//	break;
						case 'n':
							parsToAdd.Add(new CardIndexParamenter(sub));
							break;
						default:
							break;
					}
				}
			}
			parameters = parsToAdd;
		}

		public CardSelector ByID (string id)
		{
			parameters.Add(new CardIDParameter(id));
			return this;
		}

		public CardSelector ByVariable (string variable)
		{
			parameters.Add(new MatchStringVariableParameter(variable));
			return this;
		}

		public CardSelector ByZoneID (string zoneID)
		{
			parameters.Insert(0, new CardZoneIDParameter(zoneID));
			return this;
		}

		public CardSelector ByZoneTags (string zoneTags)
		{
			parameters.Insert(0, new CardZoneTagParameter(zoneTags));
			return this;
		}

		public CardSelector ByField (string fieldClause)
		{
			parameters.Add(new CardFieldParameter(fieldClause));
			return this;
		}

		public CardSelector CountFromTop (string clause)
		{
			topQuantityGetter = Build(clause);
			return this;
		}

		public CardSelector CountFromTop (int value)
		{
			topQuantityGetter = new NumberGetter(value);
			return this;
		}

		public CardSelector CountFromBottom (string clause)
		{
			bottomQuantityGetter = Build(clause);
			return this;
		}

		public CardSelector CountFromBottom (int value)
		{
			bottomQuantityGetter = new NumberGetter(value);
			return this;
		}

		public CardSelector ByIndex (string clause)
		{
			parameters.Add(new CardIndexParamenter(clause));
			return this;
		}

		public override object Get ()
		{
			if (topQuantityGetter != null)
				pool.Sort(CompareCardsByIndexIncreasing);
			else if (bottomQuantityGetter != null)
				pool.Sort(CompareCardsByIndexDecreasing);
			return base.Get();
		}

		private static int CompareCardsByIndexIncreasing (Card c1, Card c2)
		{
			if (c1.Zone != null && c2.Zone != null)
			{
				int c1Index = c1.Zone.GetIndexOf(c1), c2Index = c2.Zone.GetIndexOf(c2);
				if (c1Index < c2Index)
					return 1;
				else if (c1Index > c2Index)
					return -1;
				else
					return 0;
			}
			return 0;
		}

		private static int CompareCardsByIndexDecreasing (Card c1, Card c2)
		{
			if (c1.Zone != null && c2.Zone != null)
			{
				int c1Index = c1.Zone.GetIndexOf(c1), c2Index = c2.Zone.GetIndexOf(c2);
				if (c1Index > c2Index)
					return 1;
				else if (c1Index < c2Index)
					return -1;
				else
					return 0;
			}
			return 0;
		}
	}
}