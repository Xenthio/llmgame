namespace LLMGame;

public partial class PlayerComponent
{
	[ConVar( "llm_being_action_react" )]
	public static bool ActionReact { get; set; } = false;

	[ConVar( "llm_being_action_react_chance" )]
	public static int ActionReactChance { get; set; } = 20;

	public bool ShouldReact()
	{
		if ( ActionReact && Game.Random.Int( 100 ) <= ActionReactChance )
		{
			return true;
		}
		return false;
	}

	public void BroadcastPickupAction( PhysicsBody body )
	{
		var objname = "Unknown Object";
		var liftedfrom = "";
		if ( body.GetGameObject().Components.TryGet<LLMObject>( out var llmobject ) )
		{
			objname = llmobject.Name;
			var downtr = Scene.Trace.Ray( body.Position, body.Position + (Vector3.Down * 32) ).IgnoreGameObjectHierarchy( body.GetGameObject() ).Run();
			if ( downtr.GameObject.IsValid() && downtr.GameObject.Components.TryGet<LLMObject>( out var llmobjectdown ) )
			{
				liftedfrom = $"<liftedfrom>{llmobjectdown.Name}</liftedfrom>";
			}
			LLMScene.Instance.BroadcastAudibleMessage( this, $"<pickup><target>{objname}</target>{liftedfrom}</pickup>", ShouldReact() );
		}
	}
	public void BroadcastDropAction( PhysicsBody body )
	{
		var objname = "Unknown Object";
		var placeon = "";
		if ( body.GetGameObject().Components.TryGet<LLMObject>( out var llmobject ) )
		{
			objname = llmobject.Name;
			var downtr = Scene.Trace.Ray( body.Position, body.Position + (Vector3.Down * 32) ).IgnoreGameObjectHierarchy( body.GetGameObject() ).Run();
			if ( downtr.GameObject.IsValid() && downtr.GameObject.Components.TryGet<LLMObject>( out var llmobjectdown ) )
			{
				placeon = $"<placeon>{llmobjectdown.Name}</placeon>";
			}
			LLMScene.Instance.BroadcastAudibleMessage( this, $"<drop><target>{objname}</target>{placeon}</drop>", ShouldReact() );
		}
	}
	public void BroadcastInteractAction( GameObject obj )
	{
		var objname = "Unknown Object";
		if ( obj.Components.TryGet<LLMObject>( out var llmobject ) )
		{
			objname = llmobject.Name;
			LLMScene.Instance.BroadcastAudibleMessage( this, $"<interact><target>{objname}</target></interact>", ShouldReact() );
		}
	}
}
