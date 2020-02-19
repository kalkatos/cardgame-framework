
namespace CardGameFramework
{
	[System.Flags]
	public enum InputPermissions
	{
		None = 0,
		Click = 1,
		Drag = 2,
		Hover = 4,
		DropInto = 8
	}
}