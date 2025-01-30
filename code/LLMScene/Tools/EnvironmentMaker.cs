namespace LLMGame;

public class EnvironmentMaker : SingletonComponent<EnvironmentMaker>
{
	protected override void OnStart()
	{
		base.OnStart();
		foreach ( var hud in Scene.Components.GetAll<HUD>() )
		{
			hud.ShowEnviromentPanel = true;
		}
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
		await LLMScene.Instance.GenerateAndRunCommands();
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
<object><ident>facepunch.tree_oak_medium_a</ident><position>-1,2</position><lookat>-2,2</lookat><isstatic>false</isstatic></object>

- Use isstatic for large objects like trees, or small immovable detail like grass clumps
- Use ident to specify the model to use, this is the full ident of the model you want to use from the search results.
- Instead of ident, you can use name if you want to use the top result of anything not in the search results. This can be a comma seperated list of names to try when searching, order by most descriptiveness to least, e.g "potted plant,pot plant,plant"
- Positions are specified as a vector2 (x,y) of top down coordinates in meters, where the origin is the center of the environment.
- Placing an object that overlaps another will place it ontop of the other object. So do them in order, things like tables go first, then anything that goes on top of it (like televisions). 
- LookAt is a vector2 (x,y), and the object will be rotated to face the specified position. This is useful for things like chairs that should face a table.
- Instead of lookat, you can manually specify yaw, Yaw is specified as a float in degrees, where 0 is facing forward (positive X), 90 is facing right (positive Y), 180 is facing backwards (negative X), 270 is facing left (negative Y)
- Lift allows you to raise objects off the ground in meters, this will automatically set the object to isstatic. Do NOT use this to place objects on tables or other objects, only for things like wall art or hanging lights.

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
		await LLMScene.Instance.GenerateAndRunCommands();
		//await ProcessCommand();
		//await ProcessCommand();
		//await ProcessCommand();
	}
}
