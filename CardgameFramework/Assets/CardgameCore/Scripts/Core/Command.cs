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
		internal Func<string, string, IEnumerator> method;
		internal string strParameter;
		internal string additionalInfo;

		public StringCommand (CommandType type, Func<string, string, IEnumerator> method, string strParameter, string additionalInfo = "") : base(type)
		{
			this.type = type;
			this.method = method;
			this.strParameter = strParameter;
			this.additionalInfo = additionalInfo;
		}

		public override IEnumerator Execute ()
		{
			yield return method(strParameter, additionalInfo);
		}
	}

	public class ZoneCommand : Command
	{
		internal Func<ZoneSelector, string, IEnumerator> method;
		internal ZoneSelector zoneSelector;
		internal string additionalInfo;

		public ZoneCommand (CommandType type, Func<ZoneSelector, string, IEnumerator> method, ZoneSelector zoneSelector, string additionalInfo) : base(type)
		{
			this.type = type;
			this.zoneSelector = zoneSelector;
			this.method = method;
			this.additionalInfo = additionalInfo;
		}

		public override IEnumerator Execute ()
		{
			yield return method(zoneSelector, additionalInfo);
		}
	}

	public class ComponentCommand : Command
	{
		internal Func<ComponentSelector, string, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal string additionalInfo;

		public ComponentCommand (CommandType type, Func<ComponentSelector, string, IEnumerator> method, ComponentSelector componentSelector, string additionalInfo) : base(type)
		{
			this.type = type;
			this.method = method;
			this.componentSelector = componentSelector;
			this.additionalInfo = additionalInfo;
		}

		public override IEnumerator Execute ()
		{
			yield return method(componentSelector, additionalInfo);
		}
	}

	public class SingleComponentCommand : Command
	{
		internal CGComponent component;
		internal Func<CGComponent, string, IEnumerator> method;
		internal string additionalInfo;

		public SingleComponentCommand (Func<CGComponent, string, IEnumerator> method, CGComponent component, string additionalInfo) : base(CommandType.UseComponent)
		{
			this.component = component;
			this.method = method;
			this.additionalInfo = additionalInfo;
		}

		public void SetCard (CGComponent c)
		{
			component = c;
		}

		public override IEnumerator Execute ()
		{
			yield return method(component, additionalInfo);
		}
	}

	public class SingleZoneCommand : Command
	{
		internal Zone zone;
		internal Func<Zone, string, IEnumerator> method;
		internal string additionalInfo;

		public SingleZoneCommand(Func<Zone, string, IEnumerator> method, Zone zone, string additionalInfo) : base(CommandType.UseZone)
		{
			this.method = method;
			this.zone = zone;
			this.additionalInfo = additionalInfo;
		}

		public void SetZone(Zone z)
		{
			zone = z;
		}

		public override IEnumerator Execute()
		{
			yield return method(zone, additionalInfo);
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
		internal Func<ComponentSelector, ZoneSelector, string, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal ZoneSelector zoneSelector;
		internal string additionalInfo;

		public ComponentZoneCommand (CommandType type, Func<ComponentSelector, ZoneSelector, string, IEnumerator> method, ComponentSelector componentSelector, 
			ZoneSelector zoneSelector, string additionalInfo) : base(type)
		{
			this.type = type;
			this.method = method;
			this.componentSelector = componentSelector;
			this.zoneSelector = zoneSelector;
			this.additionalInfo = additionalInfo;
		}

		public override IEnumerator Execute ()
		{
			yield return method(componentSelector, zoneSelector, additionalInfo);
		}
	}

	public class ComponentFieldCommand : Command
	{
		internal Func<ComponentSelector, string, Getter, string, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal string fieldName;
		internal Getter valueGetter;
		internal string additionalInfo;

		public ComponentFieldCommand (CommandType type, Func<ComponentSelector, string, Getter, string, IEnumerator> method, ComponentSelector componentSelector, 
			string fieldName, Getter valueGetter, string additionalInfo) : base(type)
		{
			this.method = method;
			this.componentSelector = componentSelector;
			this.fieldName = fieldName;
			this.valueGetter = valueGetter;
			this.additionalInfo = additionalInfo;
		}

		public override IEnumerator Execute ()
		{
			yield return method(componentSelector, fieldName, valueGetter, additionalInfo);
		}
	}

	public class VariableCommand : Command
	{
		internal Func<string, Getter, string, IEnumerator> method;
		internal string variableName;
		internal Getter value;
		internal string additionalInfo;

		public VariableCommand (CommandType type, Func<string, Getter, string, IEnumerator> method, string variableName, Getter value, string additionalInfo) : base(type)
		{
			this.method = method;
			this.variableName = variableName;
			this.value = value;
			this.additionalInfo = additionalInfo;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(variableName, value, additionalInfo);
		}
	}

	public class ChangeComponentTagCommand : Command
	{
		internal Func<ComponentSelector, string, string, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal string tag;
		internal string additionalInfo;

		public ChangeComponentTagCommand (CommandType type, Func<ComponentSelector, string, string, IEnumerator> method, ComponentSelector componentSelector, string tag, string additionalInfo) : base(type)
		{
			this.method = method;
			this.componentSelector = componentSelector;
			this.tag = tag;
			this.additionalInfo = additionalInfo;
		}

		public override IEnumerator Execute ()
		{
			yield return method(componentSelector, tag, additionalInfo);
		}
	}
}