using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using GwendolineBot;
using GwendolineBot.Database.Models;
using GwendolineBot.Database.Handlers;
using System.Text.RegularExpressions;

namespace GwendolineBot.Commands.Calendar
{
    public class Reminder : ModuleBase<SocketCommandContext>
    {
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(Reminder));

        [Command("AddReminder"), Alias("addrem","remind")]
        public async Task AddReminder(string datetime, string channel, string message, bool isRepeating = false)
        {
            string channelName = "";

            if (Regex.IsMatch(channel,@"#[\d]+"))
            {     
                channelName = Context.Guild.Channels.FirstOrDefault(x => Context.Message.MentionedChannels.Any(y => y.Id == x.Id)).Name;
            }
            else if(Regex.IsMatch(channel, @"@[\d]+"))
            {
                SocketGuildUser user = Context.Guild.Users.FirstOrDefault(x => Context.Message.MentionedUsers.Any(y => y.Id == x.Id));

                if(user != null && !user.IsBot)
                {
                    channelName = channel;
                }
            }
            else
            {
                channelName = Context.Guild.Channels.FirstOrDefault(x => x.Name == channel).Name;
            }

            if (!String.IsNullOrEmpty(channelName))
            {
                try
                {
                    RemindModel model = new RemindModel
                    {
                        Time = DateTime.Parse(datetime),
                        ChannelName = channelName,
                        Message = message,
                        IsRepeating = isRepeating
                    };

                    if(model.Time <= DateTime.Now)
                    {
                        throw new Exception("Older date than now");
                    }

                    DbHandler.InsertReminder(model);
                    
                    if(model.Time.Date == DateTime.Now.Date)
                    {
                        Program.AddReminder(model);
                    }

                    SendEmbed("I will remind you " + message + " on " + datetime);
                }
                catch(Exception ex)
                {
                    _Log.Error(ex.Message + " : " + ex.InnerException);
                    SendEmbed("Do it properly baka!");
                }
            }  
            else
            {
                SendEmbed("I could not find a text channel or non bot user with that name");
            }
        }

        [Command("ShowReminders"), Alias("showrem")]
        public async Task ShowReminders()
        {
            List<RemindModel> list = DbHandler.GetAllReminders();

            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithAuthor("Reminders");
            Embed.WithColor(Color.DarkGreen);
            Embed.WithDescription("Here are all the reminders we have saved...");

            foreach(RemindModel rem in list)
            {
                Embed.AddField("Id: " + rem.Id.ToString(), rem.Time.ToString() + " : " + rem.Message);
            }

            try
            {
                Context.Channel.SendMessageAsync("", false, Embed.Build());
            }
            catch (Exception ex)
            {
                _Log.Error(ex);
            }
        }

        [Command("RemoveReminder"), Alias("remrem","delrem")]
        public async Task RemoveReminder(string Id)
        {
            if (Context.User.Id == Convert.ToUInt64(Program.AppConfig["AdminId"]))
            {
                DbHandler.DeleteReminder(Convert.ToInt32(Id));

                SendEmbed("Removed reminder");
            }            
        }

        [Command("ClearReminders"), Alias("RemoveAllReminders")]
        public async Task RemoveAllReminders()
        {
            if(Context.User.Id == Convert.ToUInt64(Program.AppConfig["AdminId"]))
            {
                DbHandler.DeleteAllReminders();

                SendEmbed("Removed all reminders");
            }            
        }

        private void SendEmbed(string message)
        {
            Helper.StandardEmbed("Reminders", "Calendar", message, Context);
        }
    }
}
