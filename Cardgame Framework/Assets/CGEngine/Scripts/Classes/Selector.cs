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

		// TODO
		//string id = "";
		//string[] zoneTags = null;
		//int quantityOfCards = -1;

		public ZoneSelector (string selectionClause)
		{

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
						case 'f':
						case '.':

							break;
						case 'x':
							if (!int.TryParse(sub, out quantity))
							{
								//It's not a number
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

	public class NumberGetter
	{

	}

	public abstract class SelectionComponent<T>
	{
		public abstract bool Match (T obj);
	}

	public class CardIDComponent : SelectionComponent<Card>
	{
		public string value;

		public CardIDComponent (string value)
		{
			this.value = value;
		}

		public override bool Match (Card card)
		{
			return card.ID == value;
		}
	}

	public class CardTextFieldComponent : SelectionComponent<Card>
	{
		public string fieldName;
		public string textFieldValue;

		public CardTextFieldComponent (string fieldName, string textFieldValue)
		{
			this.fieldName = fieldName;
			this.textFieldValue = textFieldValue;
		}

		public override bool Match (Card card)
		{
			return card.HasField(fieldName) && card.GetTextFieldValue(fieldName) == textFieldValue;
		}
	}

	public class CardNumFieldComponent : SelectionComponent<Card>
	{
		public string fieldName;
		public double numFieldValue;
		public ComparisonOperator op;

		public CardNumFieldComponent (string fieldName, double numFieldValue, ComparisonOperator op)
		{
			this.fieldName = fieldName;
			this.numFieldValue = numFieldValue;
		}

		public override bool Match (Card card)
		{
			if (!card.HasField(fieldName))
				return false;
			double cardValue = card.GetNumFieldValue(fieldName);
			return
				(op == ComparisonOperator.Equal && cardValue == numFieldValue) ||
				(op == ComparisonOperator.LessThan && cardValue < numFieldValue) ||
				(op == ComparisonOperator.GreaterThan && cardValue > numFieldValue) ||
				(op == ComparisonOperator.LessThanOrEqualTo && cardValue <= numFieldValue) ||
				(op == ComparisonOperator.GreaterThanOrEqualTo && cardValue >= numFieldValue) ||
				(op == ComparisonOperator.Unequal && cardValue != numFieldValue);
		}
	}

	public class CardTagComponent : SelectionComponent<Card>
	{
		NestedStrings tags;

		public CardTagComponent (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool Match (Card obj)
		{
			tags.PrepareEvaluation(obj.data.tags.Split(','));
			return tags.Evaluate();
		}
	}

	public class CardZoneTagComponent : SelectionComponent<Card>
	{
		NestedStrings tags;

		public CardZoneTagComponent (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool Match (Card obj)
		{
			if (obj.zone != null)
			{
				tags.PrepareEvaluation(obj.zone.zoneTags.Split(','));
				return tags.Evaluate();
			}
			return false;
		}
	}

	public class CardModifierTagComponent : SelectionComponent<Card>
	{
		NestedStrings tags;

		public CardModifierTagComponent (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool Match (Card obj)
		{
			if (obj.zone != null)
			{
				tags.PrepareEvaluation(obj.GetTagsFromModifiers().Split(','));
				return tags.Evaluate();
			}
			return false;
		}
	}

	public class NestedStrings : NestedBooleans
	{
		public string myString = "§";

		public NestedStrings () { }

		public NestedStrings(string buildingStr)
		{
			Build(buildingStr);
		}

		public void Build (string clause)
		{
			int strStart = 0;
			int strEnd = clause.Length;
			NestedStrings currentString = this;
			NestedStrings rootString = null;
			for (int i = 0; i < clause.Length; i++)
			{
				char c = clause[i];
				switch (c)
				{
					case '(':
						strStart = i + 1;
						break;
					case ')':
						if (i == 0) return;  //clause is wrong (may not start with closing parenthesis)
						currentString.and = null;
						currentString.or = null;
						currentString.subAnd = null;
						currentString.subOr = null;
						strEnd = clause[i - 1] == ')' ? strEnd : i;
						currentString.myString = clause.Substring(strStart, strEnd - strStart);
						break;
					case '&':
					case '|':
						if (i == 0 || i == clause.Length - 1) return; //clause is wrong (may not start or end with an operator)
						bool itsAnd = c == '&';
						bool closingParenthesis = clause[i - 1] == ')';

						if (closingParenthesis)
						{
							if (rootString != null)
								currentString = rootString;
						}
						else
						{
							strEnd = i;
							currentString.myString = clause.Substring(strStart, strEnd - strStart);
						}

						NestedStrings newString = new NestedStrings();
						if (clause[i + 1] == '(')
						{
							rootString = currentString;
							currentString.subAnd = itsAnd ? newString : null;
							currentString.subOr = itsAnd ? null : newString;
							currentString = newString;
						}
						else
						{
							currentString.and = itsAnd ? newString : null;
							currentString.or = itsAnd ? null : newString;
							currentString = newString;
						}

						strStart = i + 1;
						break;
					default:
						if (i == clause.Length - 1)
						{
							currentString.and = null;
							currentString.or = null;
							currentString.subAnd = null;
							currentString.subOr = null;
							strEnd = (i > 0 && clause[i - 1] != ')') ? clause.Length : strEnd;
							currentString.myString = clause.Substring(strStart, strEnd - strStart);
						}
						break;
				}
			}
		}

		public void PrepareEvaluation (string[] compareToStrings)
		{
			myBoolean = false;
			for (int i = 0; i < compareToStrings.Length; i++)
			{
				if (compareToStrings[i] == myString)
				{
					myBoolean = true;
					break;
				}
			}
			if (subAnd != null) ((NestedStrings)subAnd).PrepareEvaluation(compareToStrings);
			if (subOr != null) ((NestedStrings)subOr).PrepareEvaluation(compareToStrings);
			if (and != null) ((NestedStrings)and).PrepareEvaluation(compareToStrings);
			if (or != null) ((NestedStrings)or).PrepareEvaluation(compareToStrings);
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(myString);
			if (subAnd != null)
			{
				sb.Append("&(");
				sb.Append(subAnd.ToString());
				sb.Append(")");
			}
			else if (subOr != null)
			{
				sb.Append("|(");
				sb.Append(subOr.ToString());
				sb.Append(")");
			}

			if (and != null)
			{
				sb.Append("&");
				sb.Append(and.ToString());
			}
			else if (or != null)
			{
				sb.Append("|");
				sb.Append(or.ToString());
			}
			return sb.ToString();
		}
	}

	public class NestedBooleans
	{
		public NestedBooleans subAnd;
		public NestedBooleans subOr;
		public NestedBooleans and;
		public NestedBooleans or;
		public bool myBoolean;

		public virtual bool Evaluate ()
		{
			if (subAnd != null)
				myBoolean = myBoolean & subAnd.Evaluate();
			else if (subOr != null)
				myBoolean = myBoolean | subOr.Evaluate();

			if (and != null)
				return myBoolean & and.Evaluate();
			else if (or != null)
				return myBoolean | or.Evaluate();
			else
				return myBoolean;
		}
	}
}