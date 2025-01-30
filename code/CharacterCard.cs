using System.Collections.Generic;

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
}*/

public class CharacterCard
{
	public string spec;
	public string spec_version;
	public class CardData
	{
		public string name;
		public string description;
		public string personality;
		public string scenario;
		public string first_mes;
		public string mes_example;

		public string creator_notes;
		public string system_prompt;
		public string post_history_instructions;
		public string[] alternate_greetings;
		public CharacterBook character_book;

		public string[] tags;
		public string creator;
		public string character_version;
		public Dictionary<string, object> extensions;
	}

	public static CharacterCard LoadFromPNG( string path )
	{
		var result = new CharacterCard();
		// Load the PNG file
		var bytes = FileSystem.Data.ReadAllBytes( path );

		// Decode the PNG metadata

		return result;
	}
}

public class CharacterBook
{
	public string name;
	public string description;
	public int scan_depth;
	public int token_budget;
	public bool recursive_scanning;
	public Dictionary<string, object> extensions;
	public List<Entry> entries;
	public class Entry
	{
		public string[] keys;
		public string content;
		public Dictionary<string, object> extensions;
		public bool enabled;
		public int insertion_order;
		public bool case_sensitive;
		public string name;
		public int priority;
		public int id;
		public string comment;
		public bool selective;
		public string[] secondary_keys;
		public bool constant;
		public string position;
	}
}

