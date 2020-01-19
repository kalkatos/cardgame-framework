
namespace CardGameFramework
{
	[System.Flags]
	public enum TriggerTag
	{
		OnCardClicked = 1,
		OnCardUsed = 2,
		OnCardEnteredZone = 4,
		OnCardLeftZone = 8,
		OnMatchEnded = 16,
		OnMatchSetup = 32,
		OnMatchStarted = 64,
		OnPhaseEnded = 128,
		OnPhaseStarted = 256,
		OnTurnEnded = 512,
		OnTurnStarted = 1024,
		OnMessageSent = 2048,
		OnVariableChanged = 4096,

		//To be removed
		OnActionUsed = 8192,
		//OnModifierValueChanged = 16384
	}
} 