using System.Collections.Generic;
using XMovement;

namespace LLMGame;

public class PlayerComponent : Component, ILLMBeing
{
	[Property] public PlayerWalkControllerComplex Controller { get; set; }
	[Property] public GameObject Eye { get; set; }
	public List<Message> Memory { get; set; } = new();
	public string GetName()
	{
		return "Player";
	}
	public bool IsUser()
	{
		return true;
	}
	public void Think()
	{
		// Players don't think ;)
	}
	GameObject LastLookedAt;
	TimeSince TimeSinceLookedAtChanged;
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		var tr = Scene.Trace.Ray( Controller.Head.WorldPosition, Controller.Head.WorldPosition + Controller.EyeAngles.Forward * 1000 ).IgnoreGameObjectHierarchy( GameObject.Root ).UseHitboxes().Run();

		if ( LastLookedAt != tr.GameObject )
		{
			LastLookedAt = tr.GameObject;
			TimeSinceLookedAtChanged = 0;
		}

		if ( tr.GameObject.Components.TryGet<LLMCharacter>( out var character ) )
		{
			if ( TimeSinceLookedAtChanged > 1.5 )
				character.IsBeingLookedAt( this );
		}
	}

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
}
