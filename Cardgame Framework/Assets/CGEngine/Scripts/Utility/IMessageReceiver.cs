
public interface IMessageReceiver
{
	void TreatMessage(string type, params object[] info);
}
