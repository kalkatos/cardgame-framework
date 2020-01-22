using System.Collections;

namespace CardGameFramework
{
	public delegate IEnumerator SimpleMethod ();
	public delegate IEnumerator StringMethod (string str);
	public delegate IEnumerator ZoneMethod (ZoneSelector zoneSelector);
	public delegate IEnumerator CardMethod (CardSelector cardSelector);
	public delegate IEnumerator CardZoneMethod (CardSelector cardSelector, ZoneSelector zoneSelector, string additionalParams);
	public delegate IEnumerator CardFieldMethod (CardSelector cardSelector, string fieldName, NumberGetter number);
	public delegate IEnumerator StringNumberMethod (string text, NumberGetter number);

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
		ClickCard
	}

	public abstract class Command
	{
		public CommandType type;
		public abstract IEnumerator Execute ();
	}

	public class CommandList
	{
		Command[] list;

		public CommandList (params Command[] commands)
		{
			list = commands;
		}

		public IEnumerator Execute ()
		{
			foreach (Command item in list)
			{
				yield return item.Execute();
			}
		}
	}

	// EndCurrentPhase, EndTheMatch, EndSubphaseLoop
	public class SimpleCommand : Command
	{
		public SimpleMethod method;

		public SimpleCommand (CommandType type, SimpleMethod method)
		{
			this.type = type;
			this.method = method;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke();
		}
	}

	// UseAction, SendMessage, StartSubphaseLoop
	public class StringCommand : Command
	{
		public StringMethod method;
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
	}

	//Shuffle
	public class ZoneCommand : Command
	{
		public ZoneMethod method;
		public ZoneSelector zoneSelector;

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
	}

	//UseCard, ClickCard
	public class CardCommand : Command
	{
		public CardMethod method;
		public CardSelector cardSelector;

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
	}

	//MoveCardToZone
	public class CardZoneCommand : Command
	{
		public CardZoneMethod method;
		public CardSelector cardSelector;
		public ZoneSelector zoneSelector;
		public string additionalParams;

		public CardZoneCommand (CommandType type, CardZoneMethod method, CardSelector cardSelector, ZoneSelector zoneSelector, string additionalParams = "")
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
	}

	//SetCardFieldValue
	public class CardFieldCommand : Command
	{
		public CardFieldMethod method;
		public CardSelector cardSelector;
		public string fieldName;
		public NumberGetter number;

		public CardFieldCommand (CommandType type, CardFieldMethod method, CardSelector cardSelector, string fieldName, NumberGetter number)
		{
			this.type = type;
			this.method = method;
			this.cardSelector = cardSelector;
			this.fieldName = fieldName;
			this.number = number;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(cardSelector, fieldName, number);
		}
	}

	//SetVariable
	public class StringNumberCommand : Command
	{
		public StringNumberMethod method;
		string text;
		NumberGetter number;

		public StringNumberCommand (CommandType type, StringNumberMethod method, string text, NumberGetter number)
		{
			this.type = type;
			this.method = method;
			this.text = text;
			this.number = number;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(text, number);
		}
	}
}