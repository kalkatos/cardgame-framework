
namespace CGEngine
{
	public interface IMessageReceiver
	{
		void TreatMessage(string type, InputObject inputObject);
	}
}