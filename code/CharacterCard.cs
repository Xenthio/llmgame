using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LLMGame;

// Found a cool spec for character cards, it's heavily used by many so I want to use it so I'm able to use the huge library of characters that are available. Unfortunately it's stored as a PNG which is kinda strange.
/*
type TavernCardV2 = {
  spec: 'chara_card_v2'
  spec_version: '2.0' // May 8th addition
  data: {
    name: string
    description: string
    personality: string
    scenario: string
    first_mes: string
    mes_example: string

    // New fields start here
    creator_notes: string
    system_prompt: string
    post_history_instructions: string
    alternate_greetings: Array<string>
    character_book?: CharacterBook

    // May 8th additions
    tags: Array<string>
    creator: string
    character_version: string
    extensions: Record<string, any>
  }
}

 * type CharacterBook = {
  name?: string
  description?: string
  scan_depth?: number // agnai: "Memory: Chat History Depth"
  token_budget?: number // agnai: "Memory: Context Limit"
  recursive_scanning?: boolean // no agnai equivalent. whether entry content can trigger other entries
  extensions: Record<string, any>
  entries: Array<{
    keys: Array<string>
    content: string
    extensions: Record<string, any>
    enabled: boolean
    insertion_order: number // if two entries inserted, lower "insertion order" = inserted higher
    case_sensitive?: boolean

    // FIELDS WITH NO CURRENT EQUIVALENT IN SILLY
    name?: string // not used in prompt engineering
    priority?: number // if token budget reached, lower priority value = discarded first

    // FIELDS WITH NO CURRENT EQUIVALENT IN AGNAI
    id?: number // not used in prompt engineering
    comment?: string // not used in prompt engineering
    selective?: boolean // if `true`, require a key from both `keys` and `secondary_keys` to trigger the entry
    secondary_keys?: Array<string> // see field `selective`. ignored if selective == false
    constant?: boolean // if true, always inserted in the prompt (within budget limit)
    position?: 'before_char' | 'after_char' // whether the entry is placed before or after the character defs
  }>

interface CharacterCardV3{
  spec: 'chara_card_v3'
  spec_version: '3.0'
  data: {
    // fields from CCV2
    name: string
    description: string
    tags: Array<string>
    creator: string
    character_version: string
    mes_example: string
    extensions: Record<string, any>
    system_prompt: string
    post_history_instructions: string
    first_mes: string
    alternate_greetings: Array<string>
    personality: string
    scenario: string

    //Changes from CCV2
    creator_notes: string
    character_book?: Lorebook

    //New fields in CCV3
    assets?: Array<{
      type: string
      uri: string
      name: string
      ext: string
    }>
    nickname?: string
    creator_notes_multilingual?: Record<string, string>
    source?: string[]
    group_only_greetings: Array<string>
    creation_date?: number
    modification_date?: number
  }
}

type Lorebook = {
  name?: string
  description?: string
  scan_depth?: number 
  token_budget?: number
  recursive_scanning?: boolean
  extensions: Record<string, any>
  entries: Array<{
    keys: Array<string>
    content: string
    extensions: Record<string, any>
    enabled: boolean
    insertion_order: number
    case_sensitive?: boolean

    //V3 Additions
    use_regex: boolean

    //On V2 it was optional, but on V3 it is required to implement
    constant?: boolean

    // Optional Fields
    name?: string
    priority?: number
    id?: number|string
    comment?: string

    selective?: boolean
    secondary_keys?: Array<string>
    position?: 'before_char' | 'after_char'
  }>
}
}*/

public class CharacterAsset
{
	public string type { get; set; }
	public string uri { get; set; }
	public string name { get; set; }
	public string ext { get; set; }
}
public class CardData
{
	public string name { get; set; }
	public string description { get; set; }
	public string personality { get; set; }
	public string scenario { get; set; }
	public string first_mes { get; set; }
	public string mes_example { get; set; }

	public string creator_notes { get; set; }
	public string system_prompt { get; set; }
	public string post_history_instructions { get; set; }
	public string[] alternate_greetings { get; set; }
	public LorebookData character_book { get; set; }

	public string[] tags { get; set; }
	public string creator { get; set; }
	public string character_version { get; set; }
	public Dictionary<string, object> extensions { get; set; }

	// v3 additions
	public CharacterAsset[] assets;
	public string nickname;
	public Dictionary<string, string> creator_notes_multilingual { get; set; }
	public string[] source { get; set; }
	public string[] group_only_greetings { get; set; }
	public long creation_date { get; set; }
	public long modification_date { get; set; }
}
public class CharacterCard
{
	public string spec { get; set; }
	public string spec_version { get; set; }
	public CardData data { get; set; }

	public static CharacterCard LoadFromPNG( string path )
	{
		Log.Info( $"Loading Character Card from PNG: {path}" );
		var result = new CharacterCard();
		// Load the PNG file
		var bytes = FileSystem.Data.ReadAllBytes( path ).ToArray();

		// PNG file signature
		var pngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
		int offset = 8; // Skip the signature

		while ( offset < bytes.Length )
		{
			int chunkLength = BitConverter.ToInt32( bytes, offset );
			chunkLength = System.Net.IPAddress.NetworkToHostOrder( chunkLength ); // Convert from big-endian to little-endian
			offset += 4;

			string chunkType = Encoding.ASCII.GetString( bytes, offset, 4 );
			offset += 4;

			Log.Info( $"Processing: {offset} of {bytes.Length}, {chunkLength}" );
			byte[] chunkData = new byte[chunkLength];
			Array.Copy( bytes, offset, chunkData, 0, chunkLength );
			offset += chunkLength;

			int chunkCRC = BitConverter.ToInt32( bytes, offset );
			offset += 4;

			// Process the chunk
			if ( chunkType == "tEXt" )
			{
				string textData = Encoding.ASCII.GetString( chunkData );
				if ( textData.Contains( "ccv3" ) || textData.Contains( "chara" ) )
				{
					// Extract base64 encoded JSON
					int jsonStart = textData.IndexOf( '\0' ) + 1;
					string base64Json = textData.Substring( jsonStart );
					string json = Encoding.UTF8.GetString( Convert.FromBase64String( base64Json ) );

					// Deserialize JSON to CharacterCard
					result = JsonSerializer.Deserialize<CharacterCard>( json );
					Log.Info( $"Loaded Character Card: {result.data.name}" );
					return result;
				}
			}
		}

		return result;
	}
}

public class Lorebook
{
	public string spec { get; set; }
	public LorebookData data { get; set; }
}

public class LorebookData
{
	public string name { get; set; }
	public string description { get; set; }
	public int scan_depth { get; set; }
	public int token_budget { get; set; }
	public bool recursive_scanning { get; set; }
	public Dictionary<string, object> extensions { get; set; }
	public List<Entry> entries { get; set; }
	public class Entry
	{
		public string[] keys { get; set; }
		public string content { get; set; }
		public Dictionary<string, object> extensions { get; set; }
		public bool enabled { get; set; }
		public int insertion_order { get; set; }
		public bool case_sensitive { get; set; }
		public string name { get; set; }
		public int priority { get; set; }
		public int id { get; set; }
		public string comment { get; set; }
		public bool selective { get; set; }
		public string[] secondary_keys { get; set; }
		public bool constant { get; set; }
		public string position { get; set; }

		// V3 Additions
		public bool use_regex;

	}
}

