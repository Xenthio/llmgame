namespace LLMGame;

public sealed class BoneMerger : Component, Component.ExecuteInEditor
{
	[Property] public SkinnedModelRenderer Source { get; set; }
	[Property] public SkinnedModelRenderer Destination { get; set; }
	[Property] public bool RotationCorrection { get; set; } = true;
	[Property] public bool PositionCorrection { get; set; } = true;
	private string[][] SynonymousBones = new string[][]
	{
		new string[] {"pelvis", "bip01_pelvis"},
		new string[] {"spine_0", "bip01_spine"},
		new string[] {"spine_1", "bip01_spine1"},
		new string[] {"spine_2", "bip01_spine2", "bip01_spine4"},
		new string[] {"neck_0", "bip01_neck1"},
		new string[] {"head", "head_0", "bip01_head1"},
		new string[] {"clavicle_R", "bip01_r_clavicle"},
		new string[] {"arm_upper_R", "bip01_r_upperarm"},
		new string[] {"arm_lower_R", "bip01_r_forearm"},
		new string[] {"clavicle_L", "bip01_l_clavicle"},
		new string[] {"arm_upper_L", "bip01_l_upperarm"},
		new string[] {"arm_lower_L", "bip01_l_forearm"},
		new string[] {"leg_upper_L", "bip01_l_thigh"},
		new string[] {"leg_lower_L", "bip01_l_calf"},
		new string[] {"ankle_L", "bip01_l_foot"},
		new string[] {"leg_upper_R", "bip01_r_thigh"},
		new string[] {"leg_lower_R", "bip01_r_calf"},
		new string[] {"ankle_R", "bip01_r_foot"},
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
								if ( bone.Name == "bip01_l_clavicle" || bone.Name == "bip01_r_clavicle" )
								{
									tx.Rotation = tx.Rotation.RotateAroundAxis( Vector3.Forward, 180 );
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

							if ( PositionCorrection )
							{

								if ( bone.Name == "bip01_spine4" )
								{
									tx.Position += tx.Rotation.Forward * 5;
									tx.Position += tx.Rotation.Right * 2;
								}
								if ( bone.Name == "bip01_spine2" )
								{
									tx.Position += tx.Rotation.Right * 4;
								}
								if ( bone.Name == "bip01_spine1" )
								{
									tx.Position += tx.Rotation.Right * 2;
								}
								if ( bone.Name == "bip01_spine" )
								{
									tx.Position += tx.Rotation.Right * 2;
								}
								if ( bone.Name == "bip01_pelvis" )
								{
									tx.Position += tx.Rotation.Down * 2;
								}
							}
							//Log.Info( $"Copying {bone2} to {bone.Name}" );
							Destination.SetBoneTransform( bone, tx );
							continue;
						}
					}
				}
			}
		}
	}
}
