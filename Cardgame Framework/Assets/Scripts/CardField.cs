using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InfoType
{
	Boolean,
	String,
	Number,
	Image
}

public struct CardField
{
	public string fieldName;
	public InfoType type;
	public bool booleanInfo;
	public string stringInfo;
	public double numberInfo;
	public Sprite imageInfo;
}
