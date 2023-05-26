using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using ImageRecognition;
using System.Text.RegularExpressions;

namespace ChieApi.Pipelines
{
	public class ImageRecognitionPipeline : IRequestPipeline
	{
		public List<string> RemoveRegex = new()
		{
			"^there is "
		};

		private readonly BlipClient _blipClient;

		public ImageRecognitionPipeline(BlipClient clipClient)
		{
			this._blipClient = clipClient;
		}

		public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
		{
			if (!chatEntry.HasImage)
			{
				yield return chatEntry;
				yield break;
			}

			string description = await this._blipClient.Describe(chatEntry.Image);

			foreach (string removeRegex in this.RemoveRegex)
			{
				description = Regex.Replace(description, removeRegex, "", RegexOptions.IgnoreCase);
			}

			yield return new ChatEntry()
			{
				SourceUser = chatEntry.SourceUser,
				Content = $"*Sends an image of {description}*"
			};

			if (chatEntry.HasText)
			{
				yield return new ChatEntry()
				{
					SourceUser = chatEntry.SourceUser,
					Content = chatEntry.Content
				};
			}
		}
	}
}