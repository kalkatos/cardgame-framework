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
	internal enum CommandType
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

	[Serializable]
	internal abstract class Command
	{
		internal string buildingStr;
		internal CommandType type;
		internal Command (CommandType type)
		{
			this.type = type;
		}
		internal abstract IEnumerator Execute ();
	}

	internal class CustomCommand : Command
	{
		internal Func<IEnumerator> method;
		internal CustomCommand (Func<IEnumerator> method) : base(CommandType.Undefined)
		{
			this.method = method;
		}
		internal override IEnumerator Execute()
		{
			yield return method;
		}
	}

	internal class SimpleCommand : Command
	{
		internal Func<IEnumerator> method;
		internal SimpleCommand (CommandType type, Func<IEnumerator> method) : base(type)
		{
			this.type = type;
			this.method = method;
		}
		internal override IEnumerator Execute ()
		{
			yield return method();
		}
	}

	internal class StringCommand : Command
	{
		internal Func<string, string, IEnumerator> method;
		internal string strParameter;
		internal string additionalInfo;
		internal StringCommand (CommandType type, Func<string, string, IEnumerator> method, string strParameter, string additionalInfo = "") : base(type)
		{
			this.type = type;
			this.method = method;
			this.strParameter = strParameter;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(strParameter, additionalInfo);
		}
	}

	internal class ZoneCommand : Command
	{
		internal Func<ZoneSelector, string, IEnumerator> method;
		internal ZoneSelector zoneSelector;
		internal string additionalInfo;
		internal ZoneCommand (CommandType type, Func<ZoneSelector, string, IEnumerator> method, ZoneSelector zoneSelector, string additionalInfo) : base(type)
		{
			this.type = type;
			this.zoneSelector = zoneSelector;
			this.method = method;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(zoneSelector, additionalInfo);
		}
	}

	internal class ComponentCommand : Command
	{
		internal Func<ComponentSelector, string, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal string additionalInfo;
		internal ComponentCommand (CommandType type, Func<ComponentSelector, string, IEnumerator> method, ComponentSelector componentSelector, string additionalInfo) : base(type)
		{
			this.type = type;
			this.method = method;
			this.componentSelector = componentSelector;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(componentSelector, additionalInfo);
		}
	}

	internal class SingleComponentCommand : Command
	{
		internal CGComponent component;
		internal Func<CGComponent, string, IEnumerator> method;
		internal string additionalInfo;
		internal SingleComponentCommand (Func<CGComponent, string, IEnumerator> method, CGComponent component, string additionalInfo) : base(CommandType.UseComponent)
		{
			this.component = component;
			this.method = method;
			this.additionalInfo = additionalInfo;
		}
		internal void SetCard (CGComponent c)
		{
			component = c;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(component, additionalInfo);
		}
	}

	internal class SingleZoneCommand : Command
	{
		internal Zone zone;
		internal Func<Zone, string, IEnumerator> method;
		internal string additionalInfo;
		internal SingleZoneCommand(Func<Zone, string, IEnumerator> method, Zone zone, string additionalInfo) : base(CommandType.UseZone)
		{
			this.method = method;
			this.zone = zone;
			this.additionalInfo = additionalInfo;
		}
		internal void SetZone(Zone z)
		{
			zone = z;
		}
		internal override IEnumerator Execute()
		{
			yield return method(zone, additionalInfo);
		}
	}

	//internal class WaitCommand : Command
	//{
	//	float seconds;
	//
	//	internal WaitCommand (float seconds) : base(type)
	//	{
	//		this.seconds = seconds;
	//	}
	//
	//	internal override IEnumerator Execute ()
	//	{
	//		yield return new WaitForSeconds(seconds);
	//	}
	//}

	internal class ComponentZoneCommand : Command
	{
		internal Func<ComponentSelector, ZoneSelector, MovementAdditionalInfo, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal ZoneSelector zoneSelector;
		internal MovementAdditionalInfo additionalInfo;
		internal ComponentZoneCommand (CommandType type, Func<ComponentSelector, ZoneSelector, MovementAdditionalInfo, IEnumerator> method, ComponentSelector componentSelector, 
			ZoneSelector zoneSelector, MovementAdditionalInfo additionalInfo) : base(type)
		{
			this.type = type;
			this.method = method;
			this.componentSelector = componentSelector;
			this.zoneSelector = zoneSelector;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(componentSelector, zoneSelector, additionalInfo);
		}
	}

	internal class ComponentFieldCommand : Command
	{
		internal Func<ComponentSelector, string, Getter, string, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal string fieldName;
		internal Getter valueGetter;
		internal string additionalInfo;
		internal ComponentFieldCommand (CommandType type, Func<ComponentSelector, string, Getter, string, IEnumerator> method, ComponentSelector componentSelector, 
			string fieldName, Getter valueGetter, string additionalInfo) : base(type)
		{
			this.method = method;
			this.componentSelector = componentSelector;
			this.fieldName = fieldName;
			this.valueGetter = valueGetter;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(componentSelector, fieldName, valueGetter, additionalInfo);
		}
	}

	internal class VariableCommand : Command
	{
		internal Func<string, Getter, string, IEnumerator> method;
		internal string variableName;
		internal Getter value;
		internal string additionalInfo;
		internal VariableCommand (CommandType type, Func<string, Getter, string, IEnumerator> method, string variableName, Getter value, string additionalInfo) : base(type)
		{
			this.method = method;
			this.variableName = variableName;
			this.value = value;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method.Invoke(variableName, value, additionalInfo);
		}
	}

	internal class ChangeComponentTagCommand : Command
	{
		internal Func<ComponentSelector, string, string, IEnumerator> method;
		internal ComponentSelector componentSelector;
		internal string tag;
		internal string additionalInfo;
		internal ChangeComponentTagCommand (CommandType type, Func<ComponentSelector, string, string, IEnumerator> method, ComponentSelector componentSelector, string tag, string additionalInfo) : base(type)
		{
			this.method = method;
			this.componentSelector = componentSelector;
			this.tag = tag;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(componentSelector, tag, additionalInfo);
		}
	}
}