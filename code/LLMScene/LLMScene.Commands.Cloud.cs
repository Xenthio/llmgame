using System;
using System.Threading.Tasks;

namespace LLMGame;

public partial class LLMScene : SingletonComponent<LLMScene>
{
	public class SearchCommand
	{
		public string query { get; set; }
		public string type { get; set; }
	}
	async Task<string> SearchFor( string xml, ILLMBeing sender = null )
	{
		var obj = XmlDeserializer.Deserialize<SearchCommand>( xml );

		var type = obj.type ?? "model";

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
				boundsmins *= LLMScene.INCH_2_METERS;
				boundsmaxs *= LLMScene.INCH_2_METERS;
				var minx = MathF.Round( boundsmins.x, 2 );
				var miny = MathF.Round( boundsmins.y, 2 );
				var maxx = MathF.Round( boundsmaxs.x, 2 );
				var maxy = MathF.Round( boundsmaxs.y, 2 );
				typespecific = $"<bounds-mins>{maxx},{maxy}</bounds-mins><bounds-maxs>{minx},{miny}</bounds-maxs>";
			}
			resultstring += $"<result><ident>{result.FullIdent}</ident><name>{result.Title}</name><type>{type}</type>{typespecific}</result>\n";
		}
		return resultstring;
	}
}
