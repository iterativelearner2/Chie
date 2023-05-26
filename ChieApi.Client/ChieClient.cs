using ChieApi.Client.Extensions;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Interfaces;
using ChieApi.Shared.Models;

namespace ChieApi.Client
{
	public class ChieClient : IChieClient
	{
		private const int PORT = 5000;
		private readonly HttpClient _client;

		public ChieClient()
		{
			this._client = new HttpClient();
		}

		public async Task<List<LogEntry>> GetLogsByDate(string after) => await this._client.GetJsonAsync<List<LogEntry>>($"http://127.0.0.1:{PORT}/Chie/GetLogsByDate?after={after}");

		public async Task<List<LogEntry>> GetLogsById(long after) => await this._client.GetJsonAsync<List<LogEntry>>($"http://127.0.0.1:{PORT}/Chie/GetLogsById?after={after}");

		public async Task<ChatEntry> GetReply(long originalMessageId)
		{
			do
			{
				ChatEntry response = await this._client.GetJsonAsync<ChatEntry>($"http://127.0.0.1:{PORT}/Chie/GetReply?id={originalMessageId}");

				if (response.Id != 0)
				{
					return response;
				}

				await Task.Delay(2000);
			} while (true);
		}

		public async Task<MessageSendResponse> Send(ChatEntry chatEntry) => await this.Send(new ChatEntry[] { chatEntry });

		public async Task<MessageSendResponse> Send(ChatEntry[] chatEntry) => await this._client.PostJsonAsync<MessageSendResponse>($"http://127.0.0.1:{PORT}/Chie/Send", chatEntry);

		public async Task<StatusResponse> Status() => await this._client.GetJsonAsync<StatusResponse>($"http://127.0.0.1:{PORT}/Chie/Status");
	}
}