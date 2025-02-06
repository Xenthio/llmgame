using System.Threading.Tasks;

namespace LLMGame;

public partial class LLMScene : SingletonComponent<LLMScene>
{
	public class WalkToCommand
	{
		public string target { get; set; }
		public string position { get; set; }
	}

	[CommandHandler( "walkto" )]
	async Task<bool> WalkTo( string xml, ILLMBeing sender = null )
	{
		var obj = XmlDeserializer.Deserialize<WalkToCommand>( xml );
		if ( sender is LLMCharacter chr )
		{

			if ( obj.target != null )
			{
				var go = GetObjectByName( obj.target );
				await chr.WalkToObjectAsync( go );
			}
			else if ( obj.position != null )
			{
				var positionstring = obj.position.Split( ',' );
				var position = new Vector3( float.Parse( positionstring[0] ), float.Parse( positionstring[1] ), 0 );
				//var position = new Vector3( obj.position.x, obj.position.y, 0 );
				position *= METERS_2_INCH;
				await chr.WalkToPositionAsync( position );
			}
		}
		return true;
	}
	public class LookAtCommand
	{
		public string target { get; set; }
		public string position { get; set; }
	}
	[CommandHandler( "lookat" )]
	async Task<bool> LookAt( string xml, ILLMBeing sender = null )
	{
		Log.Info( "looking" );
		var obj = XmlDeserializer.Deserialize<LookAtCommand>( xml );
		if ( sender is LLMCharacter chr )
		{

			if ( obj.target != null )
			{
				chr.LookAtObject( GetObjectByName( obj.target ) );
				await Task.Delay( 500 );
			}
			else if ( obj.position != null )
			{
				var positionstring = obj.position.Split( ',' );
				var position = new Vector3( float.Parse( positionstring[0] ), float.Parse( positionstring[1] ), 0 );
				//var position = new Vector3( obj.position.x, obj.position.y, 0 );
				position *= METERS_2_INCH;
				chr.LookAtPosition( position );
				while ( !chr.EyeAngles.AsVector3().AlmostEqual( chr.TargetEyeAngles.AsVector3(), 2 ) )
				{
					await Task.Delay( 100 );
				}
			}
		}
		return true;
	}
}
