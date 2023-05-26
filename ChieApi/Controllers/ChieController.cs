using ChieApi.Extensions;
using ChieApi.Interfaces;
using ChieApi.Services;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Interfaces;
using ChieApi.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChieApi.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ChieController : ControllerBase, IChieClient
	{
		private readonly LogService _databaseService;
		private readonly LlamaService _llamaService;
		private readonly List<IRequestPipeline> _pipelines;

		public ChieController(IEnumerable<IRequestPipeline> pipelines, LlamaService llamaService, LogService databaseService)
		{
			this._databaseService = databaseService;
			this._llamaService = llamaService;
			this._pipelines = pipelines.ToList();
		}

		[HttpGet("GetLogsByDate")]
		public async Task<List<LogEntry>> GetLogsByDate(string after) => this._databaseService.GetLogs(DateTime.Parse(after));

		[HttpGet("GetLogsById")]
		public async Task<List<LogEntry>> GetLogsById(long after) => this._databaseService.GetLogs(after);

		[HttpGet("GetReply")]
		public async Task<ChatEntry> GetReply(long id)
		{
			if (this._llamaService.TryGetReply(id, out ChatEntry? ce))
			{
				return ce;
			}
			else
			{
				return new ChatEntry() { };
			}
		}

		[HttpPost("Send")]
		public async Task<MessageSendResponse> Send(ChatEntry[] chatEntries)
		{
			await this._llamaService.Initialization;

			List<ChatEntry> processedEntries = chatEntries.ToList();

			foreach (IRequestPipeline requestPipeline in this._pipelines)
			{
				processedEntries = await requestPipeline.Process(processedEntries);
			}

			return new MessageSendResponse()
			{
				MessageId = await this._llamaService.Send(processedEntries.ToArray())
			};
		}

		[HttpGet("Status")]
		public async Task<StatusResponse> Status()
		{
			return new StatusResponse()
			{
				State = this._llamaService.AiState
			};
		}
	}
}