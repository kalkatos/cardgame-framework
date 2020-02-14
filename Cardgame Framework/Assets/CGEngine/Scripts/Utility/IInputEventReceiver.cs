
namespace CardGameFramework
{
	public interface IInputEventReceiver
	{
		void TreatEvent(InputType type, InputObject inputObject);
	}
}