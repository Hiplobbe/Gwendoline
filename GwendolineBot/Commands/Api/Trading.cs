using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GwendolineBot.Commands.Api
{
    /// <summary>
    /// API for Stock/Currency exchanges.
    /// </summary>
    public class Trading : ModuleBase<SocketCommandContext>
    {
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(Trading));

        private static readonly string _searchUrl = "https://www.alphavantage.co/query";
        private static readonly string _apiKey = Program.AppConfig["API:AlphavantageKey"];

        #region Commands

        [Command("StockSearch"), Alias("stock", "stocks")]
        [Summary("Searches for the most relevant stock based on the search term.")]
        public async Task StockSearch([Remainder] string searchTerm)
        {
            string call = _searchUrl + $"?function=SYMBOL_SEARCH&keywords={searchTerm}&apikey={_apiKey}";

            var response = await GetResponse(call);

            if (response.IsSuccessStatusCode)
            {
                _Log.Info($"A successfull search for a stock name with the term {searchTerm}");

                string result = await response.Content.ReadAsStringAsync();
                List<StockSearchResponse> list = JObject.Parse(result)
                    .SelectToken("bestMatches")
                    .ToObject<List<StockSearchResponse>>()
                    .OrderByDescending(x => x.Score)
                    .ToList();

                List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

                foreach (StockSearchResponse item in list)
                {
                    fields.Add(new EmbedFieldBuilder()
                        .WithName(item.Name)
                        .WithValue(
                            $"Symbol: {item.Symbol} \r\n" +
                            $"Type: {item.Type} \r\n" +
                            $"Region: {item.Region} \r\n" +
                            $"Currency: {item.Currency} \r\n"
                        )
                    );
                }

                Helper.StandardEmbed("Stock search", "Trading", $"Here are the found results for {searchTerm}, sorted by relevance", Context, null, fields);
            }
        }

        [Command("StockQoute"), Alias("qoute")]
        [Summary("Searches for the most current qoute for a stock symbol")]
        public async Task StockSearch(string symbol, bool extendedInfo = false)
        {
            string call = _searchUrl + $"?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

            var response = await GetResponse(call);

            if (response.IsSuccessStatusCode)
            {
                _Log.Info($"A successfull search for a {symbol} current qoute");

                string result = await response.Content.ReadAsStringAsync();
                StockQoute qoute = JObject.Parse(result)
                    .First
                    .First
                    .ToObject<StockQoute>();

                List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

                fields.Add(new EmbedFieldBuilder()
                        .WithName("Price")
                        .WithValue(String.Format("{0:n}", Math.Round(qoute.Price, 4)))
                );

                if (extendedInfo)
                {
                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Highest")
                        .WithValue(String.Format("{0:n}", Math.Round(qoute.Highest, 4)))
                    );

                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Lowest")
                        .WithValue(String.Format("{0:n}", Math.Round(qoute.Lowest, 4)))
                    );

                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Lastest trade date")
                        .WithValue(qoute.TradingDay)
                    );

                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Change")
                        .WithValue(String.Format("{0:n}", Math.Round(qoute.ChangeDecimal, 4)))
                    );

                    fields.Add(new EmbedFieldBuilder()
                        .WithName("Percentage")
                        .WithValue(qoute.ChangePercent)
                    );
                }

                Helper.StandardEmbed("Stock qoute", "Trading", $"Here is the most recent qoute for {qoute.Symbol}", Context, null, fields);
            }
        }

        #endregion

        #region Private methods
        private async Task<HttpResponseMessage> GetResponse(string url)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            return client.GetAsync(url).Result;
        }
        #endregion

        #region Classes

        internal class StockSearchResponse
        {
            [JsonProperty("1. symbol")]
            public string Symbol { get; set; }

            [JsonProperty("2. name")]
            public string Name { get; set; }

            [JsonProperty("3. type")]
            public string Type { get; set; }

            [JsonProperty("4. region")]
            public string Region { get; set; }

            [JsonProperty("8. currency")]
            public string Currency { get; set; }

            [JsonProperty("9. matchScore")]
            public decimal Score { get; set; }
        }

        internal class StockQoute
        {
            [JsonProperty("01. symbol")]
            public string Symbol { get; set; }

            [JsonProperty("02. open")]
            public decimal Open { get; set; }

            [JsonProperty("03. high")]
            public decimal Highest { get; set; }

            [JsonProperty("04. low")]
            public decimal Lowest { get; set; }

            [JsonProperty("05. price")]
            public decimal Price { get; set; }

            [JsonProperty("07. latest trading day")]
            public string TradingDay { get; set; }

            [JsonProperty("08. change")]
            public decimal ChangeDecimal { get; set; }

            [JsonProperty("10. change percent")]
            public string ChangePercent { get; set; }
        }
        #endregion
    }
}
