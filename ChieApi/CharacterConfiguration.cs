using Llama.Shared;

namespace ChieApi
{
	public class CharacterConfiguration : LlamaSettings
	{
		public string CharacterName { get; set; }

		public override string? InSuffix
		{
			get => $"|{this.CharacterName}>";
			set { }
		}
	}
}