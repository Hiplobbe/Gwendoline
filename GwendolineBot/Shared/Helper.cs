using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Discord;
using Discord.Commands;

namespace GwendolineBot
{
    public class Helper
    {
        private static Dictionary<string, Color> ModuleColor = new Dictionary<string, Color>
        {
            {"AI", Color.DarkOrange},
            {"Admin", Color.Blue},
            {"Audio", Color.DarkBlue},
            {"CocktailDB", Color.DarkMagenta},
            {"Calendar", Color.DarkGreen},
            {"Drinking", Color.DarkPurple},
            {"Error", Color.Red},
            {"Games", Color.DarkPurple},
            {"Help", Color.Blue},
            {"Imgur", Color.Green},
            {"Raffle", Color.Orange},
            {"Rpg", Color.DarkRed},
            {"Trading", Color.Gold}
        };

        public static string GetApplicationRoot()
        {
            var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;
            return appRoot;
        }

        public static string ToApplicationPath(string fileName)
        {
            var exePath = Path.GetDirectoryName(System.Reflection
                                .Assembly.GetExecutingAssembly().CodeBase);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;
            return Path.Combine(appRoot, fileName);
        }        

        public static int RandomNumber(int maxValue, int minValue = 0)
        {
            Random rand = new Random();

            return rand.Next(minValue, maxValue);
        }

        public static void StandardEmbed(string author, string colorKey, string description, SocketCommandContext context, string thumbnail = "", List<EmbedFieldBuilder> fields = null)
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithAuthor(author);
            Embed.WithColor(ModuleColor[colorKey]);
            Embed.WithDescription(description);

            if (!String.IsNullOrEmpty(thumbnail))
            {
                Embed.WithThumbnailUrl(thumbnail);
            }

            if(fields != null)
            {
                foreach(EmbedFieldBuilder field in fields)
                {
                    Embed.AddField(field);
                }
            }

            context.Channel.SendMessageAsync("", false, Embed.Build());
        }

        public static void StandardEmbedList(string author, string colorKey, List<string> list, SocketCommandContext context, string description = "", bool withoutNum = false)
        {
            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithAuthor(author);
            Embed.WithColor(ModuleColor[colorKey]);

            string desc = $"{description}\n\n";
            int listnum = 1;

            foreach (string var in list)
            {
                if(!withoutNum)
                    desc += $"{listnum}. {var} \n\n";
                else
                    desc += $"{var} \n";

                listnum++;
            }

            Embed.WithDescription(desc);

            context.Channel.SendMessageAsync("", false, Embed.Build());
        }        
    }
}
