namespace LLMGame;

public partial class LLMCharacter
{
	[ConCmd( "llm_char_comehere" )]
	public static void ComeHereConCmd()
	{
		if ( Game.ActiveScene.Components.TryGet<PlayerComponent>( out var ply, FindMode.EverythingInSelfAndChildren ) )
		{
			foreach ( var character in Game.ActiveScene.Components.GetAll<LLMCharacter>() )
			{
				character.WalkToObject( ply.GameObject );
			}
		}
	}

	[ConCmd( "llm_char_lookhere" )]
	public static void LookHereConCmd()
	{
		if ( Game.ActiveScene.Components.TryGet<PlayerComponent>( out var ply, FindMode.EverythingInSelfAndChildren ) )
		{
			foreach ( var character in Game.ActiveScene.Components.GetAll<LLMCharacter>() )
			{
				character.LookAtObject( ply.GameObject );
			}
		}
	}
	[ConCmd( "llm_char_sit" )]
	public static void SitConCmd()
	{
		foreach ( var character in Game.ActiveScene.Components.GetAll<LLMCharacter>() )
		{
			character.SitInClosestSeat();
		}
	}
}
