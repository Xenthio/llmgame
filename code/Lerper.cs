namespace LLMGame;

public class Lerper : Component
{
	public Vector3 TargetPosition;
	public bool ShouldEnablePhysics;
	TimeSince TimeSinceStart;
	protected override void OnStart()
	{
		base.OnStart();
		TimeSinceStart = 0;
	}
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		WorldPosition = WorldPosition.LerpTo( TargetPosition, 24f * Time.Delta );
		if ( WorldPosition.Distance( TargetPosition ) < 0.3f || TimeSinceStart > 3f )
		{
			WorldPosition = TargetPosition;
			if ( ShouldEnablePhysics && Components.TryGet<Rigidbody>( out var phys ) )
				phys.MotionEnabled = true;
			Destroy();
		}
	}
}
