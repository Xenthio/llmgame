namespace LLMGame;

public sealed class BoneMerger : Component, Component.ExecuteInEditor
{
	[Property] public SkinnedModelRenderer Source { get; set; }
	[Property] public SkinnedModelRenderer Destination { get; set; }
	[Property] public bool RotationCorrection { get; set; } = true;
	[Property] public bool PositionCorrection { get; set; } = true;
	[Property] public bool MaximoCorrection { get; set; } = false;
	[Property] public bool Source2Correction { get; set; } = false;
	[Property] public bool DebugRendering { get; set; } = false;
	[Property] public float ArmSpace { get; set; } = 0.0f;
	[Property] public float ShoulderWidth { get; set; } = 0.0f;
	[Property] public float ShoulderRaise { get; set; } = 0.0f;
	[Property] public float PelvisJut { get; set; } = 0.0f;
	private static string[][] SynonymousBones = new string[][]
	{
		new string[] {"pelvis", "bip01_pelvis", "Hips"},
		new string[] {"spine_0", "bip01_spine", "Spine"},
		new string[] {"spine_1", "bip01_spine1", "Spine1"},
		new string[] {"spine_2", "bip01_spine2", "bip01_spine4", "Spine2"},
		new string[] {"spine_3", "bip01_spine4", "Spine3"},
		new string[] {"neck_0", "bip01_neck1", "Neck"},
		new string[] {"head", "head_0", "bip01_head1", "Head"},
		new string[] {"clavicle_R", "bip01_r_clavicle", "RightShoulder"},
		new string[] {"arm_upper_R", "bip01_r_upperarm", "RightArm"},
		new string[] {"arm_lower_R", "bip01_r_forearm", "RightForeArm"},
		new string[] {"clavicle_L", "bip01_l_clavicle", "LeftShoulder"},
		new string[] {"arm_upper_L", "bip01_l_upperarm", "LeftArm"},
		new string[] {"arm_lower_L", "bip01_l_forearm", "LeftForeArm"},
		new string[] {"leg_upper_L", "bip01_l_thigh", "LeftUpLeg"},
		new string[] {"leg_lower_L", "bip01_l_calf", "LeftLeg"},
		new string[] {"ankle_L", "bip01_l_foot", "LeftFoot"},
		new string[] {"leg_upper_R", "bip01_r_thigh", "RightUpLeg"},
		new string[] {"leg_lower_R", "bip01_r_calf", "RightLeg"},
		new string[] {"ankle_R", "bip01_r_foot", "RightFoot"},
	};
	protected override void OnUpdate()
	{
		foreach ( var bone in Destination.Model.Bones.AllBones )
		{
			foreach ( var identicalbones in SynonymousBones )
			{
				if ( identicalbones.Contains( bone.Name ) )
				{
					foreach ( var bone2 in identicalbones )
					{
						if ( Source.TryGetBoneTransform( bone2, out var tx ) )
						{
							tx = GameObject.Transform.World.ToLocal( tx );

							if ( RotationCorrection )
							{
								if ( bone.Name == "bip01_pelvis" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Up, -90 );
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Right, 90 );
								}
								if ( bone.Name == "bip01_neck1" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
								if ( bone.Name == "bip01_head1" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
								if ( bone.Name == "bip01_l_clavicle" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, -90 );
								}
								if ( bone.Name == "bip01_r_clavicle" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 90 );
								}
								if ( bone.Name == "bip01_l_upperarm" || bone.Name == "bip01_r_upperarm" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
								if ( bone.Name == "bip01_l_forearm" || bone.Name == "bip01_r_forearm" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
								if ( bone.Name == "bip01_l_thigh" || bone.Name == "bip01_r_thigh" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
								if ( bone.Name == "bip01_l_calf" || bone.Name == "bip01_r_calf" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
								if ( bone.Name == "bip01_l_foot" || bone.Name == "bip01_r_foot" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
							}

							if ( MaximoCorrection )
							{
								tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Right, 90 );
								tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Down, 90 );
								if ( bone2 == "Hips" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Right, 180 );
								}
								if ( bone2 == "Spine" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Right, 180 );
								}
								if ( bone2 == "Spine1" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Right, 180 );
								}
								if ( bone2 == "Spine2" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Right, 180 );
								}
								if ( bone2 == "RightForeArm" || bone2 == "RightArm" || bone2 == "RightShoulder" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 90 );
								}
								if ( bone2 == "LeftForeArm" || bone2 == "LeftArm" || bone2 == "LeftShoulder" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, -90 );
								}
							}

							if ( Source2Correction )
							{
								if ( bone2 == "clavicle_R" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Right, 180 );
								}
								if ( bone2 == "arm_upper_R" || bone2 == "arm_lower_R" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Right, 180 );
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
								if ( bone2 == "leg_upper_R" || bone2 == "leg_lower_R" || bone2 == "ankle_R" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Right, 180 );
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
								if ( bone2 == "leg_upper_R" || bone2 == "leg_lower_R" || bone2 == "leg_upper_L" || bone2 == "leg_lower_L" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
								}
							}

