namespace LLMGame;

public partial class LLMCharacter
{

	[ConVar( "llm_being_idle_thinking" )]
	public static bool IdleThinking { get; set; } = false;

	[ConVar( "llm_being_idle_thinking_time" )]
	public static float IdleThinkingTime { get; set; } = 30.0f;

	void DoIdleThinking()
	{
		if ( !HasThoughtOnce ) return;
		if ( TimeSinceLastThought > IdleThinkingTime )
		{
			Memory.Add( Message.System( $"{IdleThinkingTime} seconds has passed..." ) );
			Think();
		}
	}
}
