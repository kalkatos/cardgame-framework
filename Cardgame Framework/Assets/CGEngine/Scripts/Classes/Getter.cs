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
		public override double Get ()
		{
			throw new System.NotImplementedException();
		}
	}

	public class BooleanGetter : Getter<bool>
	{
		public override bool Get ()
		{
			throw new System.NotImplementedException();
		}
	}
}
