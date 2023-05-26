using Ai.Abstractions;
using Ai.Utils;
using Chie;
using ChieApi.Client;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Models;
using Discord;
using Discord.WebSocket;
using DiscordGpt.Constants;
using DiscordGpt.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DiscordGpt
{
	internal class DiscordIntegrationService
	{
		private static readonly ChieClient _chieClient = new();
		private static ISocketMessageChannel? _lastChannel = null;
		private static Logger _logger;
		private readonly DiscordClient _discordClient;
		private readonly Dictionary<ulong, Dictionary<string, string>> _guildEmotes = new();
		private readonly DiscordIntegrationSettings _settings;

		[SuppressMessage("CodeQuality", "IDE0052:Remove unread private members")]
		private Task? _typingTask;

		public DiscordIntegrationService(DiscordClient discordClient, Logger logger, DiscordIntegrationSettings settings)
		{
			this._settings = settings;
			_logger = logger;
			this._discordClient = discordClient;
		}

		public async Task DeferredMessageProcessing(SocketMessage arg)
		{
			try
			{
				List<ChatEntry> entries = new();

				await foreach (byte[] imageData in arg.GetImages())
				{
					entries.Add(new ChatEntry()
					{
						Image = imageData,
						SourceUser = arg.Author.GetDisplayName()
					});
				}

				entries.Add(new()
				{
					Content = arg.Content,
					SourceUser = arg.Author.GetDisplayName(),
				});

				await _logger.Write("Sending messages to client...");

				MessageSendResponse sendResponse = await _chieClient.Send(entries.ToArray());

				_lastChannel = arg.Channel;

				if (!sendResponse.Success)
				{
					await _logger.Write("Message reported as unseen. Client busy?");
					await this.MarkUnseen(arg);
					return;
				}

				await _logger.Write("Awaiting response...");
				ChatEntry messageResponse = await _chieClient.GetReply(sendResponse.MessageId);

				await _logger.Write("Response Received. Cleaning...");

				string cleanedMessage = messageResponse.Content;

				if (arg.Channel is SocketTextChannel stcb)
				{
					cleanedMessage = this.EmojiFill(stcb, cleanedMessage);
				}

				cleanedMessage = cleanedMessage.DiscordEscape();

				await _logger.Write("Sending to chat...");

				await _logger.Write($"Message: {cleanedMessage}", LogLevel.Private);

				_ = await arg.Channel.SendMessageAsync(cleanedMessage);
			}
			catch (Exception ex)
			{
				await _logger.Write("Exception occurred");
				await _logger.Write(ex.ToString());
			}
		}

		public async Task MarkUnseen(SocketMessage message)
		{
			Emoji ninja = Emoji.Parse(Emojis.NINJA);
			await message.AddReactionAsync(ninja);
		}

		public async Task Start()
		{
			Console.WriteLine("Connecting Discord...");
			await this._discordClient.Connect();
			Console.WriteLine("Connected Discord.");

			this._discordClient.OnMessageReceived += this.Client_OnMessageReceived;

			this._typingTask = Task.Run(TypingLoop);

			await LoopUtil.Forever();
		}

		private static async Task TypingLoop()
		{
			IDisposable? lastTyping = null;

			await LoopUtil.Loop(async () =>
			{
				AiState state = (await _chieClient.Status()).State;
				lastTyping?.Dispose();

				if (state == AiState.Responding)
				{
					lastTyping = _lastChannel?.EnterTypingState();
				}
				else
				{
					lastTyping = null;
				}
			}, 5000, async ex => await _logger.Write(ex));
		}

		private async Task Client_OnMessageReceived(SocketMessage arg)
		{
			if (arg.Channel.Id == Logger.DEBUG_CHANNEL_ID)
			{
				return;
			}

			await _logger.Write($"Received Message on Channel [{arg.Channel.Id}]");

			if (!arg.IsVisible())
			{
				await _logger.Write("Message not visible. Marking.");
				await this.MarkUnseen(arg);
				return;
			}

			if (arg.Author.Username == this._discordClient.CurrentUser.Username)
			{
				await _logger.Write("Self Message. Skipping.");
				return;
			}

			if (!this._settings.PublicChannels.Contains(arg.Channel.Id) && arg.Channel is not SocketDMChannel)
			{
				await _logger.Write("Message not on visible channel. Skipping");
				return;
			}

			_ = Task.Run(async () => await this.DeferredMessageProcessing(arg));
		}

		private string EmojiFill(SocketTextChannel channel, string message)
		{
			SocketGuild guild = channel.Guild;

			if (!this._guildEmotes.TryGetValue(guild.Id, out Dictionary<string, string>? emotes))
			{
				emotes = guild.Emotes.ToDictionary(e => $"\\*+[a-zA-Z\\s]*{e.Name}[a-zA-Z\\s]*\\*+", e => $"<:{e.Name}:{e.Id}>");

				this._guildEmotes.Add(guild.Id, emotes);
			}

			string newMessage = message;

			foreach (KeyValuePair<string, string> kvp in emotes)
			{
				newMessage = Regex.Replace(newMessage, kvp.Key, kvp.Value, RegexOptions.IgnoreCase);
			}

			return newMessage;
		}
	}
}