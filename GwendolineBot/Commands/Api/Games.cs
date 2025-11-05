using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using GwendolineBot;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;


namespace GwendolineBot.Commands.Api
{
    public class GamesApi : ModuleBase<SocketCommandContext>
    {
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(GamesApi));

        private static readonly string _searchUrl = "https://api.igdb.com/v4";
        private static readonly string _clientKey = Program.AppConfig["API:Twitch:client"];
        private static readonly string _secret = Program.AppConfig["API:Twitch:secret"];

        private string _token = "";
        private PeriodicTimer _apiTimer;

        private readonly string[] _GameFields = new string[]
        {
            "id",
            "name",
            "summary",
            "rating",
            "url",
            "cover.url",
            "platforms.name",
            "platforms.platform_logo.url",
            "genres.name",
            "keywords.name",
            "game_modes.name"
        };

        private readonly string[] _PlatformFields = new string[]
        {
            "id",
            "name",
            "summary",
            "url",
            "generation",
            "platform_logo.url",
            "websites.url"
        };

        public GamesApi()
        {
            if (String.IsNullOrEmpty(_token))
            {
                GetToken();
            }
        }

        #region Commands
        #region Games
        [Command("GameInfo"), Alias("game")]
        [Summary("Searches for the most relevant game for the searchterm and displays detailed information about it.")]
        public async Task GameInfo([Remainder] string name)
        {
            string parameters = $"search \"{name}\";fields {String.Join(',', _GameFields)};where version_parent = null; limit 1;";

            await GameApiSearch(parameters, true);
        }

        [Command("GameInfo"), Alias("game")]
        [Summary("Displays detailed information about a game.")]
        public async Task GameInfo(int id)
        {
            string parameters = $"fields {String.Join(',', _GameFields)};where id = {id} & version_parent = null; limit 1;";

            await GameApiSearch(parameters, true);
        }

        [Command("GameSearch"), Alias("games")]
        [Summary("Searches for games with the most relevant name to the searchterm. Add 'true' to search for only multiplayer games.")]
        public async Task GameSearch([Remainder] string gameName)
        {
            string parameters = $"search \"{gameName}\"; fields id, name, cover.url, platforms.name;where version_parent = null; limit 50;";

            await GameApiSearch(parameters, false);
        }

        [Command("GameSearch"), Alias("games")]
        [Summary("Searches for a game on the chosen platform. (Limit to 50)")]
        public async Task GameSearch(int platformId, string gameName)
        {
            string parameters = $"search \"{gameName}\";fields id,name;where platforms = {platformId}; limit 60;";

            await GameApiSearch(parameters, false);
        }

        [Command("GameSearch"), Alias("games")]
        [Summary("Searches for a game on the chosen platform. (Limit to 50)")]
        public async Task GameSearch(string gameName, string platformName)
        {
            int platformId = GetPlatformId($"search \"{platformName}\"; fields id,name; limit 50;").GetAwaiter().GetResult();

            if (platformId > 0)
            {
                string parameters = $"search \"{gameName}\";fields id,name;where platforms = {platformId}; limit 60;";

                await GameApiSearch(parameters, false);
            }
            else
            {
                Helper.StandardEmbed("Games search", "Games", $"No platform found with '{platformName}'", Context);
            }
        }

        [Command("PlatformGames"), Alias("listgames")]
        [Summary("Displays all the games for a platform with id and name. Add 'true' to search for only multiplayer games. (Limit to 50)")]
        public async Task ListPlatformGames(int platformId, bool multi = false)
        {
            string parameters;

            if(multi)
            {
                parameters = $"fields id,name;where platforms = {platformId} & game_modes != 1; limit 50;";
            }
            else
            {
                parameters = $"fields id,name;where platforms = {platformId}; limit 50;";
            }

            await GameApiSearch(parameters, false);
        }        

        [Command("PlatformGames"), Alias("listgames")]
        [Summary("Displays all the games with the relevant name on a platform. Add 'true' to search for only multiplayer games. (Limit to 50)")]
        public async Task ListPlatformGames(string platformName, bool multi = false)
        {
            int platformId = GetPlatformId($"search \"{platformName}\"; fields id,name; limit 1;").GetAwaiter().GetResult();

            if (platformId > 0)
            {
                await ListPlatformGames(platformId, true);
            }
            else
            {
                Helper.StandardEmbed("Games search", "Games", $"No platform found with '{platformName}'", Context);
            }
        }        

