using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LLMGame;

public class EnvironmentMaker : SingletonComponent<EnvironmentMaker>
{
	[Property] public ModelRenderer Floor { get; set; }
	[Property] public MeshComponent FloorMesh { get; set; }
	[Property] public MeshComponent CeilingMesh { get; set; }
	protected override void OnStart()
	{
		base.OnStart();
	}
	public async void AddEnvironment( string prompt )
	{
		var initial = """
		
----- Environment Designer -----
Your instructions as the assistant are to create a new environment of your choice, indoors, outdoors, whatever!
 
""";
		if ( prompt != null )
		{
			initial = $"""
		
----- Environment Designer -----
Your instructions as the assistant are to create a new environment that matches the prompt of the user, the prompt is {prompt}.
 
""";
		}
		LanguageModel.AddMessage( "system", initial );

		var search = """
----- Stage 1: Asset search -----

Seperate instructions with a | character
			
You will provide me instructions on what terms to search the model list for in XML format: 

### Respond with the commands formatted like one of following:

# Search for Object Command
<search><query>computer monitor</query></search>

- Query is a term to try when searching.

# Search for Material Command (floors, walls, ceilings) (always search for a wall at the least)
<search><query>plaster wall</query><type>material</type></search>

- Query is a term to try when searching.

ONLY RESPOND WITH THE XML QUERIES SEPERATED BY |. DO NOT ADD ANY ADDITIONAL TEXT. DO NOT USE NEW LINES. DO NOT USE BACKTICKS.

Start searching now.
""";
		LanguageModel.AddMessage( "system", search );
		await ProcessCommand();
		LanguageModel.Instance.Messages.ElementAt( 1 ).content = """
----- Stage 1: Asset search -----
Complete!
""";

		var environment = """
----- Stage 2: Object placement -----

Seperate instructions with a | character
			
You will provide me instructions on what to place in XML format: 

### Respond with the commands formatted like one of following:

# Create Object Command
<object><ident>facepunch.tree_oak_medium_a</ident><position>-1,2</position><lookat>-2,2</lookat><static>false</static></object>

- Use static for large objects like trees, or small immovable detail like grass clumps
- Use ident to specify the model to use, this is the full ident of the model you want to use from the search results.
- Instead of ident, you can use name if you want to use the top result of anything not in the search results. This can be a comma seperated list of names to try when searching, order by most descriptiveness to least, e.g "potted plant,pot plant,plant"
- Positions are specified as a vector2 (x,y) of top down coordinates in meters, where the origin is the center of the environment.
- Placing an object that overlaps another will place it ontop of the other object. So do them in order, things like tables go first, then anything that goes on top of it (like televisions). 
- LookAt is a vector2 (x,y), and the object will be rotated to face the specified position. This is useful for things like chairs that should face a table.
- Instead of lookat, you can manually specify yaw, Yaw is specified as a float in degrees, where 0 is facing forward (positive X), 90 is facing right (positive Y), 180 is facing backwards (negative X), 270 is facing left (negative Y)
- Lift allows you to raise objects off the ground in meters, this will automatically set the object to static. Do NOT use this to place objects on tables or other objects, only for things like wall art or hanging lights.

# Set Floor Command
<floor><name>white carpet,carpet</name></floor>

- Names is a comma seperated list of names to try when searching, order by most descriptiveness to least, e.g "wooden floor,wood"

# Set Ceiling Command
<ceiling><name>ceiling</name></ceiling>

- Names is a comma seperated list of names to try when searching, order by most descriptiveness to least, e.g "wooden floor,wood"
- Note ceilings are always 3.25 meters high, but you can change the material.

# Add Wall Command
<wall><name>plaster</name><start>-5,5</start><end>5,5</end></wall>

-- A Wall that goes from start point to end point, with the specified material. Start and end are specified as vector2 (x,y) of top down coordinates in meters, where the origin is the center of the environment.
-- You can also use Ident instead of name to specify the material directly.

# Add Spotlight Command
<spotlight><position>0,0</position><lift>3.2</lift></spotlight>

-- This will place a spotlight at the specified position, with the specified lift in meters. Great for ceiling lights.

ONLY RESPOND WITH THE XML OBJECTS SEPERATED BY |. DO NOT ADD ANY ADDITIONAL TEXT. DO NOT USE NEW LINES. DO NOT USE BACKTICKS.

Start placing objects now.
""";
		LanguageModel.AddMessage( "system", environment );
		await ProcessCommand();
		//await ProcessCommand();
		//await ProcessCommand();
		//await ProcessCommand();
	}
	async Task ProcessCommand()
	{
		var response = await LanguageModel.GenerateOnly();
		//var message = new Message();
		//message.role = "assistant";
		//message.content = "<wall><name>plaster</name><start>-5,5</start><end>5,5</end></wall>|<wall><name>plaster</name><start>5,5</start><end>5,-5</end></wall>|<wall><name>plaster</name><start>5,-5</start><end>-5,-5</end></wall>|<wall><name>plaster</name><start>-5,-5</start><end>-5,5</end></wall>|<wall><name>plaster</name><start>20,20</start><end>25,25</end></wall>";
		var message = response.choices.First().message;
		var split = message.content.Split( '|' );

		foreach ( var xml in split )
		{
			try
			{
				Log.Info( xml );
				if ( xml.StartsWith( "<object>" ) )
				{
					var serializer = new XmlSerializer( typeof( newobject ) );
					newobject obj = (newobject)serializer.Deserialize( new StringReader( xml ) );
					var success = await PlaceObject( obj );
					if ( success ) LanguageModel.AddMessage( message.role, xml );
				}
				if ( xml.StartsWith( "<wall>" ) )
				{
					var serializer = new XmlSerializer( typeof( newwall ) );
					newwall obj = (newwall)serializer.Deserialize( new StringReader( xml ) );
					var success = await AddWall( obj );
					if ( success ) LanguageModel.AddMessage( message.role, xml );
					//LanguageModel.Instance.Messages.Add( message );
				}
				if ( xml.StartsWith( "<floor>" ) )
				{
					var serializer = new XmlSerializer( typeof( newfloor ) );
					newfloor obj = (newfloor)serializer.Deserialize( new StringReader( xml ) );
					var success = await SetFloor( obj );
					if ( success ) LanguageModel.AddMessage( message.role, xml );
					//LanguageModel.Instance.Messages.Add( message );
				}
				if ( xml.StartsWith( "<ceiling>" ) )
				{
					var serializer = new XmlSerializer( typeof( newceiling ) );
					newceiling obj = (newceiling)serializer.Deserialize( new StringReader( xml ) );
					var success = await SetCeiling( obj );
					if ( success ) LanguageModel.AddMessage( message.role, xml );
					//LanguageModel.Instance.Messages.Add( message );
				}
				if ( xml.StartsWith( "<spotlight>" ) )
				{
					var serializer = new XmlSerializer( typeof( newspotlight ) );
					newspotlight obj = (newspotlight)serializer.Deserialize( new StringReader( xml ) );
					var success = await AddSpotlight( obj );
					if ( success ) LanguageModel.AddMessage( message.role, xml );
					//LanguageModel.Instance.Messages.Add( message );
				}
				if ( xml.StartsWith( "<search>" ) )
				{
					var serializer = new XmlSerializer( typeof( newsearch ) );
					newsearch obj = (newsearch)serializer.Deserialize( new StringReader( xml ) );
					//LanguageModel.AddMessage( message.role, xml );
					var type = "model";
					if ( obj.type != null ) type = obj.type;
					var result = await SearchFor( obj, type );
					LanguageModel.AddMessage( "system", result );
					//LanguageModel.Instance.Messages.Add( message );
				}
			}
			catch ( System.Exception e )
			{
				Log.Info( e.Message );
			}
		}
	}

	async Task<string> SearchFor( newsearch obj, string type = "model" )
	{
		string resultstring = "";
		var results = await Package.FindAsync( $"{obj.query} type:{type} sort:popular", take: 8 );
		if ( results.Packages.Length <= 0 ) return $"No results found for {obj.query}";
		resultstring += $"----- Results for {obj.query} -----\n";
		foreach ( var result in results.Packages )
		{
			var fullpackage = await Package.Fetch( result.FullIdent, false );
			var typespecific = "";
			if ( type == "model" )
			{
				var boundsmins = fullpackage.GetMeta<Vector3>( "RenderMins" );
				var boundsmaxs = fullpackage.GetMeta<Vector3>( "RenderMaxs" );
				boundsmins *= 0.0254f;
				boundsmaxs *= 0.0254f;
				typespecific = $"<bounds-mins>{boundsmins.x},{boundsmins.y}</bounds-mins><bounds-maxs>{boundsmins.x},{boundsmins.y}</bounds-maxs>";
			}
			resultstring += $"<result><ident>{result.FullIdent}</ident><name>{result.Title}</name><type>{type}</type>{typespecific}</result>\n";
		}
		return resultstring;
	}
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

		Floor.MaterialOverride = material;
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

