using Ai.Abstractions;
using System.Text.Json.Serialization;

namespace ChieApi.Shared.Models
{
	public class StatusResponse
	{
		[JsonPropertyName("state")]
		public AiState State { get; set; }
	}
}