        #endregion
        #region Platforms
        [Command("Platforms"), Alias("allplatforms")]
        [Summary("Displays all the platforms with id and name.")]
        public async Task GetAllPlatforms()
        {
            string parameters = $"fields id,name; limit 60;";

            await PlatformSearch(parameters, false);
        }
        [Command("Platforms"), Alias("allplatforms","platformsearch")]
        [Summary("Searches all the platforms to find the most relevant to the searchterm.")]
        public async Task GetAllPlatforms([Remainder] string name)
        {
            string parameters = $"search \"{name}\";fields id,name;";

            await PlatformSearch(parameters, false);
        }

        [Command("PlatformInfo"), Alias("platinfo","platform")]
        [Summary("Displays detailed information about a platform found by id.")]
        public async Task GetPlatform(int id)
        {
            string parameters = $"fields {String.Join(',', _PlatformFields)}; where id = {id};";

            await PlatformSearch(parameters, true);
        }

        [Command("PlatformInfo"), Alias("platinfo","platform")]
        [Summary("Displays detailed information about a platform, found by name.")]
        public async Task GetPlatform([Remainder] string name)
        {
            string parameters = $"search \"{name}\"; fields {String.Join(',', _PlatformFields)}; limit 1;";

            await PlatformSearch(parameters, true);
        }
        #endregion
        #endregion

        #region Private methods
        private async Task<int> GetPlatformId(string parameters)
        {
            HttpResponseMessage response = await GetResponse(_searchUrl + "/platforms", parameters);

            if (response.IsSuccessStatusCode)
            {
                string resp = await response.Content.ReadAsStringAsync();

                List<Platform> platformList = JsonConvert.DeserializeObject<List<Platform>>(resp);

                if (platformList.Count > 0)
                {
                    return platformList[0].Id;
                }
            }

            return -1;
        }

