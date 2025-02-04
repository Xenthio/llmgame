namespace LLMGame;

public partial class PlayerComponent
{
	PhysicsBody HoldingPhysicsBody { get; set; }
	[Sync] Transform InitialTransform { get; set; }
	[Sync] Vector3 WantedPosition { get; set; }
	[Sync] Rotation WantedRotation { get; set; }

	public void UpdateUsing()
	{
		if ( Input.Pressed( "use" ) )
		{
			if ( HoldingPhysicsBody.IsValid() )
			{
				BroadcastDropAction( HoldingPhysicsBody );
				HoldingPhysicsBody = null;
			}
			else
			{
				var usetr = Scene.Trace.Ray( Controller.AimRay, 128 ).IgnoreGameObjectHierarchy( GameObject.Root ).Run();
				if ( usetr.Body.IsValid() && usetr.Body.Mass <= 15 )
				{
					HoldingPhysicsBody = usetr.Body;
					//Log.Info( HoldingPhysicsBody.Mass );
					InitialTransform = new Transform( Controller.Head.WorldPosition, Controller.EyeAngles.ToRotation() ).ToLocal( HoldingPhysicsBody.Transform );
					BroadcastPickupAction( HoldingPhysicsBody );
				}
			}
		}
		if ( HoldingPhysicsBody.IsValid() )
		{
			if ( !IsProxy )
			{
				WantedPosition = new Transform( Controller.Head.WorldPosition, Controller.EyeAngles.ToRotation() ).ToWorld( InitialTransform ).Position;
				WantedRotation = new Transform( Controller.Head.WorldPosition, Controller.EyeAngles.ToRotation() ).ToWorld( InitialTransform ).Rotation;
			}
			var vel = HoldingPhysicsBody.Velocity;
			var angvel = HoldingPhysicsBody.AngularVelocity;

			Vector3.SmoothDamp( HoldingPhysicsBody.Position, WantedPosition, ref vel, 0.20f, Time.Delta );
			Rotation.SmoothDamp( HoldingPhysicsBody.Rotation, WantedRotation, ref angvel, 0.20f, Time.Delta );

			HoldingPhysicsBody.Velocity = vel;
			HoldingPhysicsBody.AngularVelocity = angvel;
		}
	}
}
