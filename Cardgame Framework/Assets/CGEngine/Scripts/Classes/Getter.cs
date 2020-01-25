using System.Collections;
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
			if (double.TryParse(builder, out double parsed)) 
			{
				getter = new NumberGetter(parsed);
			}
			//math operations
			else if (builder.Contains("-") || builder.Contains("+") || builder.Contains("*") || builder.Contains("/") || builder.Contains("%") || builder.Contains("^"))
			{
				getter = new MathGetter(builder);
			}
			//system variables
			else if (CGEngine.IsSystemVariable(builder))
			{

			}
			//card related
			else if (builder.StartsWith("card") || builder.StartsWith("c"))
			{
				//card in context OR selection count OR card selection
			}
			//zone related
			else if (builder.StartsWith("zone") || builder.StartsWith("z"))
			{
				//zone in context OR number of cards in zone(s) OR zone selection
			}
			else if (builder.StartsWith("$"))
			{
				//card field OR variable
			}
			

			return getter;
		}

		public abstract object Get ();
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

	public class MatchContextCardGetter : CardGetter
	{
		string contextId;

		public MatchContextCardGetter (string contextId)
		{
			this.contextId = contextId;
		}

		public override object Get ()
		{
			card = Match.Current.GetContextCard(contextId);
			return card;
		}
	}

	public class NumberGetter : Getter
	{
		public double value;

		public NumberGetter () { }

		public NumberGetter (double value)
		{
			this.value = value;
		}

		public override object Get ()
		{
			return value;
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

	public class ZoneCardCountGetter : NumberGetter
	{
		ZoneSelector selector;

		public ZoneCardCountGetter (string selectionClause, Zone[] pool = null)
		{
			selector = new ZoneSelector(selectionClause, pool);
		}

		public override object Get ()
		{
			value = 0;
			Zone[] selected = (Zone[])selector.Get();
			for (int i = 0; i < selected.Length; i++)
			{
				value += selected[i].Content.Count;
			}
			return value;
		}
	}

	
}
