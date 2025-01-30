using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LLMGame;

public partial class LLMScene : SingletonComponent<LLMScene>
{
	async Task<bool> AddSpotlight( newspotlight obj )
	{
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
	async Task<bool> AddWall( newwall obj )
	{
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

		// Calculate wall direction and perpendicular
		var wallDir = (endPos - startPos).Normal;
		var perpendicular = new Vector3( -wallDir.y, wallDir.x, 0 ).Normal;

		// Calculate the four corners of the wall (bottom and top)
		var halfThickness = thickness * 0.5f;
		var bottomLeft = startPos + (perpendicular * halfThickness);
		var bottomRight = startPos - (perpendicular * halfThickness);
		var topLeft = bottomLeft + Vector3.Up * height;
		var topRight = bottomRight + Vector3.Up * height;
		var bottomLeftEnd = endPos + (perpendicular * halfThickness);
		var bottomRightEnd = endPos - (perpendicular * halfThickness);
		var topLeftEnd = bottomLeftEnd + Vector3.Up * height;
		var topRightEnd = bottomRightEnd + Vector3.Up * height;

		// Calculate UV scale based on wall length and height
		var wallLength = (endPos - startPos).Length;
		var uScale = wallLength / 128.0f;
		var vScale = height / 128.0f;

		// Front face
		vertices.AddRange( new[] {
			new Vertex(bottomRight, -perpendicular, Vector3.Up, new Vector4(0, 0, 0, 0)),
			new Vertex(bottomRightEnd, -perpendicular, Vector3.Up, new Vector4(uScale, 0, 0, 0)),
			new Vertex(topRightEnd, -perpendicular, Vector3.Up, new Vector4(uScale, vScale, 0, 0)),
			new Vertex(topRight, -perpendicular, Vector3.Up, new Vector4(0, vScale, 0, 0))
		} );

		// Back face
		vertices.AddRange( new[] {
			new Vertex(bottomLeftEnd, perpendicular, Vector3.Up, new Vector4(0, 0, 0, 0)),
			new Vertex(bottomLeft, perpendicular, Vector3.Up, new Vector4(uScale, 0, 0, 0)),
			new Vertex(topLeft, perpendicular, Vector3.Up, new Vector4(uScale, vScale, 0, 0)),
			new Vertex(topLeftEnd, perpendicular, Vector3.Up, new Vector4(0, vScale, 0, 0))
		} );

		// Top face
		vertices.AddRange( new[] {
			new Vertex(topRight, Vector3.Up, wallDir, new Vector4(0, 0, 0, 0)),
			new Vertex(topRightEnd, Vector3.Up, wallDir, new Vector4(uScale, 0, 0, 0)),
			new Vertex(topLeftEnd, Vector3.Up, wallDir, new Vector4(uScale, thickness/128.0f, 0, 0)),
			new Vertex(topLeft, Vector3.Up, wallDir, new Vector4(0, thickness/128.0f, 0, 0))
		} );

		// Bottom face
		vertices.AddRange( new[] {
			new Vertex(bottomLeft, Vector3.Down, wallDir, new Vector4(0, 0, 0, 0)),
			new Vertex(bottomLeftEnd, Vector3.Down, wallDir, new Vector4(uScale, 0, 0, 0)),
			new Vertex(bottomRightEnd, Vector3.Down, wallDir, new Vector4(uScale, thickness/128.0f, 0, 0)),
			new Vertex(bottomRight, Vector3.Down, wallDir, new Vector4(0, thickness/128.0f, 0, 0))
		} );

		// End face
		vertices.AddRange( new[] {
			new Vertex(bottomRightEnd, wallDir, Vector3.Up, new Vector4(0, 0, 0, 0)),
			new Vertex(bottomLeftEnd, wallDir, Vector3.Up, new Vector4(thickness/128.0f, 0, 0, 0)),
			new Vertex(topLeftEnd, wallDir, Vector3.Up, new Vector4(thickness/128.0f, vScale, 0, 0)),
			new Vertex(topRightEnd, wallDir, Vector3.Up, new Vector4(0, vScale, 0, 0))
		} );

		// Start face
		vertices.AddRange( new[] {
			new Vertex(bottomLeft, -wallDir, Vector3.Up, new Vector4(0, 0, 0, 0)),
			new Vertex(bottomRight, -wallDir, Vector3.Up, new Vector4(thickness/128.0f, 0, 0, 0)),
			new Vertex(topRight, -wallDir, Vector3.Up, new Vector4(thickness/128.0f, vScale, 0, 0)),
			new Vertex(topLeft, -wallDir, Vector3.Up, new Vector4(0, vScale, 0, 0))
		} );

		// Add indices for all six faces
		for ( int i = 0; i < 6; i++ )
		{
			int baseIndex = i * 4;
			indices.AddRange( new[] {
			baseIndex, baseIndex + 1, baseIndex + 2,
			baseIndex, baseIndex + 2, baseIndex + 3
		} );
		}

		mesh.CreateVertexBuffer<Vertex>( vertices.Count, Vertex.Layout, vertices.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );
		model.Model = Model.Builder.AddMesh( mesh ).Create();
		model.MaterialOverride = material;

		lerper.TargetPosition = go.WorldPosition;
		go.WorldPosition += Vector3.Up * 1000;

		return true;
	}
	async Task<bool> SetFloor( newfloor obj )
	{
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
	async Task<bool> SetCeiling( newceiling obj )
	{
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

	async Task<bool> PlaceObject( newobject obj )
	{
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
		if ( obj.isStatic || obj.lift > 0 ) offset = 0;
		go.WorldPosition = tr.EndPosition + Vector3.Up * offset;
		go.WorldRotation = Rotation.FromYaw( obj.yaw - 90 );

		if ( obj.lift > 0 )
		{
			go.WorldPosition += Vector3.Up * obj.lift * 39.3701f;
			obj.isStatic = true;
		}

		if ( obj.name != null && obj.name.Contains( "rug" ) )
		{
			go.WorldPosition = position;
		}

		lerper.TargetPosition = go.WorldPosition;
		lerper.ShouldEnablePhysics = !obj.isStatic;
		go.WorldPosition = go.WorldPosition.WithZ( 1000 );

		if ( !string.IsNullOrEmpty( obj.lookat ) )
		{
			var lookatstring = obj.lookat.Split( ',' );
			var lookat = new Vector3( float.Parse( lookatstring[0] ), float.Parse( lookatstring[1] ), 0 );
			lookat *= 39.3701f;
			go.WorldRotation = Rotation.LookAt( lookat.WithZ( 0 ) - go.WorldPosition.WithZ( 0 ), Vector3.Up ).Angles().WithPitch( 0 ).WithRoll( 0 ).ToRotation();
		}

		prop.IsStatic = obj.isStatic;
		prop.Model = mdl;

		if ( !obj.isStatic && go.Components.TryGet<Rigidbody>( out var phys ) )
			phys.MotionEnabled = false;

		return true;

	}
}
[XmlRoot( ElementName = "object" )]
public class newobject
{
	[XmlElement]
	public string name { get; set; }
	[XmlElement]
	public string ident { get; set; }
	[XmlElement]
	public string position { get; set; }
	[XmlElement]
	public float yaw { get; set; }
	[XmlElement]
	public float lift { get; set; }
	[XmlElement]
	public string lookat { get; set; }
	[XmlElement( ElementName = "static" )]
	public bool isStatic { get; set; }
}

[XmlRoot( ElementName = "wall" )]
public class newwall
{
	[XmlElement]
	public string name { get; set; }
	[XmlElement]
	public string ident { get; set; }
	[XmlElement]
	public string start { get; set; }
	[XmlElement]
	public string end { get; set; }
}

[XmlRoot( ElementName = "floor" )]
public class newfloor
{
	[XmlElement]
	public string name { get; set; }
	[XmlElement]
	public string ident { get; set; }
}

[XmlRoot( ElementName = "ceiling" )]
public class newceiling
{
	[XmlElement]
	public string name { get; set; }
	[XmlElement]
	public string ident { get; set; }
}

[XmlRoot( ElementName = "search" )]
public class newsearch
{
	[XmlElement]
	public string query { get; set; }
	[XmlElement]
	public string type { get; set; }
}

[XmlRoot( ElementName = "spotlight" )]
public class newspotlight
{
	[XmlElement]
	public string position { get; set; }
	[XmlElement]
	public float lift { get; set; }
	[XmlElement]
	public string color { get; set; } = "#FFEDBC";
}
