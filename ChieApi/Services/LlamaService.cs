using Ai.Abstractions;
using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Shared.Entities;
using Llama;
using Loxifi;

namespace ChieApi.Services
{
	public class LlamaService
	{
		private readonly ICharacterFactory _characterFactory;
		private readonly SemaphoreSlim _chatLock = new(1);
		private readonly ChatService _chatService;
		private readonly Thread _killThread;
		private readonly LogService _logService;
		private int _characterTimeoutMs = int.MaxValue;
		private LlamaClient _client;
		private DateTime _lastKeyStroke = DateTime.MinValue;

		public LlamaService(ICharacterFactory characterFactory, LogService logService, ChatService chatService)
		{
			this._characterFactory = characterFactory;
			this._logService = logService ?? throw new ArgumentNullException(nameof(logService));
			this._chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));

			_ = logService.Log("Constructing Llama Service");

			_ = this.SetState(AiState.Initializing);

			this.Initialization = Task.Run(this.Init);

			this._killThread = new Thread(async () => await this.KillThread());

			this._killThread.Start();
		}

		public AiState AiState { get; private set; }

		public Task Initialization { get; private set; }

		private long LastMessageId { get; set; }

		public async Task KillThread()
		{
			do
			{
				await Task.Delay(1000);

				if (this._client is null)
				{
					continue;
				}

				try
				{
					if (this.AiState == AiState.Responding && (DateTime.Now - this._lastKeyStroke).TotalMilliseconds > this._characterTimeoutMs)
					{
						_ = this._logService.Log("Timed out. Sending kill signal to return control...");
						this._client.Kill();
						this._client.Send(System.Environment.NewLine, false);
					}
				}
				catch (Exception ex)
				{
					await this._logService.Log(ex.Message);
				}
			} while (true);
		}

		public async Task<long> Send(ChatEntry[] chatEntries)
		{
			await this.Initialization;

			try
			{
				this._chatLock.Wait();

				if (this.AiState != AiState.Idle)
				{
					await this._logService.Log($"Client not idle. Skipping ({chatEntries.Length}) messages.");
					return 0;
				}

				await this._logService.Log("Sending messages to client...");

				CleanedMessage[] cleanedMessages = chatEntries.Select(CleanedMessage.Parse).ToArray();

				for (int i = 0; i < chatEntries.Length; i++)
				{
					bool last = i == chatEntries.Length - 1;

					CleanedMessage cleanedMessage = cleanedMessages[i];

					await this.SendText(chatEntries[i], last);
				}

				await this._logService.Log($"Last Message Id: {this.LastMessageId}");

				return this.LastMessageId;
			}
			finally
			{
				_ = this._chatLock.Release();
			}
		}

		public bool TryGetReply(long originalMessageId, out ChatEntry? chatEntry) => this._chatService.TryGetOriginal(originalMessageId, out chatEntry);

		private async Task Init()
		{
			_ = this._logService.Log("Constructing Llama Client");
			CharacterConfiguration configuration = await this._characterFactory.Build();
			_characterTimeoutMs = configuration.Timeout;

			this._client = new LlamaClient(configuration);
			_ = this._logService.Log(this._client.Args);

			this._client.ResponseReceived += new EventHandler<string>(async (s, e) => await this.LlamaClient_ResponseReceived(s, e));
			this._client.TypingResponse += new EventHandler(async (s, e) => await this.LlamaClient_IsTyping(s, e));

			await this._logService.Log("Connecting to client...");
			await this._client.Connect();
			await this._logService.Log("Connected to client.");

			_ = await this.SetState(AiState.Idle);
		}

		private async Task LlamaClient_IsTyping(object? s, EventArgs e)
		{
			this._lastKeyStroke = DateTime.Now;

			if (this.AiState == AiState.Processing)
			{
				_ = await this.SetState(AiState.Responding);
			}
		}

		private async Task LlamaClient_ResponseReceived(object? sender, string? e)
		{
			string? userName = e.From("|").To(">");
			string? content = e.From(">").To("|").Trim();

			if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(content))
			{
				await this._logService.Log("Empty message returned. Ignoring...");
				return;
			}

			ChatEntry chatEntry = new()
			{
				ReplyToId = this.LastMessageId,
				Content = content,
				SourceUser = userName
			};

			_ = await this.SetState(AiState.Idle);

			if (chatEntry.Content != null)
			{
				_ = this._chatService.Save(chatEntry);
			}
		}

		private async Task SendText(ChatEntry chatEntry, bool flush)
		{
			await this.Initialization;

			_ = await this.SetState(AiState.Processing);

			this.LastMessageId = await this._chatService.Save(chatEntry);

			string toSend;

			if (!string.IsNullOrWhiteSpace(chatEntry.SourceUser))
			{
				toSend = $"|{chatEntry.SourceUser}> {chatEntry.Content}";
			}
			else
			{
				toSend = $"[{chatEntry.Content}]";
			}

			this._client.Send(toSend, flush);
		}

		private async Task<bool> SetState(AiState state)
		{
			if (this.AiState != state)
			{
				await this._logService.Log("Setting client state: " + state.ToString());
				this.AiState = state;
				return true;
			}

			return false;
		}
	}
}