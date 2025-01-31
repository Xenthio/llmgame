using Sandbox.Citizen;

namespace LLMGame;

public partial class LLMCharacter
{
	public Angles EyeAngles;
	public Vector3 WishVelocity => Navigator.WishVelocity;
	public Vector3 Velocity => Navigator.Velocity;

	public void UpdatePos()
	{
		GameObject.WorldPosition = Navigator.AgentPosition;
	}

	[RequireComponent] public NavMeshAgent Navigator { get; set; }

	public Vector3 WalkToObject( GameObject go )
	{
		// don't walk inside of them, walk up to them
		var size = 64f;
		if ( go.Components.TryGet<ModelRenderer>( out var mdl ) )
		{
			size = mdl.Bounds.Size.WithZ( 0 ).Length;
		}
		var offset = (go.WorldPosition.WithZ( 0 ) - WorldPosition.WithZ( 0 )).Normal * size;
		var pos = go.WorldPosition - offset;
		Navigator.MoveTo( pos );
		LookAtObject( go );
		return pos;
	}

	public void WalkToPosition( Vector3 position )
	{
		Navigator.MoveTo( position );
		TargetEyeAngles = Rotation.LookAt( (position.WithZ( 0 ) - WorldPosition.WithZ( 0 )).Normal, Vector3.Up ).Angles();
	}

	TimeSince TimeSinceLookingObjectSet;
	GameObject LookingObject;
	public void LookAtObject( GameObject go )
	{
		var ob = go;
		if ( go.Components.TryGet<PlayerComponent>( out var ply ) ) ob = ply.Eye;
		if ( go.Components.TryGet<LLMCharacter>( out var chr ) ) ob = chr.Eye;
		TimeSinceLookingObjectSet = 0;
		LookingObject = ob;
	}
	public void LookAtPosition( Vector3 position )
	{
		LookingObject = null;
		_lookAtPosition( position );
	}
	void _lookAtPosition( Vector3 position )
	{
		TargetEyeAngles = Rotation.LookAt( (position - Eye.WorldPosition).Normal, Vector3.Up ).Angles();
	}

	public Angles TargetEyeAngles;
	public void LookAt( Angles angles )
	{
		LookingObject = null;
		TargetEyeAngles = angles;
	}

	[Property, Group( "Body" )] public GameObject Body { get; set; }
	[Property, Group( "Body" )] public GameObject Eye { get; set; }
	[Property, Group( "Body" )] public SkinnedModelRenderer BodyModelRenderer { get; set; }
	[RequireComponent] public CitizenAnimationHelper AnimationHelper { get; set; }

	protected void SetupBody()
	{
		if ( !Body.IsValid() )
		{
			Body = Scene.CreateObject();
			Body.SetParent( GameObject );
			Body.LocalPosition = Vector3.Zero;
			Body.Name = "Body";
		}
		if ( !BodyModelRenderer.IsValid() )
		{
			BodyModelRenderer = Body.AddComponent<SkinnedModelRenderer>();
			BodyModelRenderer.Model = Model.Load( "models/citizen/citizen.vmdl" );
			AnimationHelper.Target = BodyModelRenderer;
		}
	}
	[Property, Group( "Animator" )] public float RotationAngleLimit { get; set; } = 45.0f;
	[Property, Group( "Animator" )] public float RotationSpeed { get; set; } = 1.0f;
	[Property, Group( "Animator" )] public bool RotationFaceLadders { get; set; } = true;
	float _animRotationSpeed;
	TimeSince timeSinceRotationSpeedUpdate;

	public virtual void RotateBody()
	{
		var targetAngle = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation();

		var velocity = WishVelocity.WithZ( 0 );

		float rotateDifference = BodyModelRenderer.WorldRotation.Distance( targetAngle );

		// We're over the limit - snap it 
		if ( rotateDifference > RotationAngleLimit )
		{
			var delta = 0.999f - (RotationAngleLimit / rotateDifference);
			var newRotation = Rotation.Lerp( BodyModelRenderer.WorldRotation, targetAngle, delta );

			var a = newRotation.Angles();
			var b = BodyModelRenderer.WorldRotation.Angles();

			var yaw = MathX.DeltaDegrees( a.yaw, b.yaw );

			_animRotationSpeed += yaw;
			_animRotationSpeed = _animRotationSpeed.Clamp( -90, 90 );

			BodyModelRenderer.WorldRotation = newRotation;
		}

		if ( velocity.Length > 10 )
		{
			var newRotation = Rotation.Slerp( BodyModelRenderer.WorldRotation, targetAngle, Time.Delta * 2.0f * RotationSpeed * velocity.Length.Remap( 0, 100 ) );

			var a = newRotation.Angles();
			var b = BodyModelRenderer.WorldRotation.Angles();

			var yaw = MathX.DeltaDegrees( a.yaw, b.yaw );

			_animRotationSpeed += yaw;
			_animRotationSpeed = _animRotationSpeed.Clamp( -90, 90 );

			BodyModelRenderer.WorldRotation = newRotation;
		}
	}
	public virtual void Animate()
	{
		if ( LookingObject != null )
		{
			_lookAtPosition( LookingObject.WorldPosition );
			if ( TimeSinceLookingObjectSet > 15 ) LookingObject = null;
		}
		EyeAngles = EyeAngles.LerpTo( TargetEyeAngles, 4f * Time.Delta );

		AnimationHelper.WithWishVelocity( WishVelocity );
		AnimationHelper.WithVelocity( Velocity );

		// skid, this isn't in AnimationHelper yet it seems? ok nvm it's absolutely FUCKED
		/*{
			var dir = Controller.Velocity.SubtractDirection( Controller.WishVelocity.Normal );
			if ( dir.IsNearlyZero( 1.0f ) ) dir = 0;

			var forward = BodyModelRenderer.WorldRotation.Forward.Dot( dir );
			var sideward = BodyModelRenderer.WorldRotation.Right.Dot( dir );

			BodyModelRenderer.Set( "skid_x", forward );
			BodyModelRenderer.Set( "skid_y", sideward );

			var skidAmount = (Controller.Velocity.Length - Controller.WishVelocity.Length).Clamp( 0, 10 ).Remap( 0, 10, 0, 0.5f );
			BodyModelRenderer.Set( "skid", skidAmount );
		}*/

		AnimationHelper.WithLook( EyeAngles.Forward * 100, 1, 1, 1.0f );
		//AnimationHelper.DuckLevel = IsCrouching ? 100 : 0;
		//AnimationHelper.IsGrounded = Controller.IsOnGround || IsTouchingLadder;
		//AnimationHelper.IsClimbing = IsTouchingLadder;
		//AnimationHelper.IsSwimming = IsSwimming;
		//AnimationHelper.IsNoclipping = IsNoclipping;

		if ( timeSinceRotationSpeedUpdate > 0.1f )
		{
			timeSinceRotationSpeedUpdate = 0;
			AnimationHelper.MoveRotationSpeed = _animRotationSpeed * 5;
			_animRotationSpeed = 0;
		}
		RotateBody();
	}
}
