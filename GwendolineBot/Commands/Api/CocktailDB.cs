using System;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;

using Newtonsoft.Json;

using GwendolineBot;

using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;


namespace GwendolineBot.Commands.Api
{
    public class CocktailDB : ModuleBase<SocketCommandContext>
    {
        private static HttpClient client = new HttpClient();

        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(CocktailDB));

        private static readonly string baseUrl = "https://www.thecocktaildb.com/api/json/v1/1/";
        private static readonly string searchUrl = baseUrl + "search.php";
        private static readonly string randomUrl = baseUrl + "random.php";
        private static readonly string ounces = 
            @"            
            2 shots(2 oz) = 60ml
            1¾ shots(1¾ oz) = 52.5ml
            1 2 / 3 shots(1 2 / 3 oz) = 50ml
            1½ shot(1½ oz) = 45ml
            1⅓ shot(1⅓ oz) = 40ml
            1¼ shot(1¼ oz) = 37.5ml
            1 shot(1 oz) = 30ml(standard spirit measure in US / Can / Aust / NZ / Asia)
            5 / 6 shot(5 / 6 oz) = 25ml(standard spirit measure in UK)
            3 / 4 shot(3 / 4 oz) = 22.5ml
            2 / 3 shot(2 / 3 oz) = 20ml(standard spirit measure in Europe)
            1 / 2 shot(1 / 2 oz) = 15ml
            ⅓ shot(1 / 3 oz) = 10ml
            ¼ shot(¼ oz) = 7.5ml
            1 / 6 shot(1 / 6 oz) = 5ml(approx.one barspoon)
            ⅛ shot(⅛ oz) = 3.75ml(slightly under - filled barspoon)
            1 / 12 shot(1 / 12oz) = 2.5ml(approx.half - filled barspoon)
            1 / 24 shot(1 / 24oz) = 1.25ml(approx.quarter barspoon)
            1 dash(1 / 32oz) = 0.94ml";

        #region Commands
        [Command("Drink")]
        [Summary("Search for a cocktail or drink by name")]
        public async Task GetDrinkByName([Remainder]string searchTerm)
        {
            string url = searchUrl + $"?s={searchTerm}";

            CocktailSearch(url, searchTerm);
        }

        [Command("DrinkId"), Alias("DrinkInfo")]
        [Summary("Returns the information about a drink with provided Id")]
        public async Task GetDrinkById([Remainder]int id)
        {
            string url = baseUrl + $"lookup.php?i={id}";

            CocktailSearch(url, id.ToString(), false);
        }

        [Command("DrinkIngredient"), Alias("drinkingr")]
        [Summary("Search for a cocktail or drink by ingredient")]
        public async Task GetDrinkByIngredient([Remainder]string searchTerm)
        {
            string url = baseUrl + $"filter.php?i={searchTerm}";

            CocktailSearch(url, searchTerm, true, true);
        }

        [Command("DrinkRandom"), Alias("randomdrink", "rdrink")]
        [Summary("Get a random drink")]
        public async Task GetRandomDrink()
        {
            string url = randomUrl;

            CocktailSearch(url, "Random", false);
        }

        [Command("IngredientInfo"), Alias("ingredient", "inginfo")]
        [Summary("Search for a cocktail or drink by ingredient")]
        public async Task GetIngredientInfo([Remainder]string searchTerm)
        {
            string url = searchUrl + $"filter.php?i={searchTerm}";

            IngredientSearch(url, searchTerm);
        }

        [Command("DrinkMeasurements"), Alias("drinkhelp","drinkmeasure","drinkounces","ounces")]
        [Summary("Returns a conversion list for ounces")]
        public async Task DrinkMeasurements()
        {
            Helper.StandardEmbed("Help with Ounces", "CocktailDB", ounces, Context);
        }
        #endregion
        #region Private methods
        private async Task IngredientSearch(string url, string searchTerm)
        {
            HttpResponseMessage response = await GetResponse(url);

            if (response.IsSuccessStatusCode)
            {
                string resp = await HandleResponse(url, searchTerm, response);

                IngredientReponse parent = JsonConvert.DeserializeObject<IngredientReponse>(resp);

                if (parent.Ingredients != null)
                {
                    _Log.Debug($"A successful search for ingredient '{searchTerm}' was made and returned {parent.Ingredients.Count} hits");

                    Ingredient ing = JsonConvert.DeserializeObject<Ingredient>(parent.Ingredients[0].ToString());

                    Context.Channel.SendMessageAsync("", false, ing.ToEmbed());
                }
                else
                {
                    Helper.StandardEmbed("Drinks", "CocktailDB", "No ingredient found", Context);
                }
            }
        }
        private async Task CocktailSearch(string url, string searchTerm, bool isList = true, bool byIngredient = false)
        {
            HttpResponseMessage response = await GetResponse(url);

            if (response.IsSuccessStatusCode)
            {
                string resp = await HandleResponse(url, searchTerm, response);

                DrinkReponse parent = JsonConvert.DeserializeObject<DrinkReponse>(resp);

                if (parent.Drinks != null)
                {
                    List<Drink> _drinkList = new List<Drink>();

                    _Log.Debug($"A successful search for drink search for {searchTerm} was made and returned {parent.Drinks.Count} hits");

                    foreach (object obj in parent.Drinks)
                    {
                        Drink d = new Drink(obj);

                        _drinkList.Add(d);
                    }

                    if (_drinkList.Count == 0)
                    {
                        Helper.StandardEmbed("Drinks", "CocktailDB", "No drinks found", Context);
                    }
                    else if (!byIngredient)
                    {
                        if (_drinkList.Count > 1)
                        {
                            Helper.StandardEmbedList("Drinks", "CocktailDB", _drinkList.Select(d => $"({d.Id}) {d.Name}").ToList(), Context, "Check for info with !DrinkId");
                        }
                        else
                        {
                            Context.Channel.SendMessageAsync("", false, _drinkList[0].ToEmbed());

                            _drinkList.Clear();
                        }
                    }
                    else
                    {
                        Helper.StandardEmbedList("Drinks", "CocktailDB", _drinkList.Select(d => $"({d.Id}) {d.Name}").Take(20).ToList(), Context, "Check for info with !DrinkId");
                        _drinkList.Clear();
                    }
                }
                else
                {
                    Helper.StandardEmbed("Drinks", "CocktailDB", "No drinks found", Context);
                }
            }
        }

