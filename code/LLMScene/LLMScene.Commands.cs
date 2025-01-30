using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LLMGame;

public partial class LLMScene : SingletonComponent<LLMScene>
{
	public async Task GenerateAndRunCommands( bool outputCommand = false )
	{
		var response = await LanguageModel.GenerateOnly();
		await RunCommandsInResponse( response, outputCommand );
	}

	public async Task RunCommandsInResponse( APIChatResponse response, bool outputCommand = false, ILLMBeing sender = null )
	{
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
					if ( success && outputCommand ) LanguageModel.AddMessage( message.role, xml );
				}
				if ( xml.StartsWith( "<wall>" ) )
				{
					var serializer = new XmlSerializer( typeof( newwall ) );
					newwall obj = (newwall)serializer.Deserialize( new StringReader( xml ) );
					var success = await AddWall( obj );
					if ( success && outputCommand ) LanguageModel.AddMessage( message.role, xml );
					//LanguageModel.Instance.Messages.Add( message );
				}
				if ( xml.StartsWith( "<floor>" ) )
				{
					var serializer = new XmlSerializer( typeof( newfloor ) );
					newfloor obj = (newfloor)serializer.Deserialize( new StringReader( xml ) );
					var success = await SetFloor( obj );
					if ( success && outputCommand ) LanguageModel.AddMessage( message.role, xml );
					//LanguageModel.Instance.Messages.Add( message );
				}
				if ( xml.StartsWith( "<ceiling>" ) )
				{
					var serializer = new XmlSerializer( typeof( newceiling ) );
					newceiling obj = (newceiling)serializer.Deserialize( new StringReader( xml ) );
					var success = await SetCeiling( obj );
					if ( success && outputCommand ) LanguageModel.AddMessage( message.role, xml );
					//LanguageModel.Instance.Messages.Add( message );
				}
				if ( xml.StartsWith( "<spotlight>" ) )
				{
					var serializer = new XmlSerializer( typeof( newspotlight ) );
					newspotlight obj = (newspotlight)serializer.Deserialize( new StringReader( xml ) );
					var success = await AddSpotlight( obj );
					if ( success && outputCommand ) LanguageModel.AddMessage( message.role, xml );
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
}
