using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public partial class LLMScene : SingletonComponent<LLMScene>
{
	public async Task GenerateAndRunCommands( List<Message> messages, ILLMBeing sender = null, bool outputCommand = false )
	{
		var response = await LanguageAPI.GenerateChatResponseFromMessages( messages );
		await RunCommandsInResponse( response, outputCommand, sender );
	}
	[ConCmd( "llm_runcommand" )]
	public static void RunCommandConCmd( string command )
	{
		Instance.RunCommandsInString( command );
	}
	public async Task RunCommandsInString( string command, bool outputCommand = false, ILLMBeing sender = null )
	{
		var message = new Message();
		message.Content = command;
		await RunCommandsInMessage( message, outputCommand, sender );
	}
	public async Task RunCommandsInResponse( ChatResponse response, bool outputCommand = false, ILLMBeing sender = null )
	{
		var message = response.Messages.First();
		await RunCommandsInMessage( message, outputCommand, sender );
	}

	public async Task RunCommandsInMessage( Message message, bool outputCommand = false, ILLMBeing sender = null )
	{
		var split = message.Content.Split( '|' );

		foreach ( var xml in split )
		{
			try
			{
				Log.Info( xml );
				await RunCommand( xml, sender, outputCommand );
			}
			catch ( System.Exception e )
			{
				Log.Error( e.Message );
			}
		}
	}

	public async Task<bool> RunCommand( string xml, ILLMBeing sender = null, bool outputCommand = false, string role = "assistant" )
	{

		var commandName = GetCommandName( xml );
		if ( commandName != null )
		{
			var method = TypeLibrary.GetMethodsWithAttribute<CommandHandlerAttribute>( false )
						.FirstOrDefault( x => x.Attribute.CommandName == commandName ).Method;
			//var method = GetType().GetMethods( BindingFlags.NonPublic | BindingFlags.Instance )
			//	.FirstOrDefault( m => m.GetCustomAttribute<CommandHandlerAttribute>()?.CommandName == commandName );

			if ( method != null )
			{
				var success = await method.InvokeWithReturn<Task<bool>>( this, new object[] { xml, sender } );
				if ( success )
				{
					//if ( outputCommand ) LanguageModel.AddMessage( role, xml );
					return true;
				}
			}
		}
		return false;
	}

	private string GetCommandName( string xml )
	{
		if ( xml.StartsWith( "<" ) && xml.EndsWith( ">" ) )
		{
			int start = xml.IndexOf( '<' ) + 1;
			int end = xml.IndexOf( '>' );

			return xml.Substring( start, end - start );
		}
		return null;
	}

	[CommandHandler( "search" )]
	private async Task<bool> HandleSearchCommand( string xml, ILLMBeing sender = null )
	{
		var result = await SearchFor( xml, sender );
		sender.Memory.Add( Message.System( result ) );
		return true;
	}
	[CommandHandler( "lookat" )]
	private async Task<bool> HandleLookAtCommand( string xml, ILLMBeing sender = null )
	{
		/*var serializer = new XmlSerializer( typeof( newsearch ) );
		newsearch obj = (newsearch)serializer.Deserialize( new StringReader( xml ) );
		var type = obj.type ?? "model";
		var result = await SearchFor( obj, type );
		LanguageModel.AddMessage( "system", result );*/
		return true;
	}
}
