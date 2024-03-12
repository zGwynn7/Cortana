﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot
{
    public class DiscordBot
    {
        public static void BootDiscordBot() => new DiscordBot().MainAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient Cortana;

        public async Task MainAsync()
        {
            var client = new DiscordSocketClient(ConfigureSocket());
            var config = ConfigurationBuilder();
            var services = ConfigureServices(client);

            var commands = services.GetRequiredService<InteractionService>();

            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            client.Log += LogAsync;
            client.MessageReceived += Client_MessageReceived;
            client.UserVoiceStateUpdated += OnUserVoiceStateUpdate;
            client.JoinedGuild += OnServerJoin;
            client.LeftGuild += OnServerLeave;
            client.UserJoined += OnUserJoin;
            client.UserLeft += OnUserLeft;

            commands.Log += LogAsync;

            client.Ready += async () =>
            {
                Cortana = client;

                DiscordData.InitSettings(client.Guilds);
                DiscordData.LoadMemes();
                DiscordData.LoadIGDB();
                DiscordData.LoadGamingProfiles();
                DiscordData.Cortana = Cortana;

                //await commands.RegisterCommandsToGuildAsync(DiscordData.DiscordIDs.NoMenID, true);
                //await commands.RegisterCommandsToGuildAsync(DiscordData.DiscordIDs.HomeID, true);
                await commands.RegisterCommandsGloballyAsync(true);

                var activityTimer = new Utility.UtilityTimer(Name: "activity-timer", Hours: 0, Minutes: 0, Seconds: 10, Callback: ActivityTimerElapsed, TimerLocation: ETimerLocation.DiscordBot, Loop: ETimerLoop.Interval);
                var statusTimer = new Utility.UtilityTimer(Name: "status-timer", Hours: 24, Minutes: 0, Seconds: 0, Callback: StatusTimerElapsed, TimerLocation: ETimerLocation.DiscordBot, Loop: ETimerLoop.Interval);

                foreach (var guild in Cortana.Guilds)
                {
                    var channel = Modules.AudioHandler.GetAvailableChannel(guild);
                    if (channel != null) Modules.AudioHandler.Connect(channel);
                }

                DiscordData.SendToChannel("I'm Online", ECortanaChannels.Cortana);
            };

            await client.LoginAsync(TokenType.Bot, config["token"]);
            await client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            if (arg.Author.Id == DiscordData.DiscordIDs.CortanaID) return;
            var message = arg.Content.ToLower();

            if (arg.Channel.GetChannelType() != ChannelType.DM)
            {
                var channel = arg.Channel as SocketGuildChannel;
                if (channel != null)
                {
                    foreach (string word in DiscordData.GuildSettings[channel.Guild.Id].BannedWords)
                    {
                        if (message.Contains(word))
                        {
                            await arg.Channel.SendMessageAsync("Ho trovato una parola non consentita, sono costretta ad eliminare il messaggio");
                            await arg.DeleteAsync();
                            return;
                        }
                    }
                }
            }

            if (message == "cortana") await arg.Channel.SendMessageAsync($"Dimmi {arg.Author.Mention}");
            else if (message == "ciao cortana") await arg.Channel.SendMessageAsync($"Ciao {arg.Author.Mention}");
        }

        private static async void ActivityTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                string temp = Utility.HardwareDriver.GetCPUTemperature();
                Game activity = new Game($"on Raspberry at {temp}", ActivityType.Playing);
                await Cortana.SetActivityAsync(activity);
            }
            catch
            {
                Utility.Functions.Log("Discord", "Errore di connessione, impossibile aggiornare l'Activity Status");
            }
        }

        private static void StatusTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Embed embed = DiscordData.CreateEmbed(title: "Still alive Chief", description: Utility.HardwareDriver.GetCPUTemperature());
            DiscordData.SendToChannel(embed: embed, ECortanaChannels.Cortana);
        }

        public static async Task Disconnect()
        {
            foreach (var guild in Modules.AudioHandler.AudioClients)
            {
                DiscordData.GuildSettings[guild.Key].AutoJoin = false;
                await Modules.AudioHandler.Play("Shutdown", guild.Key, EAudioSource.Local);
                await Task.Delay(1500);
                Modules.AudioHandler.Disconnect(guild.Key);
            }

            Utility.TimerHandler.RemoveTimers(ETimerLocation.DiscordBot);

            await Task.Delay(1000);
            await Cortana.StopAsync();
            await Cortana.LogoutAsync();
        }

        async Task OnUserVoiceStateUpdate(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            var guild = (oldState.VoiceChannel ?? newState.VoiceChannel).Guild;

            Modules.AudioHandler.HandleConnection(guild);

            if (user.Id == DiscordData.DiscordIDs.CortanaID) return;

            if (oldState.VoiceChannel == null && newState.VoiceChannel != null)
            {
                var displayName = newState.VoiceChannel.Guild.GetUser(user.Id).DisplayName;
                string title = $"Ciao {displayName}";
                Embed embed = DiscordData.CreateEmbed(title, withoutAuthor: true, footer: new EmbedFooterBuilder { IconUrl = user.GetAvatarUrl(), Text = "Joined at:" });

                if (DiscordData.GuildSettings[guild.Id].Greetings) await guild.GetTextChannel(DiscordData.GuildSettings[guild.Id].GreetingsChannel).SendMessageAsync(embed: embed);

                if (DiscordData.TimeConnected.ContainsKey(user.Id)) DiscordData.TimeConnected.Remove(user.Id);
                DiscordData.TimeConnected.Add(user.Id, DateTime.Now);

                if (newState.VoiceChannel != Modules.AudioHandler.GetCurrentCortanaChannel(guild)) return;

                await Task.Delay(1000);
                await Modules.AudioHandler.Play("Hello", newState.VoiceChannel.Guild.Id, EAudioSource.Local);
            }
            else if (oldState.VoiceChannel != null && newState.VoiceChannel == null)
            {
                var displayName = oldState.VoiceChannel.Guild.GetUser(user.Id).DisplayName;
                string title = $"A dopo {displayName}";
                Embed embed = DiscordData.CreateEmbed(title, withoutAuthor: true, footer: new EmbedFooterBuilder { IconUrl = user.GetAvatarUrl(), Text = "Left at:" });
                if (DiscordData.GuildSettings[guild.Id].Greetings) await guild.GetTextChannel(DiscordData.GuildSettings[guild.Id].GreetingsChannel).SendMessageAsync(embed: embed);

                if (DiscordData.TimeConnected.ContainsKey(user.Id)) DiscordData.TimeConnected.Remove(user.Id);
            }
        }

        async Task OnServerJoin(SocketGuild guild)
        {
            DiscordData.AddGuildSettings(guild);

            await guild.DefaultChannel.SendMessageAsync(embed: DiscordData.CreateEmbed("Ciao, sono Cortana"));
        }

        Task OnServerLeave(SocketGuild guild)
        {
            if (DiscordData.GuildSettings.ContainsKey(guild.Id)) DiscordData.GuildSettings.Remove(guild.Id);
            DiscordData.UpdateSettings();
            return Task.CompletedTask;
        }

        async Task OnUserJoin(SocketGuildUser user)
        {
            if (user.IsBot) return;

            await user.Guild.GetTextChannel(DiscordData.GuildSettings[user.Guild.Id].GreetingsChannel).SendMessageAsync(embed: DiscordData.CreateEmbed($"Benvenuto {user.DisplayName}"));
        }

        Task OnUserLeft(SocketGuild guild, SocketUser user)
        {
            if (user.IsBot) return Task.CompletedTask;

            return Task.CompletedTask;
        }

        static Task LogAsync(LogMessage message)
        {
            Utility.Functions.Log("Discord", message.Message);
            return Task.CompletedTask;
        }

        private DiscordSocketConfig ConfigureSocket()
        {
            return new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
                AlwaysDownloadUsers = true,
                UseInteractionSnowflakeDate = false
            };
        }
        private ServiceProvider ConfigureServices(DiscordSocketClient client)
        {
            return new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();
        }

        private IConfigurationRoot ConfigurationBuilder()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Data/Discord/Token.json")
                .Build();
        }
    }
}