        private async Task<string> HandleResponse(string url, string searchTerm, HttpResponseMessage response)
        {
            string resp = await response.Content.ReadAsStringAsync();

            if (resp == "")
            {
                _Log.Warn($"A drink search returned null while searching for {searchTerm} at {url}");

                Helper.StandardEmbed("Drinks", "CocktailDB", "No drinks found", Context);
            }

            return resp;
        }

        private async Task<HttpResponseMessage> GetResponse(string url)
        {            
            client.BaseAddress = new Uri(url);

            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            return client.GetAsync(url).Result;
        }
        #endregion
        #region Help classes
        private class DrinkReponse
        {
            [JsonProperty("drinks")]
            public List<object> Drinks { get; set; }
        }
        private class IngredientReponse
        {
            [JsonProperty("ingredients")]
            public List<object> Ingredients { get; set; }
        }
        protected class Ingredient
        {
            [JsonProperty("idIngredient")]
            public int Id { get; set; }

            [JsonProperty("strIngredient")]
            public string Name { get; set; }

            [JsonProperty("strDescription")]
            public string Description { get; set; }

            public Embed ToEmbed()
            {
                EmbedBuilder Embed = new EmbedBuilder();
                Embed.WithAuthor(Name);
                Embed.WithColor(Color.DarkMagenta);
                try
                {
                    Embed.WithDescription(Description.Length <= 2048 ? Description : TurncateDesc());
                }
                catch (Exception ex)
                {
                    string e = ex.Message;
                }

                return Embed.Build();
            }

            private string TurncateDesc()
            {
                string returnString = "";

                int i = 0;

                foreach(string s in Description.Split("\r\n\r\n"))
                {
                    if(i > 3)
                    {
                        break;
                    }

                    returnString += s + "\r\n\r\n";
                    i++;
                }

                return returnString;
            }
        }
        protected class Drink
        {
            [JsonProperty("idDrink")]
            public int Id { get; set; }

            [JsonProperty("strDrink")]
            public string Name { get; set; }

            [JsonProperty("strInstructions")]
            public string Instructions { get; set; }

            [JsonProperty("strGlass")]
            public string Glass { get; set; }

            [JsonProperty("strDrinkThumb")]
            public string Thumbnail { get; set; }

            public List<string> Ingredients { get; set; }

            public Drink (dynamic jsonData)
            {
                Id = jsonData.idDrink;
                Name = jsonData.strDrink;
                Instructions = jsonData.strInstructions;
                Glass = jsonData.strGlass;
                Thumbnail = jsonData.strDrinkThumb;

                Ingredients = new List<string>();

                int i = 1;
                bool ingredientRun = true;

                while(ingredientRun)
                {
                    string measure = jsonData["strMeasure" + i];
                    string ing = jsonData["strIngredient" + i];

                    string resultString;

                    if (String.IsNullOrEmpty(measure))
                    {
                        resultString = ing;
                    }
                    else
                    {
                        resultString = measure + " of " + ing;
                    }                    

                    if ((String.IsNullOrEmpty(measure) && String.IsNullOrEmpty(ing)) || i > 14)
                    {
                        ingredientRun = false;
                    }
                    else
                    {
                        Ingredients.Add(resultString);
                        i++;
                    }
                }
            }

            public Embed ToEmbed()
            {
                EmbedBuilder Embed = new EmbedBuilder();
                Embed.WithAuthor(Name);
                Embed.WithColor(Color.DarkMagenta);
                Embed.WithDescription(  $"Glass: {Glass} \n" +
                                        $"Instruction: {Instructions} \n\n" +
                                        $"Ingredients:\n {string.Join("\n", Ingredients.Select(i => i))}");

                if (!String.IsNullOrEmpty(Thumbnail))
                {
                    Embed.WithThumbnailUrl(Thumbnail);
                }

                return Embed.Build();
            }
        }
        #endregion
    }
}
