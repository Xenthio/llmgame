using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LLMGame;

public class EnvironmentMaker : Component
{
	[Property] public ModelRenderer Floor { get; set; }
	[Property] public MeshComponent Mesh { get; set; }
	protected override void OnStart()
	{
		base.OnStart();
		AddEnvironment();
	}
	async void AddEnvironment()
	{
		var environment = """
[Environment creation mode activated.

Your instructions as the assistant are to create a new environment of your choice, indoors, outdoors, whatever!

Seperate instructions with a | character
			
You will provide me instructions on what to place in XML format: 

### Respond with the commands formatted like one of following:

# Create Object Command
<object><name>computer monitor,monitor,screen</name><position>-1,2</position><lookat>-2,2</lookat><static>false</static></object>

- Use static for large objects like trees, or small immovable detail like grass clumps
- Name can be a comma seperated list of names to try when searching, order by most descriptiveness to least, e.g "potted plant,pot plant,plant"
- Positions are specified as a vector2 (x,y) of top down coordinates in meters, where the origin is the center of the environment.
- Placing an object that overlaps another will place it ontop of the other object. So do them in order, things like tables go first, then anything that goes on top of it (like televisions). 
- LookAt is a vector2 (x,y), and the object will be rotated to face the specified position. This is useful for things like chairs that should face a table.
- Instead of lookat, you can manually specify yaw, Yaw is specified as a float in degrees, where 0 is facing forward (positive X), 90 is facing right (positive Y), 180 is facing backwards (negative X), 270 is facing left (negative Y)

# Set Floor Command
<floor><name>white carpet,carpet</name></floor>

- Names can be a comma seperated list of names to try when searching, order by most descriptiveness to least, e.g "wooden floor,wood"
 

ONLY RESPOND WITH THE XML OBJECTS SEPERATED BY | , DO NOT ADD ANY ADDITIONAL TEXT. NO NOT USE NEW LINES. DO NOT USE BACKTICKS.

Start placing objects now.]
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
		var message = response.choices.First().message;
		var split = message.content.Split( '|' );

		foreach ( var xml in split )
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
				//PlaceObject( obj );
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
		}
	}

	async Task<bool> SetFloor( newfloor obj )
	{
		Material material = null;
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
		if ( material == null ) return false;

		Floor.MaterialOverride = material;
		if ( Mesh.IsValid() )
		{
			Mesh.SetMaterial( material, 0 );
			Mesh.SetMaterial( material, 1 );
		}

		return true;

	}

	async Task<bool> PlaceObject( newobject obj )
	{
		Model mdl = null;
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
		if ( mdl == null ) return false;

		var positionstring = obj.position.Split( ',' );
		var position = new Vector3( float.Parse( positionstring[0] ), float.Parse( positionstring[1] ), 0 );
		//var position = new Vector3( obj.position.x, obj.position.y, 0 );
		position *= 39.3701f;

		var go = Scene.CreateObject();
		var prop = go.AddComponent<Prop>();
		var lerper = go.AddComponent<Lerper>();

		//var sweep = Scene.Trace.M()
		var tr = Scene.Trace.Ray( position.WithZ( WorldPosition.z + 200 ), position + Vector3.Down * 100 ).Run();
		var offset = 4;
		if ( obj.isStatic ) offset = 0;
		go.WorldPosition = tr.EndPosition + Vector3.Up * offset;
		go.WorldRotation = Rotation.FromYaw( obj.yaw - 90 );
		if ( obj.name.Contains( "rug" ) )
		{
			go.WorldPosition = position;
		}

		lerper.TargetPosition = go.WorldPosition;
		go.WorldPosition = go.WorldPosition.WithZ( 3000 );

		if ( !string.IsNullOrEmpty( obj.lookat ) )
		{
			var lookatstring = obj.lookat.Split( ',' );
			var lookat = new Vector3( float.Parse( lookatstring[0] ), float.Parse( lookatstring[1] ), 0 );
			lookat *= 39.3701f;
			go.WorldRotation = Rotation.LookAt( lookat.WithZ( 0 ) - go.WorldPosition.WithZ( 0 ), Vector3.Up ).Angles().WithPitch( 0 ).WithRoll( 0 ).ToRotation();
		}

		prop.IsStatic = obj.isStatic;
		prop.Model = mdl;

		return true;

	}
}
[XmlRoot( ElementName = "object" )]
public class newobject
{
	[XmlElement]
	public string name { get; set; }
	[XmlElement]
	public string position { get; set; }
	[XmlElement]
	public float yaw { get; set; }
	[XmlElement]
	public string lookat { get; set; }
	[XmlElement( ElementName = "static" )]
	public bool isStatic { get; set; }
}

[XmlRoot( ElementName = "wall" )]
public class newwall
{
	[XmlElement]
	public string position { get; set; }
	[XmlElement]
	public string yaw { get; set; }
}

[XmlRoot( ElementName = "floor" )]
public class newfloor
{
	[XmlElement]
	public string name { get; set; }
}
