using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using GwendolineBot.Database.Models;
using GwendolineBot.Database.Handlers;

namespace GwendolineBot.Commands.Social
{
    public class DrinkingRules : ModuleBase<SocketCommandContext>
    {
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(DrinkingRules));

        [Command("addrule")]
        [Summary("Adds a new rule for a game.")]
        public async Task AddRule(string game, string level, string rule)
        {
            try
            {
                DrinkingLevel lvl = (DrinkingLevel)Enum.Parse(typeof(DrinkingLevel), level, true);

                DrinkRule nRule = new DrinkRule(game, lvl, rule);

                DbHandler.AddDrinkRule(nRule);

                SendDrinkMessage($"New rule have been added to the game '{game}'");
            }
            catch (Exception ex)
            {
                _Log.Error("Could not add rule. Exception: " + ex.Message);
                SendDrinkMessage($"Failed to add new rule");
            }
        }

        [Command("deleterule"), Alias("delrule")]
        [Summary("Delete a rule for a game.")]
        public async Task DeleteRule(int id)
        {
            bool del = DbHandler.DeleteRule(id);

            if (del)
            {
                SendDrinkMessage($"The rule has been deleted");
            }
            else
            {
                SendDrinkMessage($"No rule found");
            }
        }

        [Command("checkrule"), Alias("rule", "showrule")]
        [Summary("Finds the rules for a game.")]
        public async Task FindRule(string game, bool withId = false)
        {
            List<DrinkRule> rules = DbHandler.GetRules(game);

            if(rules == null || rules.Count == 0)
            {
                SendDrinkMessage($"No rules found for '{game}'");
            }
            else
            {
                SendRuleList(rules, withId);
            }
        }

        [Command("drinkinglevels"), Alias("rulelevels", "rulelvls", "rlvls")]
        [Summary("Lists the different drinking levels for rules.")]
        public async Task DrinkingLevels()
        {
            Helper.StandardEmbedList("Drinking", "Drinking", Enum.GetValues(typeof(DrinkingLevel)).Cast<DrinkingLevel>().Select(x => x.ToString()).ToList(), Context, null, true);
        }

        private void SendRuleList(List<DrinkRule> rules, bool withId)
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithAuthor("Drinking");
            Embed.WithColor(Color.DarkPurple);

            string desc = $"The drinking rules for {rules[0].Game}...\n\n";
            Embed.WithDescription(desc);

            foreach (DrinkingLevel lvl in (DrinkingLevel[])Enum.GetValues(typeof(DrinkingLevel)))
            {
                string text = "";

                foreach(var rule in rules.Where(x => x.Level == lvl))
                {
                    if(withId)
                        text += rule.Id + " " + rule.Rule + "\n";
                    else
                        text += rule.Rule + "\n";
                }

                if(text != "")
                {
                    Embed.AddField(lvl.ToString(), text);
                }
            }

            Context.Channel.SendMessageAsync("", false, Embed.Build());
        }
        private void SendDrinkMessage(string message)
        {
            Helper.StandardEmbed("Drinking", "Drinking", message, Context);
        }
    }
}
