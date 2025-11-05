using System;
using System.Reflection;
using System.Threading.Tasks;

using GwendolineBot;

using Discord;
using Discord.Commands;
using System.Diagnostics;

namespace GwendolineBot.Commands.Gwen
{
    public class GwenCommands : ModuleBase<SocketCommandContext>
    {
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(GwenCommands));

        [Command("close"), Alias("shutdown","kill","quit")]
        public async Task Close()
        {
            if (Context.Message.Author.Id == Convert.ToUInt64(Program.AppConfig["AdminId"]))
            {
                _Log.Info("User requested shutdown");
                Helper.StandardEmbed("Admin", "Admin", "Closing down...", Context);

                Environment.Exit(0);
            }
        }
    }
}
