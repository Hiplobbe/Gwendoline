using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

namespace GwendolineBot.Commands.Rpg
{
    
    public class Diceroller : ModuleBase<SocketCommandContext>
    {
        [Command("roll")]
        [Summary("Does a simple roll given the number of dice and type. Example: 3d6")]
        public async Task Roll([Remainder]string input)
        {
            if(Regex.IsMatch(input, @"([\d]+)d([\d]+)[\s]*$"))
            {
                var result = Regex.Match(input, @"([\d]+)d([\d]+)[\s]*$");

                int times = Convert.ToInt32(result.Groups[1].Value); 
                int sides = Convert.ToInt32(result.Groups[2].Value) + 1;

                int amount = 0;
                string resultMessage = "";
                for (int i = 0; i < times; i++)
                {
                    int roll = Helper.RandomNumber(sides, 1);

                    resultMessage += roll + " ";

                    amount += roll;
                }

                await Context.Channel.SendMessageAsync("", false, SendDiceEmbed(resultMessage, amount.ToString()));
            }
            else
            {
                await Context.Channel.SendMessageAsync("You made a stupid roll!", false);
            }            
        }
        
        private Embed SendDiceEmbed(string rolls, string result)
        {
            EmbedBuilder Embed = new EmbedBuilder();
            //Embed.WithAuthor("Dice roller");
            Embed.WithColor(Color.DarkRed);
            Embed.AddField("Rolls", rolls);
            Embed.AddField("Sum", result);

            return Embed.Build();
        }
    }
}
