using ChieApi.Extensions;
using ChieApi.Interfaces;
using ChieApi.Shared.Entities;
using Dapper;
using System.Data.SqlClient;

namespace ChieApi.Services
{
	public class ChatService
	{
		private const string CHAT_INSERT = @"INSERT INTO [dbo].[ChatEntry]
           ([DateCreated]
           ,[SourceUser]
           ,[Content]
           ,[ReplyToId])
			output INSERTED.ID
			 VALUES
				   ({0}
				   ,{1}
				   ,{2}
				   ,{3})";

		private readonly string _connectionString;

		public ChatService(IHasConnectionString connectionString)
		{
			this._connectionString = connectionString.ConnectionString;
		}

		public ChatEntry GetLastMessage(string sourceUser = null)
		{
			using SqlConnection connection = new(this._connectionString);

			string query = "select top 1 * from chatentry ";

			if (!string.IsNullOrEmpty(sourceUser))
			{
				query += "where SourceUser = " + sourceUser;
			}

			query += " order by id desc";

			return connection.Query<ChatEntry>(query).FirstOrDefault();
		}

		public async Task<long> Save(ChatEntry chatEntry)
		{
			chatEntry.DateCreated = DateTime.Now;

			using SqlConnection connection = new(this._connectionString);

			return connection.Insert(CHAT_INSERT, chatEntry.DateCreated, chatEntry.SourceUser, chatEntry.Content, chatEntry.ReplyToId);
		}

		public bool TryGetOriginal(long originalMessageId, out ChatEntry? chatEntry)
		{
			using SqlConnection connection = new(this._connectionString);

			chatEntry = connection.Query<ChatEntry>($"select * from chatentry where ReplyToId = {originalMessageId}").FirstOrDefault();

			return chatEntry != null;
		}
	}
}