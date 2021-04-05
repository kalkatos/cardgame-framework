using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CardgameCore
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
			//component selection count
			else if (builder.StartsWith("nc("))
			{
				getter = new ComponentSelectionCountGetter(builder); //NUMBER
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//component selection
			else if (builder.StartsWith("c(") || builder == "allcomponents")
			{
				getter = new ComponentSelector(builder); //SELECTION
			}
			//component field
			else if (builder.StartsWith("cf("))
			{
				getter = new ComponentFieldGetter(builder); //NUMBER OR STRING
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//component index
			else if (builder.StartsWith("ic("))
			{
				getter = new ComponentIndexGetter(builder); //NUMBER
				if (firstChar != '\0') getter.opChar = firstChar;
			}
			//zone selection count
			else if (builder.StartsWith("nz("))
			{
				getter = new ZoneSelectionCountGetter(builder); //NUMBER
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
				getter = new ZoneSelector(builder); //SELECTION
			}
			//Rule selection
			else if (builder.StartsWith("r(") || builder == "allrules")
			{
				getter = new RuleSelector(builder); //SELECTION
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

	public class ComponentGetter : Getter
	{
		public CGComponent component;

		public override object Get ()
		{
			return component;
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

	public class ComponentVariableGetter : ComponentGetter
	{
		string variableName;

		public ComponentVariableGetter (string variableName)
		{
			this.variableName = variableName;
		}

		public override object Get ()
		{
			component = Match.GetComponentByID(variableName);
			return component;
		}

		public override string ToString ()
		{
			return "ComponentVariableGetter:" + variableName;
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
							if (StringUtility.GetOperator(builder.Substring(i, closingPar - i), StringUtility.mathOperators) == "")
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

	public class ComponentSelectionCountGetter : NumberGetter
	{
		ComponentSelector selector;

		public ComponentSelectionCountGetter (string selectionClause, List<CGComponent> pool = null)
		{
			selector = new ComponentSelector(selectionClause, pool);
		}

		public override object Get ()
		{
			return value = selector.GetSelectionCount();
		}

		public override string ToString ()
		{
			return "ComponentSelectionCountGetter";
		}
	}

	public class ComponentFieldGetter : Getter
	{
		public string fieldName;
		public ComponentSelector selector;
		
		public ComponentFieldGetter (string builder)
		{ // cf(NameField,z:Play)
			int fieldNameStart = builder.IndexOf('(') + 1;
			fieldName = builder.Substring(fieldNameStart, builder.IndexOf(',') - fieldNameStart);
			string selectorString = builder.Replace("cf(", "c(").Replace(fieldName + ",", "");
			selector = new ComponentSelector(selectorString);
		}

		public override object Get ()
		{
			List<CGComponent> selection = (List<CGComponent>)selector.Get();
			if (selection.Count > 0)
			{
				CGComponent component = selection[0];
				if (component.GetFieldDataType(fieldName) == FieldType.Number)
					return component.GetNumFieldValue(fieldName);
				else if (component.GetFieldDataType(fieldName) == FieldType.Text)
					return component.GetTextFieldValue(fieldName);
			}
			Debug.LogWarning($"[CGEngine] Error trying to get value from field {fieldName} because the selection {builderStr} found no components");
			return "";
		}

		public override string ToString ()
		{
			return "ComponentFieldGetter:" + fieldName;
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

	public class ComponentIndexGetter : Getter
	{
		public ComponentSelector selector;

		public ComponentIndexGetter (string builder)
		{
			selector = new ComponentSelector(builder.Replace("ic(", "c("));
		}

		public override object Get ()
		{
			List<CGComponent> selection = (List<CGComponent>)selector.Get();
			if (selection.Count > 0)
			{
				CGComponent comp = selection[0];
				if (comp.Zone)
					return comp.Zone.GetIndexOf(comp);
			}
			return -1;
		}

		public override string ToString ()
		{
			return "ComponentIndexGetter";
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
