using Sandbox.Citizen;
using System;
using System.Threading.Tasks;

namespace LLMGame;

public partial class LLMCharacter
{
	public Angles EyeAngles;
	public Vector3 WishVelocity;
	public Vector3 Velocity;

	public void UpdatePos()
	{

		if ( UseRootMotion )
		{
			Navigator.SetAgentPosition( WorldPosition );
			BodyModelRenderer.WorldRotation *= BodyModelRenderer.RootMotion.Rotation;
			WorldPosition += BodyModelRenderer.RootMotion.Position * BodyModelRenderer.WorldRotation;
		}
		else
		{
			GameObject.WorldPosition = Navigator.AgentPosition;
		}
	}

	public bool Sitting = false;
	public LLMSeatMarker CurrentSeat;
	public async Task SitInClosestSeat()
	{
		if ( Scene.Components.GetAll<LLMSeatMarker>( FindMode.EverythingInSelfAndDescendants ).OrderBy( x => x.WorldPosition.Distance( WorldPosition ) ).FirstOrDefault() is LLMSeatMarker seatMarker )
		{
			Log.Info( "sitting" );
			LookAtObject( seatMarker.GameObject );
			await WalkToPositionAsync( seatMarker.WorldPosition );
			CurrentSeat = seatMarker;
			await Task.Delay( 500 );
			Sitting = true;
		}
	}
	public async Task Stand()
	{
		Sitting = false;
		await Task.Delay( 1500 );
		CurrentSeat = null;
	}


	public bool HasFinishedMoving()
	{
		if ( TargetPosition.HasValue )
		{
			return WorldPosition.WithZ( 0 ).AlmostEqual( TargetPosition.Value.WithZ( 0 ), 4 );
		}
		return true;
	}

	public async Task WalkToPositionAsync( Vector3 position )
	{
		await WalkToPosition( position );
		while ( !HasFinishedMoving() )
		{
			await Task.Delay( 100 );
		}
	}
	public async Task WalkToObjectAsync( GameObject go )
	{
		var targetpos = await WalkToObject( go );
		while ( !HasFinishedMoving() )
		{
			await Task.Delay( 100 );
		}
	}


	[RequireComponent] public NavMeshAgent Navigator { get; set; }

	public async Task<Vector3> WalkToObject( GameObject go )
	{
		// don't walk inside of them, walk up to them
		var size = 64f;
		if ( go.Components.TryGet<ModelRenderer>( out var mdl ) )
		{
			size = mdl.Bounds.Size.WithZ( 0 ).Length;
		}
		var offset = (go.WorldPosition.WithZ( 0 ) - WorldPosition.WithZ( 0 )).Normal * size;
		var pos = go.WorldPosition - offset;
		await _walkToPosition( pos );
		LookAtObject( go );
		return pos;
	}

	public async Task WalkToPosition( Vector3 position )
	{
		await _walkToPosition( position );
		TargetEyeAngles = Rotation.LookAt( (position.WithZ( 0 ) - WorldPosition.WithZ( 0 )).Normal, Vector3.Up ).Angles();
	}

	Vector3? TargetPosition;
	async Task _walkToPosition( Vector3 position )
	{
		if ( Sitting ) await Stand();
		TargetPosition = position;
		Navigator.MoveTo( position );
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
	[Property, Group( "Animator" )] public bool UseRootMotion { get; set; } = true;
	float _animRotationSpeed;
	TimeSince timeSinceRotationSpeedUpdate;

	public virtual void RotateBody()
	{
		var targetAngle = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation();

		var velocity = WishVelocity.WithZ( 0 );

		float rotateDifference = BodyModelRenderer.WorldRotation.Distance( targetAngle );




		if ( UseRootMotion )
		{
			if ( rotateDifference > RotationAngleLimit )
			{
				var delta = 0.999f - (RotationAngleLimit / rotateDifference);
				var newRotation = Rotation.Lerp( BodyModelRenderer.WorldRotation, targetAngle, delta );

				var a = newRotation.Angles();
				var b = BodyModelRenderer.WorldRotation.Angles();

				var yaw = MathX.DeltaDegrees( a.yaw, b.yaw );

				_animRotationSpeed += yaw;
				_animRotationSpeed = _animRotationSpeed.Clamp( -90, 90 );

				//BodyModelRenderer.WorldRotation = newRotation;
			}
			if ( CurrentSeat.IsValid() )
			{
				Log.Info( CurrentSeat );
				targetAngle = CurrentSeat.WorldRotation.RotateAroundAxis( Vector3.Up, 0 );
				var newRotation = Rotation.Lerp( BodyModelRenderer.WorldRotation, targetAngle, 2f * Time.Delta );
				BodyModelRenderer.WorldRotation = newRotation;
			}
			else if ( Navigator.Velocity.Length > 10 )
			{
				var newRotation = Rotation.Lerp( BodyModelRenderer.WorldRotation, targetAngle, Time.Delta * 2.0f * RotationSpeed * Navigator.Velocity.Length.Remap( 0, 100 ) );
				BodyModelRenderer.WorldRotation = newRotation;
			}
		}
		else
		{// We're over the limit - snap it 
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

	}
	public virtual void Animate()
	{
		if ( Navigator.Velocity.Length > 10 )
		{
			var b = Navigator.GetLookAhead( 30f );
			var ang = Rotation.LookAt( (b.WithZ( 0 ) - Eye.WorldPosition.WithZ( 0 )).Normal, Vector3.Up );
			TargetEyeAngles = ang;
		}
		else if ( LookingObject != null )
		{
			_lookAtPosition( LookingObject.WorldPosition );
			if ( TimeSinceLookingObjectSet > 15 ) LookingObject = null;
		}

		if ( TargetPosition.HasValue && HasFinishedMoving() )
		{
			TargetPosition = null;
			Navigator.Stop();
		}

		EyeAngles = EyeAngles.LerpTo( TargetEyeAngles, 4f * Time.Delta );


		WishVelocity = Navigator.WishVelocity;
		Velocity = Navigator.Velocity.WithZ( 0 );

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
		var heading = (EyeAngles - BodyModelRenderer.WorldRotation.Angles()).Normal.yaw;
		BodyModelRenderer.SceneModel.SetAnimParameter( "look_heading", heading );
		BodyModelRenderer.SceneModel.SetAnimParameter( "input_speed", 72f );
		//AnimationHelper.DuckLevel = IsCrouching ? 100 : 0;
		//AnimationHelper.IsGrounded = Controller.IsOnGround || IsTouchingLadder;
		//AnimationHelper.IsClimbing = IsTouchingLadder;
		//AnimationHelper.IsSwimming = IsSwimming;
		//AnimationHelper.IsNoclipping = IsNoclipping;
		AnimationHelper.Sitting = Sitting ? CitizenAnimationHelper.SittingStyle.Chair : CitizenAnimationHelper.SittingStyle.None;

		if ( timeSinceRotationSpeedUpdate > 0.1f )
		{
			if ( UseRootMotion && MathF.Abs( _animRotationSpeed ) < 5f )
			{
				_animRotationSpeed = 0;
			}
			timeSinceRotationSpeedUpdate = 0;
			AnimationHelper.MoveRotationSpeed = _animRotationSpeed * 5;
			_animRotationSpeed = 0;
		}
		RotateBody();
	}
}
