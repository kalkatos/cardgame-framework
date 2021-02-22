using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	public abstract class Selector<T> : Getter
	{
		protected SelectionComponent<T>[] components;
		protected T[] pool;
		protected bool selectAll = false;
		protected int quantity = int.MaxValue;
		protected Getter topQuantityGetter;
		protected Getter bottomQuantityGetter;

		public override object Get ()
		{
			if (selectAll) return pool;

			List<T> selected = new List<T>();
			if (topQuantityGetter != null)
				quantity = (int)(float)topQuantityGetter.Get();
			else if (bottomQuantityGetter != null)
				quantity = (int)(float)bottomQuantityGetter.Get();
			for (int i = 0; i < pool.Length && selected.Count < quantity; i++)
			{
				T obj = pool[i];
				if (IsAMatch(obj))
					selected.Add(obj);
			}
			return selected.ToArray();
		} 

		public int GetSelectionCount ()
		{
			if (selectAll) return pool.Length;
			int counter = 0;
			for (int i = 0; i < pool.Length; i++)
			{
				T obj = pool[i];
				if (IsAMatch(obj))
					counter++;
			}
			return counter;
		}

		public virtual bool IsAMatch (T obj)
		{
			for (int i = 0; i < components.Length; i++)
			{
				if (!components[i].IsAMatch(obj))
					return false;
			}
			return true;
		}

		public static bool Contains (string id, CardSelector selector)
		{
			Card[] selection = (Card[])selector.Get();
			foreach (Card item in selection)
			{
				if (item.ID == id)
					return true;
			}
			return false;
		}

		public static bool Contains (string id, ZoneSelector selector)
		{
			Zone[] selection = (Zone[])selector.Get();
			foreach (Zone item in selection)
			{
				if (item.ID == id)
					return true;
			}
			return false;
		}

		public static bool Contains (Selector<T> left, Selector<T> right)
		{
			T[] leftSelection = (T[])left.Get();
			int matches = 0;
			foreach (T item in leftSelection)
			{
				if (right.IsAMatch(item))
					matches++;
			}
			return matches == leftSelection.Length;
		}
	}

	public class ZoneSelector : Selector<Zone>
	{
		public ZoneSelector (string selectionClause, Zone[] pool = null)
		{
			if (pool == null)
				pool = Match.Current.GetAllZones();
			this.pool = pool;
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionComponent<Zone>> compsToAdd = new List<SelectionComponent<Zone>>();


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
							if (Match.Current.HasVariable(sub))
								compsToAdd.Add(new MatchStringZoneVariableComponent(sub));
							else
								compsToAdd.Add(new ZoneIDComponent(sub));
							break;
						case 't':
							compsToAdd.Add(new ZoneTagComponent(new NestedStrings(sub)));
							//compsToAdd.Add(new ZoneTagComponent(new NestedStrings(sub)));
							break;
					}
				}
			}
			components = compsToAdd.ToArray();
		}
	}

	public class CardSelector : Selector<Card>
	{
		public CardSelector (string selectionClause, Card[] pool = null)
		{
			if (pool == null)
				pool = Match.Current.GetAllCards();
			this.pool = pool;
			System.Array.Sort(pool, CompareCardsByIndexIncreasing);
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionComponent<Card>> compsToAdd = new List<SelectionComponent<Card>>();

			if (clauseBreakdown[0] == "card" || clauseBreakdown[0] == "c" || clauseBreakdown[0] == "allcards" || clauseBreakdown[0] == "ncards" || clauseBreakdown[0] == "nc")
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
							if (Match.Current.HasVariable(sub))
								compsToAdd.Add(new MatchStringVariableComponent(sub));
							else
								compsToAdd.Add(new CardIDComponent(sub));
							break;
						case 'z':
							if (Match.Current.HasVariable(sub))
								compsToAdd.Insert(0, new CardZoneIDComponent(sub));
							else
								compsToAdd.Insert(0, new CardZoneTagComponent(new NestedStrings(sub)));
							break;
						case 't':
							compsToAdd.Add(new CardTagComponent(new NestedStrings(sub)));
							break;
						case 'r':
							compsToAdd.Add(new CardRuleTagComponent(new NestedStrings(sub)));
							break;
						case 'f':
							compsToAdd.Add(new CardFieldComponent(new NestedCardFieldConditions(sub)));
							break;
						case 'x':
							topQuantityGetter = Build(sub);
							break;
						case 'b':
							bottomQuantityGetter = Build(sub);
							break;
						case 's':
							compsToAdd.Add(new CardZoneSlotComponent(Build(sub)));
							break;
						default:
							break;
					}


				}
			}
			components = compsToAdd.ToArray();
		}

		public override object Get ()
		{
			if (topQuantityGetter != null)
				System.Array.Sort(pool, CompareCardsByIndexIncreasing);
			else if (bottomQuantityGetter != null)
				System.Array.Sort(pool, CompareCardsByIndexDecreasing);
			return base.Get();
		}

		public static int CompareCardsByIndexIncreasing (Card c1, Card c2)
		{
			if (c1.zone != null && c2.zone != null)
			{
				int c1Index = c1.zone.Content.IndexOf(c1), c2Index = c2.zone.Content.IndexOf(c2);
				if (c1Index < c2Index)
					return 1;
				if (c1Index > c2Index)
					return -1;
			}
			return 0;
		}

		public static int CompareCardsByIndexDecreasing (Card c1, Card c2)
		{
			if (c1.zone != null && c2.zone != null)
			{
				int c1Index = c1.zone.Content.IndexOf(c1), c2Index = c2.zone.Content.IndexOf(c2);
				if (c1Index > c2Index)
					return 1;
				if (c1Index < c2Index)
					return -1;
			}
			return 0;
		}
	}


}