using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Interactions;
using Newtonsoft.Json;

namespace GwendolineBot.Commands.Api;

public class AI: ModuleBase<SocketCommandContext>
{
    private static HttpClient client = new HttpClient();

    private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(AI));
    
    private static readonly string _clientUrl = Program.AppConfig["API:AI:url"];
    private static readonly string _clientKey = Program.AppConfig["API:AI:key"];
    
    [Command("AIMessage"), Alias("aimess, aim")]
    [Discord.Commands.Summary("Sends a prompt to configured AI model.")]
    public async Task Message([Remainder] string message)
    {
        AIRequest request = new AIRequest(message);

        try
        {
            _log.Info($"User {Context.User.Username} send and AI message: {message}");
            HttpResponseMessage response = await GetResponse(_clientUrl, request);

            if (response.IsSuccessStatusCode)
            {
                MistralResponse returnMessage =
                    JsonConvert.DeserializeObject<MistralResponse>(response.Content.ReadAsStringAsync().Result);

                if (returnMessage.Choices.Length > 0)
                {
                    _log.Info($"Got response: {returnMessage.Choices[0].Message.Content}");
                    Helper.StandardEmbed("AI", "AI", returnMessage.Choices[0].Message.Content, Context);
                }
            }
            else
            {
                _log.Error($"Unable to get a response from AI: {response.StatusCode + " " + response.Content}");
                Helper.StandardEmbed("AI", "AI", "Unable to contact AI API.", Context);
            }
        }
        catch (Exception e)
        {
            _log.Error($"Unable to get a response from AI: {e.Message}");
            Helper.StandardEmbed("AI", "AI", "Unable to contact AI API.", Context);
        }
    }
    
    #region Private methods

    private async Task<HttpResponseMessage> GetResponse(string url, AIRequest request)
    {
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri(url);
        
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _clientKey);

        StringContent content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, @"application/json");
        
        return client.PostAsync(url, content).Result;
    }
    
    #endregion
    
    #region Classes & Enums

    private class AIRequest
    {
        [JsonProperty("messages")]
        public MistralMessage[] Messages { get; set; }
        
        [JsonProperty("model")]
        public string Model { get; set; }
        
        [JsonProperty("temperature")]
        public double Temperature { get; set; }
        
        [JsonProperty("max_tokens")]
        public int MaxTokens = 255;
        
        public AIRequest(string message, string model = "mistral-large-latest", double temperature = 0.3)
        {
            MistralMessage mess = new MistralMessage { Content = message };
            Messages = [mess];
            
            Model = model;
            Temperature = temperature;
        }
    }
    
    private class MistralResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("choices")]
        public Choice[] Choices { get; set; }
        
        [JsonProperty("model")]
        public string Model { get; set; }
        
        [JsonProperty("object")]
        public string Obj { get; set; }
        
        [JsonProperty("created")]
        public string Created { get; set; }
    }

    private class Choice
    {
        [JsonProperty("finish_reason")]
        public string Reason { get; set; }
        
        [JsonProperty("message")]
        public MistralMessage Message { get; set; }
    }

    private class MistralMessage
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("role")] 
        public string Role { get; set; } = "user";
    }

    #endregion
}