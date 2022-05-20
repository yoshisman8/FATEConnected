using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using FATEConnected.Services;
using FATEConnected.Modules;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FATEConnected
{
    internal class Program
    {
        private static string Token = "";
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
        static async Task MainAsync()
        {
            // Create the Data folder if it isn't already existing
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data"));

            // Attempt to read the Token file, throws an error if it's not there.
            try
            {
                Token = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Data", "token.txt"));
            }
            catch
            {
                Console.WriteLine("No Token file found! Please create a file called \"token.txt\" in the Data folder.");
            }

            // Configure the discord client entitiy.    
            var client = new DiscordClient(new DiscordConfiguration()
            {
                Token = Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                LogTimestampFormat = "MMM dd yyyy - hh:mm:ss tt",
                MinimumLogLevel = LogLevel.Debug
            });

            // Create the Dependency Injection services to be used within comman modules.
            var services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(new LiteDatabase(Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database.db")))
                .AddSingleton<Services.Utilities>()
                .AddSingleton<ButtonService>()
                .BuildServiceProvider();

            // Initiate the use of Slash Commands
            var Slash = client.UseSlashCommands(new SlashCommandsConfiguration()
            {
                Services = services
            });

            client.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = DSharpPlus.Interactivity.Enums.PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30)
            });

            // Register all command modules.
            Slash.RegisterCommands<CharacterModule>();
            Slash.RegisterCommands<ChararcterSubModule>();
            Slash.RegisterCommands<SetCommands>();
            Slash.RegisterCommands<SkillModule>();
            Slash.RegisterCommands<AspectModule>();
            Slash.RegisterCommands<ConsequenceModule>();
            Slash.RegisterCommands<GameplayModule>();
            Slash.RegisterCommands<CampaignModule>();



            // Register the Button Handling method into the Client.
            client.ComponentInteractionCreated += services.GetService<ButtonService>().HandleButtonAsync;

            await client.ConnectAsync();

            await Task.Delay(-1);
        }
    }
}

