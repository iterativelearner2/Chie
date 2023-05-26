using ChieApi.Shared.Entities;
using ChieApi.Shared.Models;

namespace ChieApi.Shared.Interfaces
{
	public interface IChieClient
	{
		Task<List<LogEntry>> GetLogsByDate(string after);

		Task<List<LogEntry>> GetLogsById(long after);

		Task<ChatEntry> GetReply(long id);

		Task<MessageSendResponse> Send(ChatEntry[] chatEntry);

		Task<StatusResponse> Status();
	}
}