namespace LLMGame;

public class Lerper : Component
{
	public Vector3 TargetPosition;
	TimeSince TimeSinceStart;
	protected override void OnStart()
	{
		base.OnStart();
		TimeSinceStart = 0;
	}
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		WorldPosition = WorldPosition.LerpTo( TargetPosition, 5f * Time.Delta );
		if ( WorldPosition.Distance( TargetPosition ) < 0.1f || TimeSinceStart > 3 )
		{
			WorldPosition = TargetPosition;
			Destroy();
		}
	}
}
