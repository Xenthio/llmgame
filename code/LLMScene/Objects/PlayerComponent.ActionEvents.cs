namespace LLMGame;

public partial class PlayerComponent
{
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
			LLMScene.Instance.BroadcastAudibleMessage( this, $"<pickup><target>{objname}</target>{liftedfrom}</pickup>", false );
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
			LLMScene.Instance.BroadcastAudibleMessage( this, $"<drop><target>{objname}</target>{placeon}</drop>", false );
		}
	}
	public void BroadcastInteractAction( GameObject obj )
	{
		var objname = "Unknown Object";
		if ( obj.Components.TryGet<LLMObject>( out var llmobject ) )
		{
			objname = llmobject.Name;
			LLMScene.Instance.BroadcastAudibleMessage( this, $"<interact><target>{objname}</target></interact>", false );
		}
	}
}
