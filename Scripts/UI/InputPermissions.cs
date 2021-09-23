using System;

namespace CardgameCore
{
	[Flags]
	public enum InputPermissions
	{
		None = 0,
		Click = 1,
		Drag = 2,
		Hover = 4,
		DropInto = 8,
	}
}