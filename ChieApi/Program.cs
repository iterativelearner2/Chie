using ChieApi.Factories;
using ChieApi.Interfaces;
using ChieApi.Pipelines;
using ChieApi.Services;
using ImageRecognition;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace ChieApi
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			ConfigurationBuilder configurationBuilder = new();
			configurationBuilder.AddUserSecrets<Program>();
			IConfigurationRoot configuration = configurationBuilder.Build();

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			_ = builder.Services.AddControllers();

			if (args.Length > 0)
			{
				_ = builder.Services.AddSingleton<ICharacterNameFactory>(new CommandLineCharacterNameFactory(args[0]));
			}
			else
			{
				_ = builder.Services.AddSingleton<ICharacterNameFactory, SecretCharacterNameFactory>();
			}

			_ = builder.Services.AddSingleton<ChatService>();
			_ = builder.Services.AddSingleton<LogService>();
			_ = builder.Services.AddSingleton<BlipClient>();
			_ = builder.Services.AddSingleton<LlamaService>();
			_ = builder.Services.AddSingleton<ICharacterFactory, CharacterService>();
			_ = builder.Services.AddTransient<IRequestPipeline, ImageRecognitionPipeline>();
			_ = builder.Services.AddTransient<IRequestPipeline, MessageCleaningPipeline>();
			_ = builder.Services.AddTransient<IRequestPipeline, TimePassagePipeline>();
			_ = builder.Services.AddSingleton<IHostLifetime>(new NullLifetime());
			_ = builder.Services.Configure<ChieApiSettings>(configuration.GetSection(nameof(ChieApiSettings)));
			_ = builder.Services.Configure<BlipClientSettings>(configuration.GetSection(nameof(BlipClientSettings)));
			_ = builder.Services.AddSingleton(s => s.GetService<IOptions<BlipClientSettings>>().Value);
			_ = builder.Services.AddSingleton(s => s.GetService<IOptions<ChieApiSettings>>().Value);
			_ = builder.Services.AddSingleton<IHasConnectionString>(s => s.GetService<IOptions<ChieApiSettings>>().Value);

			_ = builder.Services.Configure<JsonOptions>(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

			WebApplication app = builder.Build();

			_ = app.UseAuthorization();

			_ = app.MapControllers();

			CancellationTokenSource cts = new();

			Task t = app.RunAsync(cts.Token);

			//Needs to be executed so client starts before
			//first request
			await Task.Run(async () =>
			{
				await Task.Delay(3000);
				app.Services.GetService<LlamaService>();
			});

			await t;
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
		}
	}
}