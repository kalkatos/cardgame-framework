using System.Collections;
using System.Collections.Generic;

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
			//card related
			else if (builder.StartsWith("card") || builder.StartsWith("c"))
			{
				//card in context OR selection count OR card field OR card selection
			}
			//zone related
			else if (builder.StartsWith("zone") || builder.StartsWith("z"))
			{
				//zone in context OR number of cards in zone(s) OR zone selection
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

		//public static double operator+ (NumberGetter left, NumberGetter right)
		//{
		//	return left.value + right.value;
		//}
	}




	public class MathGetter : NumberGetter
	{
		protected NumberGetter sub;
		protected NumberGetter next;

		public MathGetter (string builder)
		{
			builder = StringUtility.GetCleanStringForInstructions(builder);

			for (int i = 0; i < builder.Length; i++)
			{
				char c = builder[i];
				switch (c)
				{
					case '(':

						break;
					default:
						break;
				}
			}
		}

		public MathGetter () 	{ }

		public override object Get ()
		{
			value = (double)next.Get();
			return base.Get();
		}
	}

	public class SumGetter : MathGetter
	{
		public override object Get ()
		{
			if (sub != null) value = (double)sub.Get();
			if (next != null) value += (double)next.Get();
			return value;
		}
	}

	public class DifferenceGetter : MathGetter
	{
		public override object Get ()
		{
			if (sub != null) value = (double)sub.Get();
			if (next != null) value -= (double)next.Get();
			return value;
		}
	}

	public class MultiplicationGetter : MathGetter
	{
		public override object Get ()
		{
			if (sub != null) value = (double)sub.Get();
			if (next != null) value *= (double)next.Get();
			return value;
		}
	}

	public class DivisionGetter : MathGetter
	{
		public override object Get ()
		{
			if (sub != null) value = (double)sub.Get();
			if (next != null) value /= (double)next.Get();
			return value;
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

	public class MatchVariableGetter : NumberGetter
	{
		string variableName;

		public override object Get ()
		{
			value = Match.Current.GetVariable(variableName);
			return value;
		}
	}
}
