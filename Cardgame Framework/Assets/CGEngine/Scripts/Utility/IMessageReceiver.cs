
namespace CardGameFramework
{
	public interface IMessageReceiver
	{
		void TreatMessage(string type, InputObject inputObject);
	}
}