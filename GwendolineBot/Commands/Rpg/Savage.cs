using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;

namespace GwendolineBot.Commands.Rpg
{
    
    public class Savage : ModuleBase<SocketCommandContext>
    {
        #region Initiative
        //private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(Savage));
        private static Dictionary<string, int> CurrentOrder = new Dictionary<string, int>();
        private static List<string> Characters = new List<string>();

        [Command("Initiative"), Alias("ini", "savini")]
        [Summary("Draws initiative for the number of characters chosen.")]
        public async Task Initiative()
        {
            if(Characters.Count > 0)
            {
                Random rand = new Random();
                CurrentOrder = new Dictionary<string, int>();

                for (int i = 0; i < Characters.Count; i++)
                {
                    int order = rand.Next(0, 53);

                    if (!CurrentOrder.Any(x => x.Value == order))
                    {
                        CurrentOrder.Add(Characters[i], order);
                    }
                    else
                    {
                        i--;
                    }
                }

                SendCurrentOrder();
            }
            else
            {
                SendEmbed("There are no characters in combat!");
            }
        }

        [Command("AddCombatCharacters"), Alias("addchar", "addcombat")]
        [Summary("Adds characters to the combat list.")]
        public async Task AddCharacters(params string[] chars)
        {
            foreach (var ch in chars)
            {
                if(!Characters.Any(x => x == ch))
                {
                    Characters.Add(ch);
                }
            }

            SendEmbed("Characters has been added to the combat list");
        }

        [Command("InitiativeOrder"), Alias("iniorder", "saviniorder", "order")]
        [Summary("Gets the current initiative order.")]
        public async Task GetOrder()
        {
            SendCurrentOrder();
        }

        [Command("CombatCharacters"), Alias("chars", "combatchars", "combat")]
        [Summary("Gets the current combat characters list.")]
        public async Task GetCharacters()
        {
            Helper.StandardEmbedList("Savage worlds", "Rpg", Characters, Context, "Characters in combat");
        }

        [Command("ClearInitiativeOrder"), Alias("clearini", "clearsavini")]
        [Summary("Clears the current initiative order.")]
        public async Task ClearOrder()
        {
            CurrentOrder = new Dictionary<string, int>();

            SendEmbed("The current order has been cleared"); 
        }

        [Command("ClearCharacters"), Alias("clearchar", "clearsavchar")]
        [Summary("Clears the current combat character list.")]
        public async Task ClearCharacters()
        {
            Characters = new List<string>();

            SendEmbed("The character list has been cleared");
        }

        private void SendEmbed(string message)
        {
            Helper.StandardEmbed("Savage worlds", "Rpg", message, Context);
        }

        private void SendCurrentOrder()
        {
            Helper.StandardEmbedList("Savage worlds", "Rpg", OrderToList(), Context, "Current initiative order");
        }

        private List<string> OrderToList()
        {
            List<string> returnList = new List<string>();

            CurrentOrder = CurrentOrder.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            foreach (var cha in CurrentOrder)
            {
                if(cha.Value > 50)
                {
                    returnList.Add($"{cha.Key} with a JOKER!");
                }
                else
                {
                    returnList.Add($"{cha.Key}");
                }
            }

            return returnList;
        }
        #endregion

        [Command("RaiseCalculator"), Alias("raise", "calcraise")]
        [Summary("Calculates the number of raises from a roll.")]
        public async Task CalculateRaises(int roll, int target)
        {
            decimal raise = (roll - target) / 4;
            raise = Math.Round(raise, 0);


            SendEmbed($"The roll {roll} on {target} results in {Convert.ToInt32(raise)} number of raises");
        }
    }
}
