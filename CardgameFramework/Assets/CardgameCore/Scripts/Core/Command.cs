using System;
using System.Collections;
using UnityEngine;

namespace CardgameCore
{
	/*
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
		UseZone,
		AddTagToCard,
		RemoveTagFromCard


		EndCurrentPhase,
		EndTheMatch,
		EndSubphaseLoop,
		UseAction,
		SendMessage,
		StartSubphaseLoop,
		UseCard,
		Shuffle,
		UseZone,
		SetCardFieldValue,
		SetVariable,
		MoveCardToZone,
		AddTagToCard,
		RemoveTagFromCard

		AddTagToCard = 13,
		EndCurrentPhase = 1,
		EndSubphaseLoop = 3,
		EndTheMatch = 2,
		MoveCardToZone = 12,
		RemoveTagFromCard = 14
		SendMessage = 5,
		SetCardFieldValue = 10,
		SetVariable = 11,
		Shuffle = 8,
		StartSubphaseLoop = 6,
		UseAction = 4,
		UseCard = 7,
		UseZone = 9,
	*/
	public enum CommandType
	{
		Undefined = 0,
		EndCurrentPhase = 1,
		EndTheMatch = 2,
		EndSubphaseLoop = 3,
		UseAction = 4,
		SendMessage = 5,
		StartSubphaseLoop = 6,
		UseComponent = 7,
		Shuffle = 8,
		UseZone = 9,
		SetComponentFieldValue = 10,
		SetVariable = 11,
		MoveComponentToZone = 12,
		AddTagToComponent = 13,
		RemoveTagFromComponent = 14
	}

	public abstract class Command
	{
		internal CommandType type;

		public Command (CommandType type)
		{
			this.type = type;
		}

		public abstract IEnumerator Execute ();
	}

	public class CustomCommand : Command
	{
		internal Func<IEnumerator> method;

		public CustomCommand (Func<IEnumerator> method) : base(CommandType.Undefined)
		{
			this.method = method;
		}

		public override IEnumerator Execute()
		{
			yield return method;
		}
	}

	public class SimpleCommand : Command
	{
		internal Func<IEnumerator> method;

		public SimpleCommand (CommandType type, Func<IEnumerator> method) : base(type)
		{
			this.type = type;
			this.method = method;
		}

		public override IEnumerator Execute ()
		{
			yield return method();
		}
	}

	public class StringCommand : Command
	{
		internal Func<string, IEnumerator> method;
		internal string strParameter;

		public StringCommand (CommandType type, Func<string, IEnumerator> method, string strParameter) : base(type)
		{
			this.type = type;
			this.method = method;
			this.strParameter = strParameter;
		}

		public override IEnumerator Execute ()
		{
			yield return method(strParameter);
		}

	}

	public class ZoneCommand : Command
	{
		internal Func<ZoneSelector, IEnumerator> method;
		internal ZoneSelector zoneSelector;

		public ZoneCommand (CommandType type, Func<ZoneSelector, IEnumerator> method, ZoneSelector zoneSelector) : base(type)
		{
			this.type = type;
			this.zoneSelector = zoneSelector;
			this.method = method;
		}

		public override IEnumerator Execute ()
		{
			yield return method(zoneSelector);
		}
	}

	public class ComponentCommand : Command
	{
		internal Func<ComponentSelector, IEnumerator> method;
		internal ComponentSelector componentSelector;

		public ComponentCommand (CommandType type, Func<ComponentSelector, IEnumerator> method, ComponentSelector componentSelector) : base(type)
		{
			this.type = type;
			this.method = method;
			this.componentSelector = componentSelector;
		}

		public override IEnumerator Execute ()
		{
			yield return method(componentSelector);
		}
	}

	public class SingleComponentCommand : Command
	{
		internal CGComponent component;
		internal Func<CGComponent, IEnumerator> method;

		public SingleComponentCommand (Func<CGComponent, IEnumerator> method, CGComponent component) : base(CommandType.UseComponent)
		{
			this.component = component;
			this.method = method;
		}

		public void SetCard (CGComponent c)
		{
			component = c;
		}

		public override IEnumerator Execute ()
		{
			yield return method(component);
		}
	}

	public class SingleZoneCommand : Command
	{
		internal Zone zone;
		internal Func<Zone, IEnumerator> method;

		public SingleZoneCommand(Func<Zone, IEnumerator> method, Zone zone) : base(CommandType.UseZone)
		{
			this.method = method;
			this.zone = zone;
		}

		public void SetZone(Zone z)
		{
			zone = z;
		}

		public override IEnumerator Execute()
		{
			yield return method(zone);
		}
	}

	//public class WaitCommand : Command
	//{
	//	float seconds;
	//
	//	public WaitCommand (float seconds) : base(type)
	//	{
	//		this.seconds = seconds;
	//	}
	//
	//	public override IEnumerator Execute ()
	//	{
	//		yield return new WaitForSeconds(seconds);
	//	}
	//}

	public class ComponentZoneCommand : Command
	{
		internal Func<ComponentSelector, ZoneSelector, string[], IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal ZoneSelector zoneSelector;
		internal string[] additionalParams;

		public ComponentZoneCommand (CommandType type, Func<ComponentSelector, ZoneSelector, string[], IEnumerator> method, ComponentSelector componentSelector, 
			ZoneSelector zoneSelector, string[] additionalParams = null) : base(type)
		{
			this.type = type;
			this.method = method;
			this.componentSelector = componentSelector;
			this.zoneSelector = zoneSelector;
			this.additionalParams = additionalParams;
		}

		public override IEnumerator Execute ()
		{
			yield return method(componentSelector, zoneSelector, additionalParams);
		}
	}

	public class ComponentFieldCommand : Command
	{
		internal Func<ComponentSelector, string, Getter, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal string fieldName;
		internal Getter valueGetter;

		public ComponentFieldCommand (CommandType type, Func<ComponentSelector, string, Getter, IEnumerator> method, ComponentSelector componentSelector, 
			string fieldName, Getter valueGetter) : base(type)
		{
			this.method = method;
			this.componentSelector = componentSelector;
			this.fieldName = fieldName;
			this.valueGetter = valueGetter;
		}

		public override IEnumerator Execute ()
		{
			yield return method(componentSelector, fieldName, valueGetter);
		}
	}

	public class VariableCommand : Command
	{
		internal Func<string, Getter, IEnumerator> method;
		internal string variableName;
		internal Getter value;

		public VariableCommand (CommandType type, Func<string, Getter, IEnumerator> method, string variableName, Getter value) : base(type)
		{
			this.method = method;
			this.variableName = variableName;
			this.value = value;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(variableName, value);
		}
	}

	public class ChangeComponentTagCommand : Command
	{
		internal Func<ComponentSelector, string, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal string tag;

		public ChangeComponentTagCommand (CommandType type, Func<ComponentSelector, string, IEnumerator> method, ComponentSelector componentSelector, string tag) : base(type)
		{
			this.method = method;
			this.componentSelector = componentSelector;
			this.tag = tag;
		}

		public override IEnumerator Execute ()
		{
			yield return method(componentSelector, tag);
		}
	}
}