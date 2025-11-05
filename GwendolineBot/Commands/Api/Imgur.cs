using System;
using System.Linq;
using System.Net.Http;
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
    public class Imgur : ModuleBase<SocketCommandContext>
    {
        private static HttpClient client = new HttpClient();

        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(Imgur));

        private readonly string ImgurSearchUrl = "https://api.imgur.com/3/gallery/search/";
        private readonly string ClientId = Program.AppConfig["API:ImgurClientId"];
        private readonly List<string> BlacklistWords = Program.AppConfig.GetSection("BlacklistWords").AsEnumerable().Where(x => x.Value != null).Select(x => x.Value).ToList();
        private readonly List<string> FunnyTags = Program.AppConfig.GetSection("FunnyImgurTags").AsEnumerable().Where(x => x.Value != null).Select(x => x.Value).ToList();

        #region Commands
        [Command("Image"),Alias("img")]
        public async Task GetImgurImage([Remainder]string searchTerm)
        {
            string parameters = $"?q=title:{searchTerm}&ext:jpg";
            await ImgurSearch(searchTerm, parameters, "image/jpeg");
        }

        [Command("FunnyImage"), Alias("fimg")]
        public async Task GetFunnyImgurImage(string searchTerm, bool random = false)
        {
            string parameters = $"?q=title:{searchTerm}&ext:jpg";
            await ImgurSearch(searchTerm, parameters, "image/jpeg", true, random);
        }

        [Command("RandomImage"), Alias("rimg")]
        public async Task GetRandomImage(string searchTerm, bool funny = false)
        {
            string parameters = $"?q=title:{searchTerm}&ext:jpg";
            await ImgurSearch(searchTerm, parameters, "image/jpeg", funny, true);
        }

        [Command("gif")]
        [Summary("Searches for gifs with given term.")]
        public async Task GetImgurGif([Remainder]string searchTerm)
        {
            string parameters = $"?q=title:{searchTerm}&ext:gif";
            await ImgurSearch(searchTerm, parameters, "image/gif");
        }

        [Command("funnygif"), Alias("fgif")]
        public async Task GetFunnyImgurGif(string searchTerm, bool random = false)
        {
            string parameters = $"?q=title:{searchTerm}&ext:gif";
            await ImgurSearch(searchTerm, parameters, "image/gif", true, random);
        }

        [Command("RandomGif"), Alias("rgif")]
        public async Task GetRandomGif(string searchTerm, bool funny = false)
        {
            string parameters = $"?q=title:{searchTerm}&ext:gif";
            await ImgurSearch(searchTerm, parameters, "image/gif", funny, true);
        }
        #endregion

        #region Private methods
        private async Task ImgurSearch(string searchTerm, string parameters, string searchType, bool isFunny = false, bool randomSearch = false)
        {
            if(BlacklistWords.Any(x => x.ToLower() == searchTerm.ToLower()))
            {
                _Log.Warn($"User:{Context.User} tried to search for the a blacklisted word {searchTerm}");

                Helper.StandardEmbed("Imgur", "Error", $"Nice try <@{Context.User.Id}>!", Context);

                return;
            }

            HttpResponseMessage response = await GetResponse(ImgurSearchUrl, parameters);

            if (response.IsSuccessStatusCode)
            {
                string resp = await response.Content.ReadAsStringAsync();

                ImgurData data = JsonConvert.DeserializeObject<ImgurData>(resp);

                _Log.Info($"A successful search for '{searchTerm}' was made and returned {data.Albums.Count} hits");

                List<ImgurData.ImgurResult> list = new List<ImgurData.ImgurResult>();

                if (isFunny)
                {
                    list.AddRange(data.Albums.Where(x => x.IsAlbum == false && x.Type == searchType && x.Tags.Any(tag => CheckImgurTags(tag))).Select(x => x));

                    list.AddRange(data.Albums.Where(x => x.IsAlbum == true && x.Tags.Any(tag => CheckImgurTags(tag)) && x.Images.Any(image => image.Type == searchType)).SelectMany(x => x.Images).ToList());
                }
                else
                {
                    list.AddRange(data.Albums.Where(x => x.IsAlbum == false && x.Type == searchType).Select(x => x));

                    list.AddRange(data.Albums.Where(x => x.IsAlbum == true && x.Images.Any(image => image.Type == searchType )).SelectMany(x => x.Images).ToList());
                }                

                if (list.Count > 0)
                {
                    int number = 0;

                    if (randomSearch)
                    {
                        number = Helper.RandomNumber(list.Count);                       
                    }

                    if (searchType == "image/gif")
                    {
                        Context.Channel.SendMessageAsync(list[number].Link, false);
                    }
                    else
                    {
                        Context.Channel.SendMessageAsync("", false, StandardImageEmbed(list[number].Title, list[number].Link));
                    }
                }
                else
                {
                    Helper.StandardEmbed("Imgur", "Imgur", "No Image found", Context);
                }
            }
            else
            {
                _Log.Error($"Failed to make and image search with term {searchTerm}");

                Helper.StandardEmbed("Imgur", "Imgur", "Error while searching for image", Context);
            }
        }        

        private async Task<HttpResponseMessage> GetResponse(string Url, string parameters = "")
        {            
            client.BaseAddress = new Uri(Url);

            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Add("Authorization", ClientId);

            return client.GetAsync(parameters).Result;
        }

        private Embed StandardImageEmbed(string Title,string ImageUrl)
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithAuthor("Imgur");
            Embed.WithColor(Color.Green);
            Embed.WithDescription(Title);
            Embed.WithImageUrl(ImageUrl);            

            return Embed.Build();
        }

        private bool CheckImgurTags(ImgurData.ImgurTag tag)
        {
            return FunnyTags.Any(x => x == tag.Name.ToLower());
        }
        #endregion

        private class ImgurData
        {
            [JsonProperty("data")]
            internal List<ImgurResult> Albums { get; set; }

            internal class ImgurResult
            {
                [JsonProperty("id")]
                public string Id { get; set; }

                [JsonProperty("title")]
                public string Title { get; set; }

                [JsonProperty("type")]
                public string Type { get; set; }

                [JsonProperty("link")]
                public string Link { get; set; }

                [JsonProperty("is_album")]
                public bool IsAlbum { get; set; }

                [JsonProperty("tags")]
                public List<ImgurTag> Tags { get; set; }

                [JsonProperty("images")]
                public List<ImgurResult> Images { get; set; }
            }

            internal class ImgurTag
            {
                [JsonProperty("name")]
                public string Name { get; set; }
            }
        }        
    }
}