							if ( PositionCorrection )
							{

								if ( bone.Name == "bip01_spine4" )
								{
									if ( bone2 == "spine_2" )
									{
										tx.Position += tx.Rotation.Forward * 4;
										tx.Position += tx.Rotation.Right * 1;
									}
									else
									{
										tx.Position += tx.Rotation.Forward * 1;
										tx.Position += tx.Rotation.Right * 1;
									}
								}
								if ( bone.Name == "bip01_spine2" )
								{
									tx.Position += tx.Rotation.Right * 1;
									tx.Position += tx.Rotation.Forward * -1;
								}
								if ( bone.Name == "bip01_spine1" )
								{
									tx.Position += tx.Rotation.Right * 1;
								}
								if ( bone.Name == "bip01_spine" )
								{
									tx.Position += tx.Rotation.Forward * 1;
									tx.Position += tx.Rotation.Right * 0;
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Up, -8 );
								}
								if ( bone.Name == "bip01_pelvis" )
								{
									tx.Position += tx.Rotation.Right * 2;
									tx.Position += tx.Rotation.Up * 3;
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, -5 );
								}
								if ( bone.Name == "bip01_head1" )
								{
									tx.Position += tx.Rotation.Forward * -3;
									tx.Position += tx.Rotation.Right * 0;
								}
								if ( bone2 == "clavicle_R" )
								{
									tx.Position += tx.Rotation.Up * 1.5f;
									tx.Position += tx.Rotation.Forward * 1.5f;
									//tx.Position += tx.Rotation.Right * -0.4f;
								}
								if ( bone2 == "clavicle_L" )
								{
									tx.Position += tx.Rotation.Down * 1.5f;
									tx.Position += tx.Rotation.Forward * 1.5f;
									//tx.Position += tx.Rotation.Right * -0.4f;
								}
							}

							if ( bone2.Contains( "arm" ) )
							{
								if ( bone2.Contains( "_L" ) )
									tx.Position += tx.Rotation.Up * ArmSpace;
								if ( bone2.Contains( "_R" ) )
									tx.Position += tx.Rotation.Down * ArmSpace;
							}

							if ( bone2.Contains( "clavicle" ) || bone2.Contains( "Shoulder" ) )
							{
								if ( bone2.Contains( "_L" ) )
								{
									tx.Position += tx.Rotation.Forward * ShoulderWidth;
									tx.Position += tx.Rotation.Up * ShoulderRaise;
								}
								if ( bone2.Contains( "_R" ) )
								{
									tx.Position += tx.Rotation.Forward * ShoulderWidth;
									tx.Position += tx.Rotation.Down * ShoulderRaise;
								}
							}

							if ( bone2.Contains( "pelvis" ) )
							{
								tx.Position += tx.Rotation.Up * PelvisJut;
							}

							//Log.Info( $"Copying {bone2} to {bone.Name}" );
							Destination.SetBoneTransform( bone, tx );
							continue;
						}
					}
				}
			}

			if ( DebugRendering && Destination.TryGetBoneTransform( bone.Name, out var dtx ) )
			{
				DebugOverlay.Normal( dtx.Position, dtx.Forward, Color.Red, overlay: true );
				DebugOverlay.Normal( dtx.Position, dtx.Up, Color.Blue, overlay: true );
				DebugOverlay.Normal( dtx.Position, dtx.Left, Color.Green, overlay: true );
				DebugOverlay.Sphere( new Sphere( dtx.Position, 0.1f ), overlay: true );
				DebugOverlay.Text( dtx.Position, bone.Name, 2, overlay: true );
			}
		}
	}
}
