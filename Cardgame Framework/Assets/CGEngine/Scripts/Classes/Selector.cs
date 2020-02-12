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
		protected Getter quantityGetter;

		public override object Get ()
		{
			if (selectAll) return pool;

			List<T> selected = new List<T>();
			if (quantityGetter != null)
				quantity = (int)(float)quantityGetter.Get();
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
				if (!components[i].Match(obj))
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
		public ZoneSelector (Zone z)
		{
			pool = new Zone[] { z };
			selectAll = true;
		}

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

					switch (firstChar)
					{
						case '#':
						case 'i':
							if (Match.Current.HasVariable(sub))
								compsToAdd.Add(new MatchStringZoneVariableComponent(sub));
							else
								compsToAdd.Add(new ZoneIDComponent(sub));
							break;
						case '@':
						case 'z':
						case '~':
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
		public CardSelector (Card c)
		{
			pool = new Card[] { c };
			selectAll = true;
		}

		public CardSelector (string selectionClause, Card[] pool = null)
		{
			if (pool == null)
				pool = Match.Current.GetAllCards();
			this.pool = pool;
			System.Array.Sort(pool, CompareCardsByIndexForSorting);
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

					switch (firstChar)
					{

						case '#':
						case 'i':
							if (Match.Current.HasVariable(sub))
								compsToAdd.Add(new MatchStringVariableComponent(sub));
							else
								compsToAdd.Add(new CardIDComponent(sub));
							break;
						case '@':
						case 'z':
							if (Match.Current.HasVariable(sub))
								compsToAdd.Insert(0, new CardZoneIDComponent(sub));
							else
								compsToAdd.Insert(0, new CardZoneTagComponent(new NestedStrings(sub)));
							break;
						case '~':
						case 't':
							compsToAdd.Add(new CardTagComponent(new NestedStrings(sub)));
							break;
						case ':':
						case 'm':
							compsToAdd.Add(new CardModifierTagComponent(new NestedStrings(sub)));
							break;
						case '.':
						case 'f':
							compsToAdd.Add(new CardFieldComponent(new NestedCardFieldConditions(sub)));
							break;
						case 'x':
							quantityGetter = Build(sub);
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
			if (quantityGetter != null)
				System.Array.Sort(pool, CompareCardsByIndexForSorting);
			return base.Get();
		}

		public static int CompareCardsByIndexForSorting (Card c1, Card c2)
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
	}


}