using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	public abstract class Selector<T>
	{
		public abstract T[] Select (T[] from);
	}

	public class ZoneSelector : Selector<Zone>
	{
		SelectionComponent<Zone>[] components;

		bool selectAll = false;
		// TODO
		//string id = "";
		//string[] zoneTags = null;
		//int quantityOfCards = -1;

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
							//compsToAdd.Add(new ZoneTagComponent(new NestedStrings(sub)));
							break;
					}
				}
			}
		}

		public override Zone[] Select (Zone[] from)
		{
			return null;
		}
	}

	public class CardSelector : Selector<Card>
	{
		SelectionComponent<Card>[] components;

		bool selectAll = false;
		int quantity = int.MaxValue;

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

		public int GetSelectionCount (Card[] from)
		{
			if (selectAll) return from.Length;
			int counter = 0;
			for (int i = 0; i < from.Length; i++)
			{
				Card card = from[i];
				if (IsAMatch(card))
					counter++;
			}
			return counter;
		}

		public override Card[] Select (Card[] from)
		{
			if (selectAll) return from;

			List<Card> selected = new List<Card>();
			for (int i = 0; i < from.Length && i < quantity; i++)
			{
				Card card = from[i];
				if (IsAMatch(card))
					selected.Add(card);
			}
			return selected.ToArray();
		}

		bool IsAMatch (Card card)
		{
			for (int i = 0; i < components.Length; i++)
			{
				if (!components[i].Match(card))
					return false;
			}
			return true;
		}
	}

	

}