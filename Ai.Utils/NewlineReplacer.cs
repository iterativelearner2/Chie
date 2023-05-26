using System.Text;

namespace Ai.Utils
{
	public static class NewlineReplacer
	{
		public static string Replace(string inStr)
		{
			StringBuilder stringBuilder = new();

			bool lastR = false;

			foreach (char c in inStr)
			{
				switch (c)
				{
					case '\r':
						lastR = true;
						stringBuilder.Append('\\');
						stringBuilder.Append(c);
						break;

					case '\n':
						if (!lastR)
						{
							stringBuilder.Append('\\');
							stringBuilder.Append('\r');
						}

						stringBuilder.Append(c);
						lastR = false;
						break;

					default:
						lastR = false;
						stringBuilder.Append(c);
						break;
				}
			}

			return stringBuilder.ToString();
		}
	}
}