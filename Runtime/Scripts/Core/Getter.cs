using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CardgameFramework
{
	public abstract class Getter
	{
		public string builderStr { get; protected set; }
		public char opChar = '\0';

		public static Getter Build (string builder)
		{
			if (string.IsNullOrEmpty(builder))
				return null;
			Getter getter = null;
			char firstChar = builder[0];
			if (firstChar == '+' || firstChar == '*' || firstChar == '/' || firstChar == '%' || firstChar == '^')
				builder = builder.Substring(1);
			else
				firstChar = '\0';

			//a string value, not any other value getter
			if (Match.HasVariable(builder))
			{
				//if (builder.EndsWith("Card"))
				getter = new MatchVariableGetter(builder); //NUMBER OR STRING
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//A simple number
			else if (float.TryParse(builder, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsed))
			{
				getter = new NumberGetter(parsed); //NUMBER
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//math operation
			else if (builder.Contains("-") || builder.Contains("+") || builder.Contains("*") || builder.Contains("/") || builder.Contains("%") || builder.Contains("^"))
			{
				getter = new MathGetter(builder); //NUMBER
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//card selection count
			else if (builder.StartsWith("nc("))
			{
				getter = new CardSelectionCountGetter(builder, Match.GetAllCards()); //NUMBER
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//card selection
			else if (builder.StartsWith("c(") || builder == "allcards")
			{
				getter = new CardSelector(builder, Match.GetAllCards()); //SELECTION
			}
			//card field
			else if (builder.StartsWith("cf("))
			{
				getter = new CardFieldGetter(builder); //NUMBER OR STRING
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//card index
			else if (builder.StartsWith("ic("))
			{
				getter = new CardIndexGetter(builder); //NUMBER
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//zone selection count
			else if (builder.StartsWith("nz("))
			{
				getter = new ZoneSelectionCountGetter(builder, Match.GetAllZones()); //NUMBER
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//random number
			else if (builder.StartsWith("rn("))
			{
				getter = new RandomNumberGetter(builder); //NUMBER
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//zone selection
			else if (builder.StartsWith("z(") || builder == "allzones")
			{
				getter = new ZoneSelector(builder, Match.GetAllZones()); //SELECTION
			}
			//Rule selection
			else if (builder.StartsWith("r(") || builder == "allrules")
			{
				getter = new RuleSelector(builder, Match.GetAllRules()); //SELECTION
			}
			//if nothing else, a simple string
			else
				getter = new StringGetter(builder); //STRING

			getter.builderStr = builder;
			return getter;
		}

		public abstract object Get ();

		public static bool operator== (Getter a, Getter b)
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
				return true;
			else if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			object left = a.Get(), right = b.Get();
			if (left is float && right is float)
				return (float)left == (float)right;
			if (left is string && right is string)
				return (string)left == (string)right;
			return a.Get() == b.Get();
		}

		public static bool operator != (Getter a, Getter b)
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
				return false;
			else if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return true;
			object left = a.Get(), right = b.Get();
			if (left is float && right is float)
				return (float)left != (float)right;
			if (left is string && right is string)
				return (string)left != (string)right;
			return a.Get() != b.Get();
		}

		public override bool Equals (object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}
	}

	public class CardGetter : Getter
	{
		public Card card;

		public override object Get ()
		{
			return card;
		}
	}

	public class MatchVariableGetter : Getter
	{
		string variableName;

		public MatchVariableGetter (string variableName)
		{
			this.variableName = variableName;
		}

		public override object Get ()
		{
			return Match.GetVariable(variableName);
		}

		public override string ToString ()
		{
			return "MatchVariableGetter:" + variableName;
		}
	}

	public class CardVariableGetter : CardGetter
	{
		string variableName;

		public CardVariableGetter (string variableName)
		{
			this.variableName = variableName;
		}

		public override object Get ()
		{
			card = Match.GetCardByID(variableName);
			return card;
		}

		public override string ToString ()
		{
			return "CardVariableGetter:" + variableName;
		}
	}

	public class StringGetter : Getter
	{
		public string value;

		public StringGetter (string value)
		{
			this.value = value;
		}

		public override object Get ()
		{
			return value;
		}

		public static bool operator == (StringGetter a, StringGetter b)
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
				return true;
			else if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return (string)a.Get() == (string)b.Get();
		}

		public static bool operator != (StringGetter a, StringGetter b)
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
				return false;
			else if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return true;
			return (string)a.Get() != (string)b.Get();
		}

		public override bool Equals (object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}

		public override string ToString ()
		{
			return "StringGetter:" + value;
		}
	}

	public class NumberGetter : Getter
	{
		public float value;

		public NumberGetter () { }

		public NumberGetter (float value)
		{
			this.value = value;
		}

		public override object Get ()
		{
			return value;
		}

		public static bool operator > (NumberGetter a, NumberGetter b)
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
				return true;
			else if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return (float)a.Get() > (float)b.Get();
		}

		public static bool operator >= (NumberGetter a, NumberGetter b)
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
				return false;
			else if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return true;
			return (float)a.Get() >= (float)b.Get();
		}

		public static bool operator < (NumberGetter a, NumberGetter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return (float)a.Get() < (float)b.Get();
		}

		public static bool operator <= (NumberGetter a, NumberGetter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return (float)a.Get() <= (float)b.Get();
		}

		public static bool operator == (NumberGetter a, NumberGetter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return (float)a.Get() == (float)b.Get();
		}

		public static bool operator != (NumberGetter a, NumberGetter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return (float)a.Get() != (float)b.Get();
		}

		public override bool Equals (object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}

		public override string ToString ()
		{
			return "NumberGetter:" + value;
		}
	}

	public class MathGetter : NumberGetter
	{
		public Getter[] getters { get; private set; }
		string[] operators;
		bool firstIsOperator = false;
		StringBuilder sb = new StringBuilder();
		string builder;

		public MathGetter (string builder)
		{
			this.builder = builder;
			builder = StringUtility.GetCleanStringForInstructions(builder);

			List<Getter> gettersList = new List<Getter>();
			List<string> operatorsList = new List<string>();
			bool lastWasOp = true;
			string currentString = "";
			int startIndex = 0;
			int endIndex = builder.Length - 1;
			for (int i = 0; i <= builder.Length; i++)
			{
				if (i == builder.Length)
				{
					if (i > startIndex)
						gettersList.Add(Build(builder.Substring(startIndex, i - startIndex)));
					break;
				}
				char c = builder[i];
				switch (c)
				{
					case '(':
					case ')':
					case '+':
					case '-':
					case '*':
					case '/':
					case '%':
					case '^':
						if (c == '(')
						{
							int closingPar = StringUtility.GetClosingParenthesisIndex(builder, i);
							if (StringUtility.GetOperator(builder.Substring(i, closingPar - i), StringUtility.MathOperators) == "")
							{
								i = endIndex = closingPar;
								continue;
							}
						}
						if (i == 0) firstIsOperator = true;
						currentString += c;
						if (!lastWasOp)
						{
							gettersList.Add(Build(builder.Substring(startIndex, endIndex - startIndex + 1)));
						}
						startIndex = i + 1;
						lastWasOp = true;
						if (i == builder.Length - 1)
							operatorsList.Add(currentString);
						break;
					default:
						endIndex = i;
						if (currentString != "")
						{
							operatorsList.Add(currentString);
							currentString = "";
						}
						lastWasOp = false;
						break;
				}
			}
			getters = gettersList.ToArray();
			operators = operatorsList.ToArray();
		}

		public override object Get ()
		{
			sb.Clear();
			int max = System.Math.Max(getters.Length, operators.Length);
			for (int i = 0; i < max; i++)
			{
				if (firstIsOperator)
				{
					if (i < operators.Length)
						sb.Append(operators[i]);
					if (i < getters.Length)
						sb.Append(getters[i].Get());
				}
				else
				{
					if (i < getters.Length)
						sb.Append(getters[i].Get());
					if (i < operators.Length)
						sb.Append(operators[i]);
				}
			}
			float result = ExpressionEvaluator.Evaluate(sb.ToString());
			return result;
		}

		public override string ToString ()
		{
			return "MathGetter:" + builder;
		}
	}


	public class ZoneSelectionCountGetter : NumberGetter
	{
		ZoneSelector selector;

		public ZoneSelectionCountGetter (string selectionClause, List<Zone> pool = null)
		{
			selector = new ZoneSelector(selectionClause, pool);
		}

		public override object Get ()
		{
			return value = selector.GetSelectionCount();
		}

		public override string ToString ()
		{
			return "ZoneSelectionCountGetter";
		}
	}

	public class CardSelectionCountGetter : NumberGetter
	{
		CardSelector selector;

		public CardSelectionCountGetter (string selectionClause, List<Card> pool = null)
		{
			selector = new CardSelector(selectionClause, pool);
		}

		public override object Get ()
		{
			return value = selector.GetSelectionCount();
		}

		public override string ToString ()
		{
			return "CardSelectionCountGetter";
		}
	}

	public class CardFieldGetter : Getter
	{
		public string fieldName;
		public CardSelector selector;
		
		public CardFieldGetter (string builder)
		{ // cf(NameField,z:Play)
			int fieldNameStart = builder.IndexOf('(') + 1;
			fieldName = builder.Substring(fieldNameStart, builder.IndexOf(',') - fieldNameStart);
			string selectorString = builder.Replace("cf(", "c(").Replace(fieldName + ",", "");
			selector = new CardSelector(selectorString, Match.GetAllCards());
		}

		public override object Get ()
		{
			List<Card> selection = (List<Card>)selector.Get();
			if (selection.Count > 0)
			{
				Card card = selection[0];
				if (card.GetFieldDataType(fieldName) == FieldType.Number)
					return card.GetNumFieldValue(fieldName);
				else if (card.GetFieldDataType(fieldName) == FieldType.Text)
					return card.GetTextFieldValue(fieldName);
			}
			//Debug.LogWarning($"[CGEngine] Error trying to get value from field {fieldName} because the selection {builderStr} found no cards");
			return "";
		}

		public override string ToString ()
		{
			return "CardFieldGetter:" + fieldName;
		}
	}

	public class RandomNumberGetter : Getter
	{
		Getter from;
		Getter to;
		bool isInteger;
		public RandomNumberGetter(string builder)
		{
			string[] builderBreakdown = StringUtility.ArgumentsBreakdown(builder);
			if (builderBreakdown.Length == 3)
			{
				string fromString = builderBreakdown[1];
				string toString = builderBreakdown[2];
				//if (!fromString.Contains("f"))
				//	fromString = fromString + "f";
				//if (!toString.Contains("f"))
				//	toString = toString + "f";
				from = Build(fromString);
				to = Build(toString);
				isInteger = !builder.Contains(".");
			}
		}

		public override object Get ()
		{
			if (from == null)
				return 0;
			object fromValue = from.Get();
			object toValue = to.Get();
			if (isInteger)
				return Random.Range((int)(float)fromValue, (int)(float)toValue + 1);
			return Random.Range((float)fromValue, (float)toValue);
		}
	}

	public class CardIndexGetter : Getter
	{
		public CardSelector selector;

		public CardIndexGetter (string builder)
		{
			selector = new CardSelector(builder.Replace("ic(", "c("), Match.GetAllCards());
		}

		public override object Get ()
		{
			List<Card> selection = (List<Card>)selector.Get();
			if (selection.Count > 0)
			{
				Card card = selection[0];
				if (card.Zone)
					return card.Zone.GetIndexOf(card);
			}
			return -1;
		}

		public override string ToString ()
		{
			return "CardIndexGetter";
		}
	}

	//public class ZoneCardCountGetter : NumberGetter //NOTE Unnecessary because we can just use "nc(z:Tag)"
	//{
	//	ZoneSelector selector;
	//	public ZoneCardCountGetter (string selectionClause, Zone[] pool = null)
	//	{
	//		selector = new ZoneSelector(selectionClause, pool);
	//	}
	//	public override object Get ()
	//	{
	//		value = 0;
	//		Zone[] selected = (Zone[])selector.Get();
	//		for (int i = 0; i < selected.Length; i++)
	//		{
	//			value += selected[i].Content.Count;
	//		}
	//		return value;
	//	}
	//}
}
