﻿using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	public abstract class Getter
	{
		public static Getter Build (string builder)
		{
			Getter getter = null;
			//A simple number
			if (float.TryParse(builder, out float parsed))
			{
				getter = new NumberGetter(parsed); //NUMBER
			}
			//math operations
			else if (builder.Contains("-") || builder.Contains("+") || builder.Contains("*") || builder.Contains("/") || builder.Contains("%") || builder.Contains("^"))
			{
				getter = new MathGetter(builder); //NUMBER
			}
			//card selection count
			else if (builder.StartsWith("nc("))
			{
				getter = new CardSelectionCountGetter(builder); //NUMBER
			}
			//card selection
			else if (builder.StartsWith("c(") || builder == "allcards")
			{
				getter = new CardSelector(builder); //SELECTION
			}
			//card field
			else if (builder.StartsWith("cf("))
			{
				getter = new CardFieldGetter(builder); //NUMBER OR STRING
			}
			//zone selection count
			else if (builder.StartsWith("nz("))
			{
				getter = new ZoneSelectionCountGetter(builder); //NUMBER
			}
			//zone selection
			else if (builder.StartsWith("z(") || builder == "allzones")
			{
				getter = new ZoneSelector(builder); //SELECTION
			}
			//system variables
			else if (Match.Current && Match.Current.HasVariable(builder))
			{
				getter = new MatchVariableGetter(builder); //NUMBER , STRING OR CARD
			}
			else
				getter = new StringGetter(builder); //STRING

			UnityEngine.Debug.Log("DEBUG  = = = = = " + getter.GetType() + "  =>  " + getter.ToString());
			return getter;
		}

		public abstract object Get ();

		public static bool operator== (Getter a, Getter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return a.Get() == b.Get();
		}

		public static bool operator != (Getter a, Getter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return a.Get() != b.Get();
		}

		public override bool Equals (object obj)
		{
			if (obj.GetType() == typeof(Getter))
				return Get() == ((Getter)obj).Get();
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
			return Match.Current.GetVariable(variableName);
		}
	}

	//public class MatchCardVariableGetter : CardGetter
	//{
	//	string variableName;

	//	public MatchCardVariableGetter (string variableName)
	//	{
	//		this.variableName = variableName;
	//	}

	//	public override object Get ()
	//	{
	//		card = Match.Current.GetCardVariable(variableName);
	//		return card;
	//	}
	//}

	public class StringGetter : Getter
	{
		public string value;

		public StringGetter (string value)
		{
			this.value = value;
		}

		public override object Get ()
		{
			if (Match.Current && Match.Current.HasVariable(value))
				return Match.Current.GetVariable(value);
			return value;
		}

		public static bool operator == (StringGetter a, StringGetter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return (string)a.Get() == (string)b.Get();
		}

		public static bool operator != (StringGetter a, StringGetter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return (string)a.Get() != (string)b.Get();
		}

		public override bool Equals (object obj)
		{
			if (obj == null) return false;
			if (obj.GetType() == typeof(StringGetter))
				return (string)Get() == (string)((StringGetter)obj).Get();
			return base.Equals(obj);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
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

		public static bool operator> (NumberGetter a, NumberGetter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return (float)a.Get() > (float)b.Get();
		}

		public static bool operator >= (NumberGetter a, NumberGetter b)
		{
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
			return (float)a.Get() >= (float)b.Get();
		}

		public static bool operator< (NumberGetter a, NumberGetter b)
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
			if (obj == null) return false;
			if (obj.GetType() == typeof(NumberGetter))
				return (float)Get() == (float)((NumberGetter)obj).Get();
			return base.Equals(obj);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}
	}




	public class MathGetter : NumberGetter
	{
		Getter[] getters;
		string[] operators;
		bool firstIsOperator = false;
		StringBuilder sb = new StringBuilder();

		public MathGetter (string builder)
		{
			builder = StringUtility.GetCleanStringForInstructions(builder);

			List<Getter> gettersList = new List<Getter>();
			List<string> operatorsList = new List<string>();
			bool lastWasOp = true;
			string currentString = "";
			int startIndex = 0;
			int endIndex = builder.Length - 1;
			for (int i = 0; i < builder.Length; i++)
			{
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
						if (i == builder.Length - 1)
							gettersList.Add(Build(builder.Substring(startIndex, endIndex - startIndex + 1)));
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
			float result = 0;
			UnityEditor.ExpressionEvaluator.Evaluate(sb.ToString(), out result);
			return result;
		}
	}


	public class ZoneSelectionCountGetter : NumberGetter
	{
		ZoneSelector selector;

		public ZoneSelectionCountGetter (string selectionClause, Zone[] pool = null)
		{
			selector = new ZoneSelector(selectionClause, pool);
		}

		public override object Get ()
		{
			return value = selector.GetSelectionCount();
		}
	}

	public class CardSelectionCountGetter : NumberGetter
	{
		CardSelector selector;

		public CardSelectionCountGetter (string selectionClause, Card[] pool = null)
		{
			selector = new CardSelector(selectionClause, pool);
		}

		public override object Get ()
		{
			return value = selector.GetSelectionCount();
		}
	}

	public class CardFieldGetter : Getter
	{
		public string fieldName;
		public CardSelector selector;
		
		public CardFieldGetter (string builder)
		{
			string[] builderBreakdown = StringUtility.ArgumentsBreakdown(builder);
			int fieldNameStart = builder.IndexOf('(') + 1;
			fieldName = builder.Substring(fieldNameStart, builder.IndexOf(',') - fieldNameStart);
			string selectorString = builder.Replace("cf(", "c(").Replace(fieldName + ",", "");
			selector = new CardSelector(selectorString);
		}

		public override object Get ()
		{
			Card[] selection = (Card[])selector.Get();
			if (selection.Length > 0)
			{
				Card card = selection[0];
				if (card.GetFieldDataType(fieldName) == CardFieldDataType.Number)
					return card.GetNumFieldValue(fieldName);
				else if (card.GetFieldDataType(fieldName) == CardFieldDataType.Text)
					return card.GetTextFieldValue(fieldName);
			}
			UnityEngine.Debug.LogWarning(StringUtility.BuildMessage("Error trying to get value from field " + fieldName));
			return null;
		}
	}

	//public class ZoneCardCountGetter : NumberGetter
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
