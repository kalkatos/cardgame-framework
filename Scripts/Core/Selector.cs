using System.Collections;
using System.Collections.Generic;

namespace CardgameCore
{
	public abstract class Selector<T> : Getter
	{
		protected SelectionParameter<T>[] parameters;
		protected List<T> pool;
		protected bool selectAll = false;
		protected int quantity = int.MaxValue;
		protected Getter topQuantityGetter;
		protected Getter bottomQuantityGetter;
		protected List<T> hardSelectionPool;

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

		public virtual bool IsAMatch (T obj)
		{
			for (int i = 0; i < parameters.Length; i++)
				if (!parameters[i].IsAMatch(obj))
					return false;
			return true;
		}

		public static bool Contains (string id, CardSelector selector)
		{
			List<Card> selection = (List<Card>)selector.Get();
			foreach (Card item in selection)
				if (item.id == id)
					return true;
			return false;
		}

		public static bool Contains (string id, ZoneSelector selector)
		{
			List<Zone> selection = (List<Zone>)selector.Get();
			foreach (Zone item in selection)
				if (item.id == id)
					return true;
			return false;
		}

		public static bool Contains (string id, RuleSelector selector)
		{
			List<Rule> selection = (List<Rule>)selector.Get();
			foreach (Rule item in selection)
				if (item.id == id)
					return true;
			return false;
		}

		public static bool Contains (Selector<T> left, Selector<T> right)
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
		public ZoneSelector (string selectionClause, List<Zone> pool = null)
		{
			builderStr = selectionClause;
			if (pool == null)
				pool = Match.GetAllZones();
			this.pool = pool;
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionParameter<Zone>> parsToAdd = new List<SelectionParameter<Zone>>();

			if (clauseBreakdown[0] == "zone" || clauseBreakdown[0] == "z" || clauseBreakdown[0] == "allzones")
			{
				if (clauseBreakdown.Length == 1)
				{
					selectAll = true;
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
							parsToAdd.Add(new ZoneTagParameter(new NestedStrings(sub)));
							break;
						case 'c':
							parsToAdd.Add(new ZoneByCardsParameter(new CardSelector(sub)));
							break;
					}
				}
			}
			parameters = parsToAdd.ToArray();
		}
	}

	public class RuleSelector : Selector<Rule>
	{
		public RuleSelector (string selectionClause, List<Rule> pool = null)
		{
			builderStr = selectionClause;
			if (pool == null)
				pool = Match.GetAllRules();
			this.pool = pool;
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionParameter<Rule>> parsToAdd = new List<SelectionParameter<Rule>>();

			if (clauseBreakdown[0] == "rule" || clauseBreakdown[0] == "r" || clauseBreakdown[0] == "allrules")
			{
				if (clauseBreakdown.Length == 1)
				{
					selectAll = true;
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
			parameters = parsToAdd.ToArray();
		}
	}

	public class CardSelector : Selector<Card>
	{
		public CardSelector (string selectionClause, List<Card> pool = null)
		{
			builderStr = selectionClause;
			if (pool == null)
				pool = Match.GetAllCards();
			this.pool = pool;
			System.Array.Sort(pool.ToArray(), CompareCardsByIndexIncreasing);
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionParameter<Card>> parsToAdd = new List<SelectionParameter<Card>>();

			if (clauseBreakdown[0] == "c" || clauseBreakdown[0] == "allcards" || clauseBreakdown[0] == "nc")
			{
				if (clauseBreakdown.Length == 1)
				{
					selectAll = true;
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
								parsToAdd.Add(new MatchStringVariableParameter(sub));
							else
								parsToAdd.Add(new CardIDParameter(sub));
							break;
						case 'z':
							if (Match.HasVariable(sub))
								parsToAdd.Insert(0, new CardZoneIDParameter(sub));
							else
								parsToAdd.Insert(0, new CardZoneTagParameter(new NestedStrings(sub)));
							break;
						case 't':
							parsToAdd.Add(new CardTagParameter(new NestedStrings(sub)));
							break;
						//case 'r':
						//	parsToAdd.Add(new CardRuleTagCard(new NestedStrings(sub)));
						//	break;
						case 'f':
							parsToAdd.Add(new CardFieldParameter(new NestedCardFieldConditions(sub)));
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
							parsToAdd.Add(new CardIndexParamenter(new NestedCardIndexConditions(sub)));
							break;
						default:
							break;
					}
				}
			}
			parameters = parsToAdd.ToArray();
		}

		public override object Get ()
		{
			if (topQuantityGetter != null)
				pool.Sort(CompareCardsByIndexIncreasing);
			else if (bottomQuantityGetter != null)
				pool.Sort(CompareCardsByIndexDecreasing);
			return base.Get();
		}

		public static int CompareCardsByIndexIncreasing (Card c1, Card c2)
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

		public static int CompareCardsByIndexDecreasing (Card c1, Card c2)
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