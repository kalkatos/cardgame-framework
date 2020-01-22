using System.Collections;

namespace CardGameFramework
{
	public delegate IEnumerator SimpleMethod ();
	public delegate IEnumerator StringMethod (string str);

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

	public class StringCommand : Command
	{
		public string strParameter;
		public StringMethod method;

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
}