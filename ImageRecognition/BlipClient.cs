using Loxifi;
using System.Drawing;
using System.Text;

namespace ImageRecognition
{
	public class BlipClient
	{
		private const string TEMP_FILE_NAME = "TempFile.png";
		private readonly BlipClientSettings _settings;

		public BlipClient(BlipClientSettings settings)
		{
			this._settings = settings;
		}

		public async Task<string> Describe(byte[] data)
		{
			Bitmap b = new(Image.FromStream(new MemoryStream(data)));

			if (File.Exists(TEMP_FILE_NAME))
			{
				File.Delete(TEMP_FILE_NAME);
			}

			b.Save(TEMP_FILE_NAME);

			StringBuilder resultBuilder = new();
			StringBuilder errorBuilder = new();

			int tries = 0;

			do
			{
				try
				{
					ProcessSettings settings = new("Python.exe")
					{
						Arguments = $"{this._settings.PredictPath} {Path.Combine(Directory.GetCurrentDirectory(), TEMP_FILE_NAME)}",
						StdOutWrite = (s, e) => resultBuilder.Append(e),
						StdErrWrite = (s, e) => errorBuilder.Append(e),
						WorkingDirectory = new FileInfo(this._settings.PredictPath).DirectoryName
					};

					uint r = await ProcessRunner.StartAsync(settings);

					string result = resultBuilder.ToString();

					return result.Trim();
				}
				catch (Exception ex) when (tries++ < 3)
				{
				}
			} while (true);
		}
	}
}