﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using IGDB;

namespace DiscordBot.Modules
{
    public class UtilityModule : InteractionModuleBase<SocketInteractionContext>
    {
        [Group("personal", "Comandi personali")]
        public class PersonalGroup : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("comandi", "Vi mostro le categorie dei miei comandi")]
            public async Task ShowCommands()
            {
                var commandsEmbed = DiscordData.CreateEmbed("Comandi", withTimeStamp: false);
                commandsEmbed = commandsEmbed.ToEmbedBuilder()
                    .AddField("/media", "Gestione audio dei canali vocali")
                    .AddField("/domotica", "Domotica personale riservata")
                    .AddField("/timer", "Gestione timer e sveglie")
                    .AddField("/personal", "Comandi per gli utenti")
                    .AddField("/utility", "Funzioni di utility")
                    .AddField("/random", "Scelte random")
                    .AddField("/games", "Comandi per videogames")
                    .AddField("/unipi", "Gestione Università di Pisa")
                    .AddField("/gestione", "Gestione Server Discord")
                    .AddField("/settings", "Impostazioni Server Discord")
                    .Build();
                await RespondAsync(embed: commandsEmbed);
            }

            [SlashCommand("avatar", "Vi mando la vostra immagine profile")]
            public async Task GetAvatar([Summary("user", "Di chi vuoi vedere l'immagine?")] SocketUser user, [Summary("grandezza", "Grandezza dell'immagine [Da 64px a 4096px, inserisci un numero da 1 a 7]"), MaxValue(7), MinValue(1)] int size = 4)
            {
                var url = user.GetAvatarUrl(size: Convert.ToUInt16(Math.Pow(2, size + 5)));
                Embed embed = DiscordData.CreateEmbed("Profile Picture", user);
                embed = embed.ToEmbedBuilder().WithImageUrl(url).Build();
                await RespondAsync(embed: embed);
            }

