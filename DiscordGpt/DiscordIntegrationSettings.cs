using System.Text.Json.Serialization;

namespace DiscordGpt
{
	public class DiscordIntegrationSettings
	{
		[JsonPropertyName("publicChannels")]
		public List<ulong> PublicChannels { get; set; } = new List<ulong>();
	}
}