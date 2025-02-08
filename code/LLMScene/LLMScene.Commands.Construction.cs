using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public partial class LLMScene : SingletonComponent<LLMScene>
{
	public class SpotlightCommand
	{
		public string position { get; set; }
		public float lift { get; set; }
		public string color { get; set; } = "#FFEDBC";
	}
	[CommandHandler( "spotlight" )]
	async Task<bool> AddSpotlight( string xml, ILLMBeing sender = null )
	{
		var obj = XmlDeserializer.Deserialize<SpotlightCommand>( xml );

		var go = Scene.CreateObject();
		var prop = go.AddComponent<Prop>();
		var lerper = go.AddComponent<Lerper>();
		var light = go.AddComponent<SpotLight>();
		var positionstring = obj.position.Split( ',' );
		var position = new Vector3( float.Parse( positionstring[0] ), float.Parse( positionstring[1] ), 0 );
		position *= 39.3701f;
		var offset = 4;
		if ( obj.lift > 0 ) offset = 0;
		go.WorldPosition = position + Vector3.Up * offset;
		if ( obj.lift > 0 )
		{
			go.WorldPosition += Vector3.Up * obj.lift * 39.3701f;
		}
		light.WorldRotation = Rotation.FromPitch( 90 );
		light.LightColor = Color.Parse( obj.color ).Value;
		light.Radius = 1000;
		light.ConeInner = 60;
		light.ConeOuter = 70;
		light.Enabled = true;

		lerper.TargetPosition = go.WorldPosition;
		go.WorldPosition = go.WorldPosition.WithZ( 1000 );
		return true;
	}
	public class WallCommand
	{
		public string name { get; set; }
		public string ident { get; set; }
		public string start { get; set; }
		public string end { get; set; }
	}
	[CommandHandler( "wall" )]
	async Task<bool> AddWall( string xml, ILLMBeing sender = null )
	{
		var obj = XmlDeserializer.Deserialize<WallCommand>( xml );

		Material material = null;
		if ( obj.ident == null )
		{
			if ( obj.name.Contains( "," ) )
			{
				var names = obj.name.Split( ',' );
				foreach ( var name in names )
				{
					material = await CloudLookup.GetMaterialFromName( name );
					if ( material != null ) break;
				}
			}
			else
			{
				material = await CloudLookup.GetMaterialFromName( obj.name );
			}
		}
		else
		{
			material = await CloudLookup.GetMaterialFromIdent( obj.ident );
		}
		if ( material == null ) return false;

		var go = Scene.CreateObject();
		var model = go.AddComponent<ModelRenderer>();
		var lerper = go.AddComponent<Lerper>();
		var mesh = new Mesh();

		var start = obj.start.Split( ',' );
		var end = obj.end.Split( ',' );
		var startPos = new Vector3( float.Parse( start[0] ), float.Parse( start[1] ), 0 ) * 39.3701f;
		var endPos = new Vector3( float.Parse( end[0] ), float.Parse( end[1] ), 0 ) * 39.3701f;
		var height = 128;
		var thickness = 8;

		var vertices = new List<Vertex>();
		var indices = new List<int>();

		// Calculate the 2D (XY) positions from the start and end positions.
		var start2D = new Vector2( startPos.x, startPos.y );
		var end2D = new Vector2( endPos.x, endPos.y );
		var dir2D = end2D - start2D;

		var wallLength = dir2D.Length;
		if ( wallLength < 0.0001f )
			return false; // Too short a wall

		var uScale = wallLength / 128.0f;
		var vScale = height / 128.0f;

		// Get the normalized direction along the wall and compute a perpendicular.
		// This ensures that even for diagonal walls, the offset is computed correctly.
		var wallDir2D = dir2D.Normal;
		var perp2D = new Vector2( -wallDir2D.y, wallDir2D.x ).Normal; // 90° rotated

		// Compute half the thickness and the offset vector.
		float halfThickness = thickness * 0.5f;
		Vector2 offset2D = perp2D * halfThickness;

		// Define the bottom vertices in the XY plane (with Z = 0).
		// "Front" is defined as the side in the direction of -offset.
		Vector3 A = new Vector3( start2D.x - offset2D.x, start2D.y - offset2D.y, 0 ); // front left
		Vector3 B = new Vector3( end2D.x - offset2D.x, end2D.y - offset2D.y, 0 ); // front right
		Vector3 C = new Vector3( start2D.x + offset2D.x, start2D.y + offset2D.y, 0 ); // back left
		Vector3 D = new Vector3( end2D.x + offset2D.x, end2D.y + offset2D.y, 0 ); // back right

		// Create top vertices by extruding upward by 'height' (along Z).
		Vector3 A2 = A + Vector3.Up * height;
		Vector3 B2 = B + Vector3.Up * height;
		Vector3 C2 = C + Vector3.Up * height;
		Vector3 D2 = D + Vector3.Up * height;

		// Helper method to add a face (quad) with tangent data.
		// We calculate the tangent as the normalized vector from v0 to v1,
		// which corresponds to increasing U in our UV layout.
		void AddFace( Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 faceNormal )
		{
			int startIndex = vertices.Count;
			Vector3 tangent = (v1 - v0).Normal; // Calculate the tangent vector

			// Note: new Vertex( position, normal, tangent, texcoord0 )
			vertices.Add( new Vertex( v0, faceNormal, tangent, new Vector2( 0, 1 ) ) );
			vertices.Add( new Vertex( v1, faceNormal, tangent, new Vector2( uScale, 1 ) ) );
			vertices.Add( new Vertex( v2, faceNormal, tangent, new Vector2( uScale, 1 - vScale ) ) );
			vertices.Add( new Vertex( v3, faceNormal, tangent, new Vector2( 0, 1 - vScale ) ) );

			// Create two triangles for this quad.
			indices.Add( startIndex + 0 );
			indices.Add( startIndex + 1 );
			indices.Add( startIndex + 2 );

			indices.Add( startIndex + 0 );
			indices.Add( startIndex + 2 );
			indices.Add( startIndex + 3 );
		}

		// Convert the 2D directions to 3D vectors (still lying in the XY plane).
		Vector3 wallDir3D = new Vector3( wallDir2D.x, wallDir2D.y, 0 );
		Vector3 perp3D = new Vector3( perp2D.x, perp2D.y, 0 );

		// ─── Build the wall faces ─────────────────────────────────────────

		// Front face (facing in the -perp3D direction):
		// Uses vertices: A (bottom front left), B (bottom front right), B2 (top front right), A2 (top front left)
		AddFace( A, B, B2, A2, -perp3D );

		// Back face (facing in the +perp3D direction):
		// Uses vertices: D (bottom back right), C (bottom back left), C2 (top back left), D2 (top back right)
		AddFace( D, C, C2, D2, perp3D );

		// Left face (start end, facing -wallDir3D):
		// Uses vertices: C (bottom back left), A (bottom front left), A2 (top front left), C2 (top back left)
		AddFace( C, A, A2, C2, -wallDir3D );

		// Right face (end end, facing +wallDir3D):
		// Uses vertices: B (bottom front right), D (bottom back right), D2 (top back right), B2 (top front right)
		AddFace( B, D, D2, B2, wallDir3D );

		// Top face (facing upward):
		// Uses vertices: A2, B2, D2, C2
		AddFace( A2, B2, D2, C2, Vector3.Up );

		// (Optional) Bottom face (if needed; often omitted if the wall sits on the ground):
		AddFace( C, D, B, A, -Vector3.Up );

		mesh.CreateVertexBuffer<Vertex>( vertices.Count, Vertex.Layout, vertices.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );
		model.Model = Model.Builder.AddMesh( mesh ).Create();
		model.MaterialOverride = material;

		lerper.TargetPosition = go.WorldPosition;
		go.WorldPosition += Vector3.Up * 1000;

		return true;
	}

	public class FloorCommand
	{
		public string name { get; set; }
		public string ident { get; set; }
	}
	[CommandHandler( "floor" )]
	async Task<bool> SetFloor( string xml, ILLMBeing sender = null )
	{
		var obj = XmlDeserializer.Deserialize<FloorCommand>( xml );

		Material material = null;
		if ( obj.ident == null )
		{
			if ( obj.name.Contains( "," ) )
			{
				var names = obj.name.Split( ',' );
				foreach ( var name in names )
				{
					material = await CloudLookup.GetMaterialFromName( name );
					if ( material != null ) break;
				}
			}
			else
			{
				material = await CloudLookup.GetMaterialFromName( obj.name );
			}
		}
		else
		{
			material = await CloudLookup.GetMaterialFromIdent( obj.ident );
		}
		if ( material == null ) return false;

		if ( FloorMesh.IsValid() )
		{
			FloorMesh.SetMaterial( material, 0 );
			FloorMesh.SetMaterial( material, 1 );
		}

		return true;

	}

	public class CeilingCommand
	{
		public string name { get; set; }
		public string ident { get; set; }
	}
	[CommandHandler( "ceiling" )]
	async Task<bool> SetCeiling( string xml, ILLMBeing sender = null )
	{
		var obj = XmlDeserializer.Deserialize<CeilingCommand>( xml );

		Log.Info( "setting ceiling..." );
		Material material = null;
		if ( obj.ident == null )
		{
			if ( obj.name.Contains( "," ) )
			{
				var names = obj.name.Split( ',' );
				foreach ( var name in names )
				{
					material = await CloudLookup.GetMaterialFromName( name );
					if ( material != null ) break;
				}
			}
			else
			{
				material = await CloudLookup.GetMaterialFromName( obj.name );
			}
		}
		else
		{
			material = await CloudLookup.GetMaterialFromIdent( obj.ident );
		}
		if ( material == null ) return false;

		if ( CeilingMesh.IsValid() )
		{
			CeilingMesh.GameObject.Enabled = true;
			CeilingMesh.SetMaterial( material, 0 );
			CeilingMesh.SetMaterial( material, 1 );

			var lerper = CeilingMesh.GameObject.AddComponent<Lerper>();

			lerper.TargetPosition = CeilingMesh.GameObject.WorldPosition;
			CeilingMesh.GameObject.WorldPosition += Vector3.Up * 1000;
			return true;
		}

		return false;

	}

	class ObjectCommand
	{
		public string name { get; set; }
		public string ident { get; set; }
		public string position { get; set; }
		public float yaw { get; set; }
		public float lift { get; set; }
		public string lookat { get; set; }
		public bool isstatic { get; set; }
	}
	[CommandHandler( "object" )]
	async Task<bool> PlaceObject( string xml, ILLMBeing sender = null )
	{
		// Deserialize
		var obj = XmlDeserializer.Deserialize<ObjectCommand>( xml );

		await Task.Delay( 600 );
		Model mdl = null;
		if ( obj.name != null )
		{
			if ( obj.name.Contains( "," ) )
			{
				var names = obj.name.Split( ',' );
				foreach ( var name in names )
				{
					mdl = await CloudLookup.GetModelFromName( name );
					if ( mdl != null && mdl.Physics.Parts.Any() ) break;
				}
			}
			else
			{
				mdl = await CloudLookup.GetModelFromName( obj.name );
			}
		}
		if ( obj.ident != null )
		{
			mdl = await CloudLookup.GetModelFromIdent( obj.ident );
		}

		if ( mdl == null ) return false;

		var positionstring = obj.position.Split( ',' );
		var position = new Vector3( float.Parse( positionstring[0] ), float.Parse( positionstring[1] ), 0 );
		//var position = new Vector3( obj.position.x, obj.position.y, 0 );
		position *= 39.3701f;

		var go = Scene.CreateObject();
		var prop = go.AddComponent<Prop>();
		var lerper = go.AddComponent<Lerper>();

		//var sweep = Scene.Trace.M()
		var tr = Scene.Trace.Ray( position.WithZ( WorldPosition.z + 80 ), position + Vector3.Down * 80 ).WithoutTags( "ceiling" ).Run();
		var offset = 4;
		if ( obj.isstatic || obj.lift > 0 ) offset = 0;
		go.WorldPosition = tr.EndPosition + Vector3.Up * offset;
		go.WorldRotation = Rotation.FromYaw( obj.yaw - 90 );

		bool IsStatic = obj.isstatic;
		if ( obj.lift > 0 )
		{
			go.WorldPosition += Vector3.Up * obj.lift * 39.3701f;
			IsStatic = true;
		}

		if ( obj.name != null && obj.name.Contains( "rug" ) )
		{
			go.WorldPosition = position;
		}

		lerper.TargetPosition = go.WorldPosition;
		lerper.ShouldEnablePhysics = !IsStatic;
		go.WorldPosition = go.WorldPosition.WithZ( 1000 );

		if ( !string.IsNullOrEmpty( obj.lookat ) )
		{
			var lookatstring = obj.lookat.Split( ',' );
			var lookat = new Vector3( float.Parse( lookatstring[0] ), float.Parse( lookatstring[1] ), 0 );
			lookat *= 39.3701f;
			go.WorldRotation = Rotation.LookAt( lookat.WithZ( 0 ) - go.WorldPosition.WithZ( 0 ), Vector3.Up ).Angles().WithPitch( 0 ).WithRoll( 0 ).ToRotation();
		}

		prop.IsStatic = IsStatic;
		prop.Model = mdl;

		if ( !IsStatic && go.Components.TryGet<Rigidbody>( out var phys ) )
			phys.MotionEnabled = false;

		return true;

	}
}
