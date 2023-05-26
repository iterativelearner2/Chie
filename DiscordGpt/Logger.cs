using Ai.Utils;
using Chie;
using ChieApi.Client;
using ChieApi.Shared.Entities;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;

namespace DiscordGpt
{
	public class Logger
	{
		public const ulong DEBUG_CHANNEL_ID = 1105296789348823120;
		private const string LAST_LOG_ENTRY_PATH = "LastLogEntry.dat";
		private static long? _lastLogEntry = null;
		private readonly ChieClient _chieClient;
		private readonly DiscordClient _discordClient;

		[SuppressMessage("CodeQuality", "IDE0052:Remove unread private members")]
		private readonly Task? _logTask;

		private SocketTextChannel _debugChannel;

		public Logger(ChieClient client, DiscordClient discordClient)
		{
			this._chieClient = client;
			this._discordClient = discordClient;
			this._logTask = Task.Run(this.LogLoop);
		}

		private long LastLogEntry
		{
			get
			{
				if (!File.Exists(LAST_LOG_ENTRY_PATH))
				{
					return 0;
				}

				if (!_lastLogEntry.HasValue)
				{
					_lastLogEntry = long.Parse(File.ReadAllText(LAST_LOG_ENTRY_PATH));
				}

				return _lastLogEntry.Value;
			}
			set
			{
				if (_lastLogEntry.HasValue)
				{
					if (value <= _lastLogEntry.Value)
					{
						throw new InvalidOperationException("Can not set log entry lower than existing value");
					}
				}

				_lastLogEntry = value;

				File.WriteAllText(LAST_LOG_ENTRY_PATH, value.ToString());
			}
		}

		public async Task<SocketTextChannel> GetDebugChannel()
		{
			if (this._debugChannel == null)
			{
				if (!this._discordClient.Connected)
				{
					await this._discordClient.Connect();
				}

				this._debugChannel = this._discordClient.GetChannel(DEBUG_CHANNEL_ID);
			}

			return this._debugChannel;
		}

		public async Task Write(string message, LogLevel logLevel = LogLevel.Info)
		{
			Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");

			if (logLevel != LogLevel.Private)
			{
				_ = await (await this.GetDebugChannel()).SendMessageAsync($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
			}
		}

		public async Task Write(Exception ex) => _ = await (await this.GetDebugChannel()).SendMessageAsync(ex.Message);

		private async Task LogLoop()
		{
			await LoopUtil.Loop(async () =>
			{
				List<LogEntry> entries = (await this._chieClient.GetLogsById(this.LastLogEntry)).ToList();

				foreach (LogEntry logEntry in entries)
				{
					_ = this.Write(logEntry.Content, logEntry.Level);

					this.LastLogEntry = logEntry.Id;
				}
			}, 1000, this.Write);
		}
	}
}