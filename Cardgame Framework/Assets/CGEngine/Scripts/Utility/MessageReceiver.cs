
namespace CGEngine
{
	public interface IMessageReceiver
	{
		void TreatMessage(MessageType type, Message msg);
	}
}