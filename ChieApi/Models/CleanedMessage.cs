using ChieApi.Shared.Entities;
using System.Text;
using System.Text.RegularExpressions;

namespace ChieApi.Models
{
	public class CleanedMessage
	{
		public CleanedMessage(string message)
		{
			List<char> foundInvalid = new();

			StringBuilder newMessage = new();

			foreach (char c in message)
			{
				if (Regex.IsMatch($"{c}", @"[^\u0000-\u007F]"))// || "\r\n|".Contains(c))
				{
					foundInvalid.Add(c);
					newMessage.Append(' ');
				}
				else
				{
					_ = newMessage.Append(c);
				}
			}

			this.Message = newMessage.ToString();

			while (this.Message.Contains("  "))
			{
				this.Message = this.Message.Replace("  ", " ");
			}

			this.InvalidCharacters = foundInvalid.ToArray();
		}

		public char[] InvalidCharacters { get; }

		public bool IsNullOrWhitespace => string.IsNullOrWhiteSpace(this.Message);

		public string Message { get; private set; }

		public static CleanedMessage Parse(ChatEntry arg1) => new(arg1.Content);
	}
}