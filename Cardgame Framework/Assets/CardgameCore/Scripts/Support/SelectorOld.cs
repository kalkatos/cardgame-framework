using System;
using System.Collections.Generic;

namespace CardgameCore
{
	[Serializable]
	public class SelectorOld : Getter
	{
		public Parameter[] parameters;

		public SelectorOld (params Parameter[] parameters)
		{
			this.parameters = parameters;
		}

		public override object Get()
		{
			throw new NotImplementedException();
		}

		public List<CGObject> Select (List<CGObject> list)
		{
			List<CGObject> selected = new List<CGObject>();
			for (int i = 0; i < list.Count; i++)
			{
				int matches = 0;
				for (int j = 0; j < parameters.Length; j++)
					if (parameters[j].IsAFit(list[i]))
						matches++;

				if (matches == parameters.Length)
					selected.Add(list[i]);
			}
			return selected;
		}
    }

	public enum ParameterType
	{
		Undefined,
		ObjectByID,
		ObjectByName,
		ComponentByFieldValue,
		ComponentByZone,
		ComponentByTag
	}

	[Serializable]
	public class Parameter
	{
		public ParameterType type;
		public string string1;
		public string string2;

		public Parameter(ParameterType type, string string1)
		{
			this.type = type;
			this.string1 = string1;
		}

		public Parameter(ParameterType type, string string1, string string2) : this(type, string1)
		{
			this.string2 = string2;
		}

		public bool IsAFit (CGObject subject)
		{
			switch (type)
			{
				case ParameterType.ObjectByID:
					return subject.id == string1;
				case ParameterType.ObjectByName:
					return subject.name == string1;
				case ParameterType.ComponentByFieldValue:
					return ((CGComponent)subject).GetFieldValue(string1) == string2;
				case ParameterType.ComponentByZone:
					return ((CGComponent)subject).zone.name == string1;
				case ParameterType.ComponentByTag:
					return ((CGComponent)subject).HasTag(string1);
			}
			return false;
		}
	}

	//[Serializable]
	//public class ObjectByID : Parameter
	//{
	//	public string idToCompare;

	//	public ObjectByID (string id)
	//	{
	//		type = ParameterType.ObjectByID;
	//		idToCompare = id;
	//	}

	//	public override bool IsAFit (CardgameObject subject)
	//	{
	//		return subject.id == idToCompare;
	//	}
	//}

	//[Serializable]
	//public class ObjectByName : Parameter
	//{
	//	public string name;

	//	public ObjectByName (string name)
	//	{
	//		this.name = name;
	//	}

	//	public override bool IsAFit (CardgameObject subject)
	//	{
	//		return subject.name == name;
	//	}
	//}

	//[Serializable]
	//public class ComponentByFieldValue : Parameter
	//{
	//	public string fieldName;
	//	public string fieldValue;

	//	public ComponentByFieldValue (string fieldName, string fieldValue)
	//	{
	//		this.fieldName = fieldName;
	//		this.fieldValue = fieldValue;
	//	}

	//	public override bool IsAFit (CardgameObject subject)
	//	{
	//		return ((Component)subject).GetFieldValue(fieldName) == fieldValue;
	//	}
	//}

	//[Serializable]
	//public class ComponentByZone : Parameter
	//{
	//	public string zoneName;

	//	public ComponentByZone (string zoneName)
	//	{
	//		this.zoneName = zoneName;
	//	}

	//	public override bool IsAFit (CardgameObject subject)
	//	{
	//		return ((Component)subject).zone.name == zoneName;
	//	}
	//}

	//[Serializable]
	//public class ComponentByTag : Parameter
	//{
	//	public string componentTag;

	//	public ComponentByTag (string componentTag)
	//	{
	//		this.componentTag = componentTag;
	//	}

	//	public override bool IsAFit (CardgameObject subject)
	//	{
	//		return ((Component)subject).HasTag(componentTag);
	//	}
	//}

}