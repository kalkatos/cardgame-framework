using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	public abstract class Selector<T>
	{
		protected SelectionComponent<T>[] components;
		protected bool selectAll = false;
		protected int quantity = int.MaxValue;

		public virtual T[] Select (T[] from)
		{
			if (selectAll) return from;

			List<T> selected = new List<T>();
			for (int i = 0; i < from.Length && i < quantity; i++)
			{
				T obj = from[i];
				if (IsAMatch(obj))
					selected.Add(obj);
			}
			return selected.ToArray();
		}

		public int GetSelectionCount (T[] from)
		{
			if (selectAll) return from.Length;
			int counter = 0;
			for (int i = 0; i < from.Length; i++)
			{
				T card = from[i];
				if (IsAMatch(card))
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
	}

	public class ZoneSelector : Selector<Zone>
	{
		public ZoneSelector (string selectionClause)
		{
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionComponent<Zone>> compsToAdd = new List<SelectionComponent<Zone>>();
			if (clauseBreakdown[0] == "zone")
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
		}
	}

	public class CardSelector : Selector<Card>
	{
		public CardSelector (string selectionClause)
		{
			string[] clauseBreakdown = StringUtility.ArgumentsBreakdown(selectionClause);
			List<SelectionComponent<Card>> compsToAdd = new List<SelectionComponent<Card>>();
			if (clauseBreakdown[0] == "card")
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
							compsToAdd.Add(new CardIDComponent(sub));
							break;
						case '@':
						case 'z':
							compsToAdd.Add(new CardZoneTagComponent(new NestedStrings(sub)));
							break;
						case '~':
						case 't':
							compsToAdd.Add(new CardTagComponent(new NestedStrings(sub)));
							break;
						case '%':
						case 'm':
							compsToAdd.Add(new CardModifierTagComponent(new NestedStrings(sub)));
							break;
						case '.':
						case 'f':
							compsToAdd.Add(new CardFieldComponent(sub));
							break;
						case 'x':
							if (!int.TryParse(sub, out quantity))
							{
								//It's not a number TODO mudar para NumberGetter
							}
							break;
						default:
							break;
					}
				}
			}
			components = compsToAdd.ToArray();
		}
	}

}