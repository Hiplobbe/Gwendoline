using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;

namespace GwendolineBot.Commands.Help
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _Cmds;

        public Help(CommandService cmds)
        {
            _Cmds = cmds;
        }

        [Command("help"), Alias("h")]
        [Summary("Gets information about a command or a modules list of commands")]
        public async Task GetCommandInfo(string name = "")
        {  
            if(name == "")
            {
                List<string> embedText = new List<string>();
                embedText.AddRange(_Cmds.Modules.Select(m => m.Name).OrderBy(t => t));

                Helper.StandardEmbedList("Help", "Help", embedText, Context);
            }
            else
            {
                ModuleInfo mInfo = _Cmds.Modules.Where(m => m.Name.ToLower() == name.ToLower()).FirstOrDefault();

                if (mInfo != null)
                {
                    string embedText = $"Name: {mInfo.Name} \n" +
                                            $"Commands: {string.Join(", ", mInfo.Commands.Select(p => p.Name).OrderBy(p => p))}";

                    Helper.StandardEmbed("Help", "Help", embedText, Context);
                }
                else
                {
                    List<CommandInfo> cmds = _Cmds.Commands.Where(c => c.Name.ToLower() == name.ToLower() || c.Aliases.Any(a => a.ToLower() == name.ToLower())).ToList();
                    CommandInfo cInfo = cmds[0];

                    var parameters = cmds
                                        .Where(c => c.Parameters.Count > 0)
                                        .SelectMany(c => c.Parameters)
                                        .GroupBy(p => p.Name)
                                        .Select(p => p.First())
                                        .ToList();

                    if (cInfo != null)
                    {
                        string embedText = $"Aliases: {string.Join(", ", cInfo.Aliases)} \n" +
                                            $"Summary: {cInfo.Summary} \n" +
                                            $"Attributes: {string.Join(" | ", parameters)}";

                        Helper.StandardEmbed("Help", "Help", embedText, Context);
                    }
                    else
                    {
                        Helper.StandardEmbed("Help", "Help", "No command or module found...", Context);
                    }
                }
            }            
        }
    }
}