            [SlashCommand("progetti", "Vi mando il link di Notion, per gestire i vostri progetti")]
            public async Task GetProjects()
            {
                Embed NotionEmbed = DiscordData.CreateEmbed("Progetti");
                NotionEmbed = NotionEmbed.ToEmbedBuilder()
                    .AddField("Notion", "[Vai a Notion](https://www.notion.so)")
                    .Build();
                await RespondAsync(embed: NotionEmbed);
            }
        }

        [Group("utility", "Comandi di utilità")]
        public class UtilityGroup : InteractionModuleBase<SocketInteractionContext>
        {

            [SlashCommand("my-code", "Vi mando il mio codice")]
            public async Task SendMyCode([Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                Embed GithubEmbed = DiscordData.CreateEmbed("Github");
                GithubEmbed = GithubEmbed.ToEmbedBuilder()
                    .AddField("Cortana", "[Vai al codice](https://github.com/GwynbleiddN7/Cortana)")
                    .Build();
                await RespondAsync(embed: GithubEmbed, ephemeral: Ephemeral == EAnswer.Si);
            }

            [SlashCommand("ping", "Pinga un IP", runMode: RunMode.Async)]
            public async Task Ping([Summary("ip", "IP da pingare")] string ip)
            {
                bool result;
                if (ip == "pc") result = Utility.HardwareDriver.PingPC();
                else result = Utility.HardwareDriver.Ping(ip);

                if (result) await RespondAsync($"L'IP {ip} ha risposto al ping");
                else await RespondAsync($"L'IP {ip} non ha risposto al ping");
            }

            [SlashCommand("tempo-in-voicechat", "Da quanto tempo state in chat vocale?")]
            public async Task TimeConnected([Summary("user", "A chi è rivolto?")] SocketUser? User = null, [Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                if (User == null) User = Context.User;
                if (DiscordData.TimeConnected.ContainsKey(User.Id))
                {
                    var DeltaTime = DateTime.Now.Subtract(DiscordData.TimeConnected[User.Id]);
                    string ConnectionTime = "Sei connesso da";
                    if (DeltaTime.Hours > 0) ConnectionTime += $" {DeltaTime.Hours} ore";
                    if (DeltaTime.Minutes > 0) ConnectionTime += $" {DeltaTime.Minutes} minuti";
                    if (DeltaTime.Seconds > 0) ConnectionTime += $" {DeltaTime.Seconds} secondi";

                    var embed = DiscordData.CreateEmbed(title: ConnectionTime, user: User);
                    await RespondAsync(embed: embed, ephemeral: Ephemeral == EAnswer.Si);
                }
                else
                {
                    var embed = DiscordData.CreateEmbed(title: "Non connesso alla chat vocale", user: User);
                    await RespondAsync(embed: embed, ephemeral: Ephemeral == EAnswer.Si);
                }
            }

            [SlashCommand("qrcode", "Creo un QRCode con quello che mi dite")]
            public async Task CreateQR([Summary("contenuto", "Cosa vuoi metterci?")] string content, [Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No, [Summary("colore-base", "Vuoi il colore bianco normale?")] EAnswer NormalColor = EAnswer.No, [Summary("bordo", "Vuoi aggiungere il bordo?")] EAnswer QuietZones = EAnswer.Si)
            {
                var ImageStream = Utility.Functions.CreateQRCode(content, NormalColor == EAnswer.Si, QuietZones == EAnswer.Si);

                await RespondWithFileAsync(fileStream: ImageStream, fileName: "QRCode.png", ephemeral: Ephemeral == EAnswer.Si);
            }


            [SlashCommand("conta-parole", "Scrivi un messaggio e ti dirò quante parole e caratteri ci sono")]
            public async Task CountWorld([Summary("contenuto", "Cosa vuoi metterci?")] string content, [Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                Embed embed = DiscordData.CreateEmbed("Conta Parole");
                embed = embed.ToEmbedBuilder()
                    .AddField("Parole", content.Split(" ").Length)
                    .AddField("Caratteri", content.Replace(" ", "").Length)
                    .AddField("Caratteri con spazi", content.Length)
                    .Build();

                await RespondAsync(embed: embed, ephemeral: Ephemeral == EAnswer.Si);
            }

            [SlashCommand("scrivi", "Scrivo qualcosa al posto vostro")]
            public async Task WriteSomething([Summary("testo", "Cosa vuoi che dica?")] string text, [Summary("canale", "In che canale vuoi che scriva?")] SocketTextChannel channel)
            {
                try
                {
                    await channel.SendMessageAsync(text);
                    await RespondAsync("Fatto", ephemeral: true);
                }
                catch
                {
                    await RespondAsync("C'è stato un problema, probabilmente il messaggio è troppo lungo", ephemeral: true);
                }
            }

            [SlashCommand("scrivi-in-privato", "Scrivo in privato qualcosa a chi volete")]
            public async Task WriteSomethingInDM([Summary("testo", "Cosa vuoi che dica?")] string text, [Summary("user", "Vuoi mandarlo in privato a qualcuno?")] SocketUser user)
            {
                try
                {
                    await user.SendMessageAsync(text);
                    await RespondAsync("Fatto", ephemeral: true);
                }
                catch
                {
                    await RespondAsync("C'è stato un problema, probabilmente il messaggio è troppo lungo", ephemeral: true);
                }
            }

            [SlashCommand("code", "Converto un messaggio sotto forma di codice")]
            public async Task ToCode()
            {
                await RespondWithModalAsync<CodeModal>("to-code");
            }

            public class CodeModal : IModal
            {
                public string Title => "Codice";

                [InputLabel("Cosa vuoi convertire?")]
                [ModalTextInput("text", TextInputStyle.Paragraph, placeholder: "Scrivi qui...")]
                public string Text { get; set; }
            }

            [ModalInteraction("to-code", true)]
            public async Task CodeModalResponse(CodeModal modal)
            {
                string text = modal.Text;
                if (text.Length >= 1500)
                {
                    await RespondAsync("```" + text.Substring(0, 1000) + "```");
                    await Context.Channel.SendMessageAsync("```" + text.Substring(1000) + "```");
                }
                else await RespondAsync("```" + text + "```");
            }

            [SlashCommand("links", "Vi mando dei link utili")]
            public async Task SendLinks([Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                Embed ShortcutsEmbed = DiscordData.CreateEmbed("Shorcuts");
                ShortcutsEmbed = ShortcutsEmbed.ToEmbedBuilder()
                    .AddField("Google", "[Vai al sito](https://www.google.com)", inline: true)
                    .AddField("Youtube", "[Vai al sito](https://youtube.com)", inline: true)
                    .AddField("Reddit", "[Vai al sito](https://www.reddit.com)", inline: true)
                    .AddField("Twitch", "[Vai al sito](http://www.twitch.tv)", inline: true)
                    .AddField("Instagram", "[Vai al sito](http://www.instagram.com)", inline: true)
                    .AddField("Twitter", "[Vai al sito](https://www.twitter.com)", inline: true)
                    .AddField("Pinterest", "[Vai al sito](https://www.pinterest.com)", inline: true)
                    .AddField("Deviantart", "[Vai al sito](https://www.deviantart.com)", inline: true)
                    .AddField("Artstation", "[Vai al sito](https://www.artstation.com)", inline: true)
                    .AddField("Speedtest", "[Vai al sito](https://www.speedtest.net/it)", inline: true)
                    .AddField("Google Drive", "[Vai al sito](https://drive.google.com)", inline: true)
                    .AddField("Gmail", "[Vai al sito](https://mail.google.com)", inline: true)
                    .Build();
                await RespondAsync(embed: ShortcutsEmbed, ephemeral: Ephemeral == EAnswer.Si);
            }
        }

        [Group("games", "videogames")]
        public class VideogamesGroup : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("igdb", "Cerco uno o più giochi su IGDB")]
            public async Task SearchGame([Summary("game", "Nome del gioco")] string game)
            {
                await DeferAsync();

                var gameEmbed = await getGameEmbedAsync(game, 0);
                if (gameEmbed == null)
                {
                    await FollowupAsync("Mi dispiace, non ho trovato il gioco che stavi cercando");
                    return;
                }

                var mex = await FollowupAsync(embed: gameEmbed);

                var messageComponent = new ComponentBuilder()
                    .WithButton("<", $"game-backward-{game}-0-{mex.Id}")
                    .WithButton(">", $"game-forward-{game}-0-{mex.Id}")
                    .Build();

                await mex.ModifyAsync(mex => mex.Components = messageComponent);
            }

            private async Task<Embed?> getGameEmbedAsync(string game, int count)
            {
                var igdb = new IGDBClient(DiscordData.IGDB.ClientID, DiscordData.IGDB.ClientSecret);
                string fields = "cover.image_id, game_engines.name, genres.name, involved_companies.company.name, name, platforms.name, rating, total_rating_count, release_dates.human, summary, themes.name, url";
                string query = $"fields {fields}; search \"{game}\"; where category != (1,2,5,6,7,12); limit 15;";
                var games = await igdb.QueryAsync<IGDB.Models.Game>(IGDBClient.Endpoints.Games, query: query);
                var sortedGames = games.ToList();
                sortedGames.Sort(delegate (IGDB.Models.Game a, IGDB.Models.Game b) {
                    if ((a.TotalRatingCount == null || a.TotalRatingCount == 0) && (b.TotalRatingCount == null || b.TotalRatingCount == 0))
                    {
                        if (Math.Abs(a.Name.Length - game.Length) <= Math.Abs(b.Name.Length - game.Length)) return -1;
                        else return 1;
                    }
                    if (a.TotalRatingCount == null || a.TotalRatingCount == 0) return 1;
                    if (b.TotalRatingCount == null || b.TotalRatingCount == 0) return -1;

                    if (a.TotalRatingCount >= b.TotalRatingCount) return -1;
                    else return 1;
                });

                if (sortedGames.Count == 0) return null;   
                
                if (count >= sortedGames.Count) count = 0;
                else if (count < 0) count = sortedGames.Count - 1;

                var foundGame = sortedGames[count];

                var coverID = foundGame.Cover != null ? foundGame.Cover.Value.ImageId : "nocover_qhhlj6";
                Embed GameEmbed = DiscordData.CreateEmbed(foundGame.Name, withTimeStamp: false);
                GameEmbed = GameEmbed.ToEmbedBuilder()
                    .WithDescription($"[Vai alla pagina IGDB]({foundGame.Url})")
                    .WithThumbnailUrl($"https://images.igdb.com/igdb/image/upload/t_cover_big/{coverID}.jpg")
                    .AddField("Risultato", $"{ count + 1} di { sortedGames.Count}")
                    .AddField("Rating", foundGame.Rating != null ? Math.Round(foundGame.Rating.Value, 2).ToString() : "N/A")
                    .AddField("Release Date", foundGame.ReleaseDates != null ? foundGame.ReleaseDates.Values.First().Human : "N/A")
                    .AddField("Themes", foundGame.Themes != null ? string.Join("\n", foundGame.Themes.Values.Take(3).Select(x => x.Name)) : "N/A")
                    .AddField("Genres", foundGame.Genres != null ? string.Join("\n", foundGame.Genres.Values.Take(3).Select(x => x.Name)) : "N/A")
                    .AddField("Game Engine", foundGame.GameEngines != null ? foundGame.GameEngines.Values.First().Name : "N/A")
                    .AddField("Developers", foundGame.InvolvedCompanies != null ? string.Join("\n", foundGame.InvolvedCompanies.Values.Take(3).Select(x => x.Company.Value.Name)) : "N/A")
                    .AddField("Platforms", foundGame.Platforms != null ? string.Join("\n", foundGame.Platforms.Values.Take(3).Select(x => x.Name)) : "N/A")
                    .WithFooter(foundGame.Summary != null ? (foundGame.Summary.Length <= 350 ? foundGame.Summary : foundGame.Summary.Substring(0, 350) + "...") : "No summary available")
                    .Build();
             
                return GameEmbed;
            }

            [ComponentInteraction("game-*-*-*-*", ignoreGroupNames: true)]
            public async Task GameButtonAnswer(string action, string game, int count, ulong messageID)
            {
                if (action == "forward") count += 1;
                else if(action == "backward") count -= 1;

                await DeferAsync();

                var gameEmbed = await getGameEmbedAsync(game, count);
                if (gameEmbed == null)
                {
                    await FollowupAsync("Mi dispiace, non ho trovato il gioco che stavi cercando");
                    return;
                }

                var counter = gameEmbed.Fields.Where(x => x.Name == "Risultato").First();
                var counterValues = counter.Value.Split(" di ");

                count = int.Parse(counterValues[0])-1;

                var messageComponent = new ComponentBuilder()
                    .WithButton("<", $"game-backward-{game}-{count}-{messageID}")
                    .WithButton(">", $"game-forward-{game}-{count}-{messageID}")
                    .Build();

               await Context.Channel.ModifyMessageAsync(messageID, message => { message.Embed = gameEmbed; message.Components = messageComponent; });
            }

            [SlashCommand("gaming-profile", "Profili RAWG, Steam e GOG")]
            public async Task ShowGamingProfile([Summary("user", "Di chi vuoi vedere il profilo?")] SocketUser user, [Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                if(user.Id == DiscordData.DiscordIDs.CortanaID)
                {
                    await RespondAsync("Purtroppo la conferma \"NON SONO UN ROBOT\" mi impedisce ogni volta di creare account di gioco", ephemeral: Ephemeral == EAnswer.Si);
                    return;
                }

                if(DiscordData.GamingProfile.ContainsKey(user.Id))
                {
                    var gamingEmbed = DiscordData.CreateEmbed("Gaming Profile", user: user);
                    gamingEmbed = gamingEmbed.ToEmbedBuilder()
                        .AddField("RAWG", DiscordData.GamingProfile[user.Id].RAWG == "N/A" ? "N/A" : $"[Vai al profilo](https://rawg.io/@{DiscordData.GamingProfile[user.Id].RAWG})")
                        .AddField("Steam", DiscordData.GamingProfile[user.Id].Steam == "N/A" ? "N/A" : $"[Vai al profilo](https://steamcommunity.com/id/{DiscordData.GamingProfile[user.Id].Steam}/)")
                        .AddField("GOG", DiscordData.GamingProfile[user.Id].GOG == "N/A" ? "N/A" : $"[Vai al profilo](https://www.gog.com/u/{DiscordData.GamingProfile[user.Id].GOG})")
                        .Build();
                    await RespondAsync(embed: gamingEmbed, ephemeral: Ephemeral == EAnswer.Si);
                }
                else
                {
                    if(user.Id == Context.User.Id) await RespondAsync("Non hai ancora registrato nessun profilo. Per aggiungerlo, usa il seguente comando: ```/games add-gaming-profile```", ephemeral: Ephemeral == EAnswer.Si);
                    else await RespondAsync("L'utente non ha ancora registrato nessun profilo", ephemeral: Ephemeral == EAnswer.Si);
                }
            }

            [SlashCommand("add-gaming-profile", "Aggiungi o modifica profili RAWG, Steam o GOG")]
            public async Task AddGamingProfile([Summary("account", "Di cosa vuoi aggiungere l'account?")] EGamingProfiles profile, [Summary("username", "Username del tuo account")] string username)
            {
                if (!DiscordData.GamingProfile.ContainsKey(Context.User.Id))
                {
                    DiscordData.GamingProfile.Add(Context.User.Id, new GamingProfileSet { GOG = "N/A", Steam = "N/A", RAWG = "N/A" }); 
                }

                switch(profile)
                {
                    case EGamingProfiles.RAWG:
                        DiscordData.GamingProfile[Context.User.Id].RAWG = username;
                        break;
                    case EGamingProfiles.Steam:
                        DiscordData.GamingProfile[Context.User.Id].Steam = username;
                        break;
                    case EGamingProfiles.GOG:
                        DiscordData.GamingProfile[Context.User.Id].GOG = username;
                        break;
                }

                DiscordData.UpdateGamingProfile();

                await RespondAsync("Profilo aggiunto con successo. Per visualizzarlo, usa il seguente comando: ```/games gaming-profile```");
            }

            [SlashCommand("remove-gaming-profile", "Rimuovi profilo RAWG, Steam o GOG")]
            public async Task RemoveGamingProfile([Summary("account", "Di cosa vuoi rimuovere l'account?")] EGamingProfiles profile)
            {
                if (!DiscordData.GamingProfile.ContainsKey(Context.User.Id))
                {
                    await RespondAsync("Non hai ancora registrato nessun profilo. Per aggiungerlo, usa il seguente comando: ```/games add-gaming-profile```");
                    return;
                }

                switch (profile)
                {
                    case EGamingProfiles.RAWG:
                        DiscordData.GamingProfile[Context.User.Id].RAWG = "N/A";
                        break;
                    case EGamingProfiles.Steam:
                        DiscordData.GamingProfile[Context.User.Id].Steam = "N/A";
                        break;
                    case EGamingProfiles.GOG:
                        DiscordData.GamingProfile[Context.User.Id].GOG = "N/A";
                        break;
                }

                DiscordData.UpdateGamingProfile();

                await RespondAsync("Profilo rimosso con successo. Per visualizzarlo usa il seguente comando: ```/games gaming-profile```");
            }
        }

        [Group("random", "Generatore di cose random")]
        public class RandomGroup : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("numero", "Genero un numero random")]
            public async Task RandomNumber([Summary("minimo", "Minimo [0 default]")] int min = 0, [Summary("massimo", "Massimo [100 default]")] int max = 100, [Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                string randomNumber = Convert.ToString(new Random().Next(min, max));
                Embed embed = DiscordData.CreateEmbed(title: randomNumber);
                await RespondAsync(embed: embed, ephemeral: Ephemeral == EAnswer.Si);
            }

            [SlashCommand("dado", "Lancio uno o più dadi")]
            public async Task Dice([Summary("dadi", "Numero di dadi [default 1]")] int dices = 1, [Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                string dicesResults = "";
                for (int i = 0; i < dices; i++) dicesResults += Convert.ToString(new Random().Next(1, 7)) + " ";
                Embed embed = DiscordData.CreateEmbed(title: dicesResults);
                await RespondAsync(embed: embed, ephemeral: Ephemeral == EAnswer.Si);
            }

            [SlashCommand("moneta", "Lancio una moneta")]
            public async Task Coin([Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                var list = new List<string>{"Testa","Croce"};
                int index = new Random().Next(list.Count);
                string result = list[index];
                Embed embed = DiscordData.CreateEmbed(title: result);
                await RespondAsync(embed: embed, ephemeral: Ephemeral == EAnswer.Si);
            }

            [SlashCommand("opzione", "Scelgo un'opzione tra quelle che mi date")]
            public async Task RandomChoice([Summary("opzioni", "Opzioni separate dallo spazio")] string options, [Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                string[] separatedList = options.Split(" ");
                int index = new Random().Next(separatedList.Length);
                string result = separatedList[index];
                Embed embed = DiscordData.CreateEmbed(title: result);
                await RespondAsync(embed: embed, ephemeral: Ephemeral == EAnswer.Si);
            }


            [SlashCommand("user", "Scelgo uno di voi")]
            public async Task OneOfYou([Summary("tutti", "Anche chi non è in chat vocale")] EAnswer all = EAnswer.No, [Summary("cortana", "Anche io?")] EAnswer cortana = EAnswer.No, [Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                var Users = new List<SocketGuildUser>();
                var AvailableUsers = Context.Guild.Users;
                if (all == EAnswer.No)
                {
                    foreach (var channel in Context.Guild.VoiceChannels)
                    {
                        if (channel.ConnectedUsers.Contains(Context.User)) AvailableUsers = channel.ConnectedUsers;
                    }
                }
                foreach (var user in AvailableUsers)
                {
                    if (!user.IsBot || (user.IsBot && user.Id == DiscordData.DiscordIDs.CortanaID && cortana == EAnswer.Si)) Users.Add(user);
                }

                SocketGuildUser ChosenUser = Users[new Random().Next(0, Users.Count)];
                await RespondAsync($"Ho scelto {ChosenUser.Mention}", ephemeral: Ephemeral == EAnswer.Si);
            }

            [SlashCommand("lane", "Vi dico in che lane giocare")]
            public async Task Lane([Summary("user", "A chi è rivolto?")] SocketUser? User = null, [Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                if (User == null) User = Context.User;
                string[] lanes = new[] { "Top", "Jungle", "Mid", "ADC", "Support" };
                int randomIndex = new Random().Next(0, lanes.Length);
                await RespondAsync($"{User.Mention} vai *{lanes[randomIndex]}*", ephemeral: Ephemeral == EAnswer.Si);
            }
        }

        [Group("unipi", "Università")]
        public class UniversityGroup : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("link", "Siti UNIPI")]
            public async Task UnipiSites([Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                Embed embed = DiscordData.CreateEmbed("Siti UNIPI", user: Context.User);
                EmbedBuilder embed_builder = embed.ToEmbedBuilder();
                embed_builder.AddField("Home", "[Vai al sito](https://agendadidattica.unipi.it/Prod)");
                embed_builder.AddField("Laurea in Informatica", "[Vai al sito](https://didattica.di.unipi.it/laurea-in-informatica/)");
                embed_builder.AddField("Piano di Studi", "[Vai al sito](https://didattica.di.unipi.it/laurea-in-informatica/piani-di-studio-2/)");
                embed_builder.AddField("Iscrizione Esami", "[Vai al sito](https://esami.unipi.it/archivioiscrizioni.php)");             

                Dictionary<string, ulong> ids = new Dictionary<string, ulong>()
                {
                    { "matteo", 468399905023721481 },
                    { "samuele", 648939655579828226 },
                    { "danu", 306402234135085067 }
                };

                if (Context.User.Id == ids["matteo"])
                {
                    embed_builder.AddField("Matricola", "658274");
                    embed_builder.AddField("Email", "m.cherubini6@studenti.unipi.it");
                }
                else if (Context.User.Id == ids["samuele"])
                {
                    embed_builder.AddField("Matricola", "658988");
                    embed_builder.AddField("Email", "s.baffo@studenti.unipi.it");
                }
                else if (Context.User.Id == ids["danu"])
                {
                    embed_builder.AddField("Matricola", "658992");
                    embed_builder.AddField("Email", "v.nitu@studenti.unipi.it");
                }
                else
                {
                    await RespondAsync("Mi dispiace, non ho dati su di te per questa università", ephemeral: Ephemeral == EAnswer.Si);
                    return;
                }

                await RespondAsync(embed: embed_builder.Build(), ephemeral: Ephemeral == EAnswer.Si);
            }

            [SlashCommand("courses", "Lezioni UNIPI")]
            public async Task UnipiLessons([Summary("ephemeral", "Voi vederlo solo tu?")] EAnswer Ephemeral = EAnswer.No)
            {
                Embed embed = DiscordData.CreateEmbed("Lezioni UNIPI", user: Context.User);
                EmbedBuilder embed_builder = embed.ToEmbedBuilder();
                embed_builder.WithDescription("[Calendario](https://cdn.discordapp.com/attachments/912356013108256858/1137669285251121213/Orario_settimanale_-_Orario_settimanale.jpg?ex=65169016&is=65153e96&hm=0055311a24931ed11a8db1b3c462b8be1264a4371f5aac6ebdd9292c995fd762&)");
                embed_builder.AddField("Architettura e Sistemi Operativi", "[Classroom](https://classroom.google.com/u/2/c/NjIyMjg0ODk2OTM2)\n[Teams](https://teams.microsoft.com/l/team/19%3ad78R1wqZ_8OCtKvUSqGAhRLVm_kbb0FTD6AZIdc2eus1%40thread.tacv2/conversations?groupId=956555d6-95af-4d8c-ae35-33549b96e038&tenantId=c7456b31-a220-47f5-be52-473828670aa1)");
                embed_builder.AddField("Laboratorio II", "[Classroom](https://classroom.google.com/u/2/c/NjIyMjg2NTcxMzE5)\n[SAI EVO](https://evo.di.unipi.it/student/courses/36/practices)\n[Teams](https://teams.microsoft.com/l/team/19%3a2yvkDN_uhbuK3sFWnzfwFZHOauEM86hzRBHAIEhBTz41%40thread.tacv2/conversations?groupId=613aa738-d3b6-49e8-8d39-9cfda135d653&tenantId=c7456b31-a220-47f5-be52-473828670aa1)");
                embed_builder.AddField("Ricerca Operativa", "[Moodle](https://elearning.di.unipi.it/course/view.php?id=495)\n[Teams](https://teams.microsoft.com/l/team/19%3amQrlWLskRtrOerZ0c5t_XRf8-DXjjnsnnmZ0Q892QZQ1%40thread.tacv2/conversations?groupId=7efd8a47-9ad9-4eb2-9fec-cb079183c650&tenantId=c7456b31-a220-47f5-be52-473828670aa1)");
                embed_builder.AddField("Paradigmi di Programmazione", "[Moodle A](https://elearning.di.unipi.it/course/view.php?id=534)\n[Moodle B](https://elearning.di.unipi.it/course/view.php?id=540)\n[Moodle 2022/2023](https://elearning.di.unipi.it/course/view.php?id=309)");
                embed_builder.WithFooter("Corso A");

                await RespondAsync(embed: embed_builder.Build(), ephemeral: Ephemeral == EAnswer.Si);
            }
        }

        [Group("gestione", "Comandi gestione server")]
        public class ManageGroup : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("banned-words", "Vi mostro le parole bannate in questo server")]
            public async Task ShowBannedWords()
            {
                if(DiscordData.GuildSettings[Context.Guild.Id].BannedWords.Count == 0)
                {
                    await RespondAsync("Non ci sono parole vietate in questo server");
                    return;
                }
                string bannedWordsList = "Ecco le parole bannate di questo server:\n```\n";
                foreach(var word in DiscordData.GuildSettings[Context.Guild.Id].BannedWords)
                {
                    bannedWordsList += word + "\n";
                }
                bannedWordsList += "```";
                await RespondAsync(bannedWordsList);
            }

            [SlashCommand("modify-banned-words", "Aggiungo o rimuovo parole bannate da questo server")]
            public async Task ShowBannedWords([Summary("action", "Cosa vuoi fare?")] EAction action, [Summary("word", "Parola bannata")] string word)
            {
                word = word.ToLower();
                switch(action)
                {
                    case EAction.Crea:
                        if(DiscordData.GuildSettings[Context.Guild.Id].BannedWords.Contains(word))
                        {
                            await RespondAsync("Questa parola è già presente tra quelle bannate in questo server");
                            return;
                        }
                        DiscordData.GuildSettings[Context.Guild.Id].BannedWords.Add(word);
                        await RespondAsync("Parola aggiunta con successo! Usa il seguente comando per visualizzare la nuova lista: ```/gestione banned-words```");                   
                        break;
                    case EAction.Elimina:
                        if(!DiscordData.GuildSettings[Context.Guild.Id].BannedWords.Contains(word))
                        {
                            await RespondAsync("Questa parola non è presente tra quelle bannate in questo server");
                            return;
                        }
                        DiscordData.GuildSettings[Context.Guild.Id].BannedWords.Remove(word);
                        await RespondAsync("Parola rimossa con successo! Usa il seguente comando per visualizzare la nuova lista: ```/gestione banned-words```");
                        break;
                }
                DiscordData.UpdateSettings();
            }

            [SlashCommand("kick", "Kicko un utente dal server")]
            public async Task KickMember([Summary("user", "Chi vuoi kickare?")] SocketGuildUser user, [Summary("motivazione", "Per quale motivo?")] string reason = "Motivazione non specificata")
            {
                if (user.Id == DiscordData.DiscordIDs.ChiefID) await RespondAsync("Non farei mai una cosa simile");
                else if (user.Id == DiscordData.DiscordIDs.CortanaID) await RespondAsync("Divertente");
                else
                {
                    await user.KickAsync(reason: reason);
                    await RespondAsync("Utente kickato");
                }
            }

            [SlashCommand("ban", "Banno un utente dal server")]
            public async Task BanMember([Summary("user", "Chi vuoi bannare?")] SocketGuildUser user, [Summary("motivazione", "Per quale motivo?")] string reason = "Motivazione non specificata")
            {
                if (user.Id == DiscordData.DiscordIDs.ChiefID) await RespondAsync("Non farei mai una cosa simile");
                else if (user.Id == DiscordData.DiscordIDs.CortanaID) await RespondAsync("Divertente");
                else
                {
                    await user.BanAsync(reason: reason);
                    await RespondAsync("Utente bannato");
                }
            }

            [SlashCommand("imposta-timeout", "Timeouto un utente dal server")]
            public async Task SetTimeoutMember([Summary("user", "Chi vuoi timeoutare?")] SocketGuildUser user, [Summary("tempo", "Quanti minuti deve durare il timeout? [Default: 10]")] double timeout = 10)
            {
                if (user.Id == DiscordData.DiscordIDs.ChiefID) await RespondAsync("Non farei mai una cosa simile");
                else if (user.Id == DiscordData.DiscordIDs.CortanaID) await RespondAsync("Divertente");
                else
                {
                    await user.SetTimeOutAsync(TimeSpan.FromMinutes(timeout));
                    await RespondAsync($"Utente timeoutato per {timeout} minuti");
                }
            }

            [SlashCommand("rimuovi-timeout", "Rimuovo il timeout di un utente del server")]
            public async Task RemoveTimeoutMember([Summary("user", "Di chi vuoi rimuovere il timeout?")] SocketGuildUser user)
            {
                await user.RemoveTimeOutAsync();
                await RespondAsync("Timeout rimosso");
            }
        }
    }
}
