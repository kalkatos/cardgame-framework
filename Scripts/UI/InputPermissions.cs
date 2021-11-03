using System;

namespace CardgameCore
{
	[Flags, Obsolete("Not being used since InputHandler became deprecated.")]
	public enum InputPermissions
	{
		None = 0,
		Click = 1,
		Drag = 2,
		Hover = 4,
		DropInto = 8,
	}
}