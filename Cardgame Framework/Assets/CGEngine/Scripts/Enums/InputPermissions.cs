
namespace CardGameFramework
{
	[System.Flags]
	public enum InputPermissions
	{
		Click = 1,
		Drag = 2,
		Hover = 4,
		DropInto = 8
	}
}