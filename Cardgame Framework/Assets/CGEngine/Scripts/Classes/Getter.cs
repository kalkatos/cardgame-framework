using System.Collections;
using System.Collections.Generic;

namespace CardGameFramework
{
	public abstract class Getter<T>
	{
		public abstract T Get ();
	}

	public class NumberGetter : Getter<double>
	{
		public double value;

		public NumberGetter () { }

		public NumberGetter (double value)
		{
			this.value = value;
		}

		public override double Get ()
		{
			return value;
		}
	}

	public class BooleanGetter : Getter<bool>
	{
		public override bool Get ()
		{
			return false;
		}
	}

	public class CardSelectionCountGetter : NumberGetter
	{
		CardSelector selector;

		public CardSelectionCountGetter (Card[] pool, string selectionClause)
		{
			selector = new CardSelector(pool, selectionClause);
		}

		public override double Get ()
		{
			return value = selector.GetSelectionCount();
		}
	}


}