        private async Task PlatformSearch(string parameters, bool infoSearch)
        {            
            HttpResponseMessage response = await GetResponse(_searchUrl + "/platforms", parameters);

            if (response.IsSuccessStatusCode)
            {
                _Log.Info($"Successful search for platforms with term ' {parameters} '");

                string resp = await response.Content.ReadAsStringAsync();
                List<Platform> platformList = JsonConvert.DeserializeObject<List<Platform>>(resp);

                if (platformList.Count == 0)
                {
                    Helper.StandardEmbed("Games", "Games", $"Could not find any platform with that name", Context);
                }                

                if (infoSearch && platformList[0] != null)
                {
                    Platform plat = platformList[0];

                    List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Generation")
                        .WithValue(plat.Generation));
                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Url")
                        .WithValue($"[{plat.Name}]({plat.Url})"));                    

                    string desc = plat.Summary ?? "";

                    if(plat.Websites != null)
                    {
                        fields.Add(new EmbedFieldBuilder()
                        .WithName("Websites")
                        .WithValue(string.Join(" \r\n ", plat.Websites.Select(w => w.Url))));
                    }

                    Helper.StandardEmbed(
                        plat.Name, 
                        "Games", 
                        desc, 
                        Context, 
                        plat.Logo != null ? "http:" + plat.Logo.Url : "", 
                        fields);
                }
                else if (platformList.Count > 0)
                {
                    List<string> embedList = new List<string>();

                    foreach (Platform plat in platformList)
                    {
                        embedList.Add(plat.Id + " : " + plat.Name);
                    }

                    Helper.StandardEmbedList("Platforms", "Games", embedList, Context);
                }
            }
            else
            {
                _Log.Warn($"Failed to search with term ' {parameters} ' return with error message {response.Content}");
                Helper.StandardEmbed("Games", "Error", "Error from api", Context);
            }
        }

        private async Task GameApiSearch(string parameters, bool infoSearch)
        {
            HttpResponseMessage response = await GetResponse(_searchUrl + "/games", parameters);

            if (response.IsSuccessStatusCode)
            {
                _Log.Info($"Successful search for games with term ' {parameters} '");

                string resp = await response.Content.ReadAsStringAsync();
                List<Game> gamesList = JsonConvert.DeserializeObject<List<Game>>(resp);

                if (gamesList.Count == 0)
                {
                    Helper.StandardEmbed("Games", "Games", $"Could not find any game with that name", Context);
                }                

                if(infoSearch && gamesList[0] != null)
                {
                    Game game = gamesList[0];

                    List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Rating")
                        .WithValue(Math.Round(game.Rating)));

                    if (game.GameModes != null)
                    {
                        fields.Add(new EmbedFieldBuilder()
                            .WithName("Game Modes")
                            .WithValue(String.Join(',', game.GameModes.Select(g => g.Name))));
                    }

                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Platforms")
                        .WithValue(String.Join(',', game.Platforms.Select(p => p.Name))));

                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Genres")
                        .WithValue(String.Join(',', game.Genres.Select(g => g.Name))));

                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Link")
                        .WithValue(game.Url));

                    string desc;

                    if(game.Summary.Length > 1000)
                    {
                        desc = game.Summary.Substring(1000) + "...";
                    }
                    else
                    {
                        desc = $"{game.Summary}";
                    }
                    

                    Helper.StandardEmbed(game.Name, "Games", desc, Context, "http:" + game.Cover.Url, fields);
                }
                else if(gamesList.Count > 0)
                {
                    List<string> embedList = new List<string>();

                    foreach(Game game in gamesList)
                    {
                        embedList.Add(game.Id + " : " + game.Name);
                    }

                    Helper.StandardEmbedList("Games found", "Games", embedList, Context);
                }
                else
                {
                    Helper.StandardEmbed("Games search", "Games", $"No game found.", Context);
                }
            }
            else
            {
                _Log.Warn($"Failed to search with term ' {parameters} ' return with error message {response.Content}");
            }
        }

        private async Task GetToken()
        {
            HttpClient tokClient = new HttpClient();
            tokClient.BaseAddress = new Uri("https://id.twitch.tv/oauth2/token?client_id=" + _clientKey + "&client_secret=" + _secret + "&grant_type=client_credentials");
            tokClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            string resp = await tokClient.PostAsync(tokClient.BaseAddress, null).Result.Content.ReadAsStringAsync();
            TokenResponse token = JsonConvert.DeserializeObject<TokenResponse>(resp);

            _token = token.Token;
            _apiTimer = new PeriodicTimer(TimeSpan.FromSeconds(token.Expires_In));

            RefreshToken(_apiTimer);
        }

        private async Task RefreshToken(PeriodicTimer timer)
        {
            while (await _apiTimer.WaitForNextTickAsync())
            {
                await GetToken();
                _apiTimer.Dispose();
            }
        }

        private async Task<HttpResponseMessage> GetResponse(string url, string parameters = "")
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Add("Client-ID", _clientKey);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);

            var content = new StringContent(parameters);

            return client.PostAsync(url,content).Result;
        }
        #endregion

        #region Classes & Enums
        private class BaseApiClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Summary { get; set; }
            public string Url { get; set; }
        }

        private class TokenResponse
        {
            [JsonProperty("access_token")]
            public string Token { get; set; }
            [JsonProperty("expires_in")]
            public int Expires_In { get; set; }
        }
        private class Game : BaseApiClass
        {
            [JsonProperty("rating")]
            public double Rating { get; set; }
            public double Popularity { get; set; }
            [JsonProperty("cover")]
            public BaseApiClass Cover { get; set; }
            public Platform[] Platforms { get; set; }
            [JsonProperty("time_to_beat")]
            public TimeToBeat[] TimeToBeat { get; set; }
            [JsonProperty("game_modes")]
            public BaseApiClass[] GameModes { get; set; }
            [JsonProperty("genres")]
            public BaseApiClass[] Genres { get; set; }
            [JsonProperty("keywords")]
            public BaseApiClass[] Keywords { get; set; }
        }
        private class Platform : BaseApiClass
        {
            public int Generation { get; set; }
            [JsonProperty("platform_logo")]
            public BaseApiClass Logo { get; set; }
            [JsonProperty("websites")]
            public BaseApiClass[] Websites { get; set; }
        }
        private class TimeToBeat
        {
            public int Normally { get; set; }
            public int Hastly { get; set; }
            public int Completely { get; set; }
        }
        #endregion
    }
}
