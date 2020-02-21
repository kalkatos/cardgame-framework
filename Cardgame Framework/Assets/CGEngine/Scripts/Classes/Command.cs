using System.Collections;

namespace CardGameFramework
{
	public delegate IEnumerator SimpleMethod ();
	public delegate IEnumerator IntMethod (int integer);
	public delegate IEnumerator StringMethod (string str);
	public delegate IEnumerator ZoneMethod (ZoneSelector zoneSelector);
	public delegate IEnumerator SpecialClickCardMethod (Card card);
	public delegate IEnumerator SpecialClickZoneMethod (Zone zone);
	public delegate IEnumerator CardMethod (CardSelector cardSelector);
	public delegate IEnumerator CardZoneMethod (CardSelector cardSelector, ZoneSelector zoneSelector, string[] additionalParams);
	public delegate IEnumerator CardFieldMethod (CardSelector cardSelector, string fieldName, Getter value, Getter minValue, Getter maxValue);
	public delegate IEnumerator VariableMethod (string variableName, Getter value, Getter minValue, Getter maxValue);

	public enum CommandType
	{
		EndCurrentPhase,
		MoveCardToZone,
		Shuffle,
		UseAction,
		EndTheMatch,
		SendMessage,
		StartSubphaseLoop,
		EndSubphaseLoop,
		SetCardFieldValue,
		SetVariable,
		UseCard,
		UseZone
	}

	public abstract class Command
	{
		protected CommandType type;
		public abstract IEnumerator Execute ();
	}

	//public class CommandList
	//{
	//	Command[] list;

	//	public CommandList (params Command[] commands)
	//	{
	//		list = commands;
	//	}

	//	public IEnumerator Execute ()
	//	{
	//		foreach (Command item in list)
	//		{
	//			yield return item.Execute();
	//		}
	//	}
	//}

	// EndCurrentPhase, EndTheMatch, EndSubphaseLoop
	public class SimpleCommand : Command
	{
		SimpleMethod method;

		public SimpleCommand (CommandType type, SimpleMethod method)
		{
			this.type = type;
			this.method = method;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke();
		}

		public override string ToString ()
		{
			return method.Method.ToString();
		}
	}

	// UseAction, SendMessage, StartSubphaseLoop
	public class StringCommand : Command
	{
		StringMethod method;
		public string strParameter;

		public StringCommand (CommandType type, StringMethod method, string strParameter)
		{
			this.type = type;
			this.method = method;
			this.strParameter = strParameter;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(strParameter);
		}

		public override string ToString ()
		{
			return method.Method.ToString();
		}
	}

	//Shuffle
	public class ZoneCommand : Command
	{
		ZoneMethod method;
		ZoneSelector zoneSelector;

		public ZoneCommand (CommandType type, ZoneMethod method, ZoneSelector zoneSelector)
		{
			this.type = type;
			this.zoneSelector = zoneSelector;
			this.method = method;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(zoneSelector);
		}

		public override string ToString ()
		{
			return method.Method.ToString();
		}
	}

	//UseCard
	public class CardCommand : Command
	{
		CardMethod method;
		CardSelector cardSelector;

		public CardCommand (CommandType type, CardMethod method, CardSelector cardSelector)
		{
			this.type = type;
			this.method = method;
			this.cardSelector = cardSelector;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(cardSelector);
		}

		public override string ToString ()
		{
			return method.Method.ToString();
		}
	}

	public class SpecialUseCardCommand : Command
	{
		Card card;
		SpecialClickCardMethod method;

		public SpecialUseCardCommand (SpecialClickCardMethod method)
		{
			type = CommandType.UseCard;
			this.method = method;
		}

		public void SetCard (Card c)
		{
			card = c;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(card);
		}

		public override string ToString ()
		{
			return method.Method.ToString();
		}
	}

	public class SpecialUseZoneCommand : Command
	{
		Zone zone;
		SpecialClickZoneMethod method;

		public SpecialUseZoneCommand (SpecialClickZoneMethod method)
		{
			type = CommandType.UseZone;
			this.method = method;
		}

		public void SetZone (Zone z)
		{
			zone = z;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(zone);
		}

		public override string ToString ()
		{
			return method.Method.ToString();
		}
	}

	//MoveCardToZone
	public class CardZoneCommand : Command
	{
		CardZoneMethod method;
		CardSelector cardSelector;
		ZoneSelector zoneSelector;
		string[] additionalParams;

		public CardZoneCommand (CommandType type, CardZoneMethod method, CardSelector cardSelector, ZoneSelector zoneSelector, string[] additionalParams = null)
		{
			this.type = type;
			this.method = method;
			this.cardSelector = cardSelector;
			this.zoneSelector = zoneSelector;
			this.additionalParams = additionalParams;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(cardSelector, zoneSelector, additionalParams);
		}

		public override string ToString ()
		{
			return method.Method.ToString();
		}
	}

	//SetCardFieldValue
	public class CardFieldCommand : Command
	{
		CardFieldMethod method;
		CardSelector cardSelector;
		string fieldName;
		Getter valueGetter;
		Getter minValue;
		Getter maxValue;

		public CardFieldCommand (CommandType type, CardFieldMethod method, CardSelector cardSelector, string fieldName, Getter valueGetter, Getter minValue, Getter maxValue)
		{
			this.method = method;
			this.cardSelector = cardSelector;
			this.fieldName = fieldName;
			this.valueGetter = valueGetter;
			this.minValue = minValue;
			this.maxValue = maxValue;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(cardSelector, fieldName, valueGetter, minValue, maxValue);
		}

		public override string ToString ()
		{
			return method.Method.ToString();
		}
	}

	//SetVariable
	public class VariableCommand : Command
	{
		VariableMethod method;
		string variableName;
		Getter value;
		Getter minValue;
		Getter maxValue;

		public VariableCommand (CommandType type, VariableMethod method, string variableName, Getter value, Getter minValue, Getter maxValue)
		{
			this.method = method;
			this.variableName = variableName;
			this.value = value;
			this.minValue = minValue;
			this.maxValue = maxValue;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(variableName, value, minValue, maxValue);
		}

		public override string ToString ()
		{
			return method.Method.ToString();
		}
	}
}