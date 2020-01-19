using System.Collections;
using System.Collections.Generic;

namespace CardGameFramework
{
	public abstract class Selector<T>
	{
		public abstract T[] Select (T[] from);
	}

	public class CardSelector : Selector<Card>
	{
		public CardSelector ()
		{

		}

		public override Card[] Select (Card[] from)
		{
			return null;
		}
	}
}