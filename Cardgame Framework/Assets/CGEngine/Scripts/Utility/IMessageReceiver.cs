
namespace CardGameFramework
{
	public interface IInputEventReceiver
	{
		void TreatEvent(string type, InputObject inputObject);
	}
}