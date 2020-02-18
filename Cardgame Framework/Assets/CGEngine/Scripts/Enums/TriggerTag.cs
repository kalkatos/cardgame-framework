
namespace CardGameFramework
{
	[System.Flags]
	public enum TriggerTag
	{
		OnZoneUsed = 1,
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
		OnActionUsed = 8192
	}
} 