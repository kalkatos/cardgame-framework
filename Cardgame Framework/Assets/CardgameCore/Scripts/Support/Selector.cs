using System.Collections;
using System.Collections.Generic;

namespace CardgameCore
{
	public abstract class Selector<T> : Getter
	{
		protected SelectionParameter<T>[] components;
		protected List<T> pool;
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
			for (int i = 0; i < components.Length; i++)
			{
				if (!components[i].IsAMatch(obj))
					return false;
			}
			return true;
		}

		public static bool Contains (string id, ComponentSelector selector)
		{
			CGComponent[] selection = (CGComponent[])selector.Get();
			foreach (CGComponent item in selection)
			{
				if (item.id == id)
					return true;
			}
			return false;
		}

		public static bool Contains (string id, ZoneSelector selector)
		{
			Zone[] selection = (Zone[])selector.Get();
			foreach (Zone item in selection)
			{
				if (item.id == id)
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
		public ZoneSelector (string selectionClause, List<Zone> pool = null)
		{
			if (pool == null)
				pool = Match.GetAllZones();
			this.pool = pool;
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionParameter<Zone>> compsToAdd = new List<SelectionParameter<Zone>>();


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
								compsToAdd.Add(new MatchStringZoneVariableParameter(sub));
							else
								compsToAdd.Add(new ZoneIDParameter(sub));
							break;
						case 't':
							compsToAdd.Add(new ZoneTagParameter(new NestedStrings(sub)));
							//compsToAdd.Add(new ZoneTagComponent(new NestedStrings(sub)));
							break;
					}
				}
			}
			components = compsToAdd.ToArray();
		}
	}

	public class ComponentSelector : Selector<CGComponent>
	{
		public ComponentSelector (string selectionClause, List<CGComponent> pool = null)
		{
			if (pool == null)
				pool = Match.GetAllComponents();
			this.pool = pool;
			System.Array.Sort(pool.ToArray(), CompareCardsByIndexIncreasing);
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionParameter<CGComponent>> compsToAdd = new List<SelectionParameter<CGComponent>>();

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
							if (Match.HasVariable(sub))
								compsToAdd.Add(new MatchStringVariableParameter(sub));
							else
								compsToAdd.Add(new CardIDParameter(sub));
							break;
						case 'z':
							if (Match.HasVariable(sub))
								compsToAdd.Insert(0, new CardZoneIDParameter(sub));
							else
								compsToAdd.Insert(0, new CardZoneTagParameter(new NestedStrings(sub)));
							break;
						case 't':
							compsToAdd.Add(new CardTagParameter(new NestedStrings(sub)));
							break;
						//case 'r':
						//	compsToAdd.Add(new CardRuleTagComponent(new NestedStrings(sub)));
						//	break;
						case 'f':
							compsToAdd.Add(new CardFieldParameter(new NestedCardFieldConditions(sub)));
							break;
						case 'x':
							topQuantityGetter = Build(sub);
							break;
						case 'b':
							bottomQuantityGetter = Build(sub);
							break;
						//case 's':
						//	compsToAdd.Add(new CardZoneSlotComponent(Build(sub)));
						//	break;
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
				pool.Sort(CompareCardsByIndexIncreasing);
			else if (bottomQuantityGetter != null)
				pool.Sort(CompareCardsByIndexDecreasing);
			return base.Get();
		}

		public static int CompareCardsByIndexIncreasing (CGComponent c1, CGComponent c2)
		{
			if (c1.zone != null && c2.zone != null)
			{
				int c1Index = c1.zone.GetIndexOf(c1), c2Index = c2.zone.GetIndexOf(c2);
				if (c1Index < c2Index)
					return 1;
				else if (c1Index > c2Index)
					return -1;
				else
					return 0;
			}
			return 0;
		}

		public static int CompareCardsByIndexDecreasing (CGComponent c1, CGComponent c2)
		{
			if (c1.zone != null && c2.zone != null)
			{
				int c1Index = c1.zone.GetIndexOf(c1), c2Index = c2.zone.GetIndexOf(c2);
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