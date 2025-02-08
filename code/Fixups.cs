namespace LLMGame;

public sealed class Fixups : Component
{
	protected override void OnUpdate()
	{

	}
	protected override void OnStart()
	{
		base.OnStart();
		if ( GameObject.Components.TryGet<SkyBox2D>( out var sky, FindMode.EverythingInSelfAndChildren ) )
		{
			sky.SkyIndirectLighting = false;
		}
		foreach ( var cubemap in GameObject.Components.GetAll<EnvmapProbe>() )
		{
			cubemap.Feathering = 8f;
		}
	}
}
