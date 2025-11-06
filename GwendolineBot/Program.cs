using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Timers;
using System.Reflection;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using GwendolineBot;
using GwendolineBot.Database.Models;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

using GwendolineBot.Database;
using GwendolineBot.Database.Handlers;

namespace GwendolineBot
{
    class Program
    {        
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(Program));
        public static IConfiguration AppConfig = new ConfigurationBuilder().AddJsonFile(Helper.ToApplicationPath("appsettings.json")).Build();
        private DiscordSocketClient _Client;
        private CommandService _Commands;

        private static bool _LoadedReminders = false;

        public static List<RemindModel> todaysReminders = new List<RemindModel>();

        static void Main(string[] args) =>
            new Program().BotSetup().GetAwaiter().GetResult();

        private async Task BotSetup()
        {
            try
            {
                _Log.Info("Setting up database...");
                using (var DbContext = new SqlContext())
                {
                    DbContext.Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                _Log.Error("Error while setting up database: " + ex.Message);
            }
            

            SetupLogger();

            _Commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug,
                DefaultRunMode = RunMode.Async,
                CaseSensitiveCommands = false,
                SeparatorChar = '|'
            });

            await _Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            _Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug
            });

            _Client.Log += _Client_Log;
            _Client.Ready += _Client_Ready;
            _Client.MessageReceived += _Client_MessageReceived;

            await _Client.LoginAsync(TokenType.Bot, AppConfig["BotToken"]);
            await _Client.StartAsync();

            ReminderClock();

            await Task.Delay(-1);            
        }

        public static void SetupLogger()
        {
            XmlDocument log4netConfig = new XmlDocument();

            if(File.Exists("log4net.config"))
            {
                log4netConfig.Load(File.OpenRead("log4net.config"));
            }
            else
            {
                log4netConfig.Load(File.OpenRead("publish/log4net.config"));
            }
            
            var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(),
                       typeof(log4net.Repository.Hierarchy.Hierarchy));
            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
        }

        #region Reminders
        public static void AddReminder(RemindModel model)
        {
            todaysReminders.Add(model);
        }

        public static void ClearReminders()
        {
            todaysReminders.Clear();
        }

        private async Task ReminderClock()
        {
            todaysReminders = DbHandler.GetTodaysReminders();

            var timer = new Timer(60*1000);
            int lastHour = DateTime.Now.Hour;
            timer.Elapsed += new ElapsedEventHandler(CalendarEventCheck);

            DateTime now = DateTime.Now;
            timer.Interval = ((60 - now.Second) * 1000 - now.Millisecond);
            timer.Start();
        }

        private void CalendarEventCheck(object source, ElapsedEventArgs e)
        {
            if (DateTime.Now.Hour == 0 && !_LoadedReminders)
            {     
                todaysReminders.Clear();
                todaysReminders = DbHandler.GetTodaysReminders();

                _Log.Info("Loaded todays reminders..." + todaysReminders.Count);

                _LoadedReminders = true;
            }
            else if(DateTime.Now.Hour != 0 && _LoadedReminders)
            {
                _LoadedReminders = false;
            }
            if(todaysReminders.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.Now;

            List<RemindModel> reminders = todaysReminders.Where(x => x.Time.Hour == now.Hour && x.Time.Minute == now.Minute).ToList();

            foreach(RemindModel remind in reminders)
            { 
                EmbedBuilder Embed = new EmbedBuilder();
                Embed.WithAuthor("Reminder");
                Embed.WithColor(Color.DarkGreen);
                Embed.WithDescription(remind.Message);

                SocketGuild guild = _Client.GetGuild(Convert.ToUInt64(AppConfig["GuildId"]));

                var match = Regex.Match(remind.ChannelName, @"<@([\d]+)>");

                if (!String.IsNullOrEmpty(match.Groups[1].Value))
                {
                    string userId = match.Groups[1].Value;
                    SocketUser user = guild.Users.FirstOrDefault(x => x.Id == Convert.ToUInt64(userId));

                    user.SendMessageAsync("", false, Embed.Build());
                }
                else
                {
                    SocketTextChannel chObj = guild.Channels.First(x => x.Name == remind.ChannelName) as SocketTextChannel;
                    chObj.SendMessageAsync("@here", false, Embed.Build());
                }                

                todaysReminders.Remove(remind);

                if (!remind.IsRepeating)
                {                    
                    DbHandler.DeleteReminder(remind);
                }
                else
                {
                    DbHandler.UpdateReminder(remind.Id);
                }
            }
        }
        #endregion
        #region Client Events
        private async Task _Client_Ready()
        {
            #if DEBUG
                await _Client.SetGameAsync("Under construction", "https://discord.foxbot.me/stable/");
            #endif
        }

        private async Task _Client_Log(LogMessage Message)
        {
            _Log.Info($"[{Message.Source}] {Message.Message}");
        }

        private async Task _Client_MessageReceived(SocketMessage Mess)
        {            
            var Message = Mess as SocketUserMessage;
            var Context = new SocketCommandContext(_Client, Message);

            if (Context.Message.Content.Length <= 0 || Context.User.IsBot)
                return;

            int ArgPos = 0;
            if (!(Message.HasStringPrefix("!", ref ArgPos)) || Message.HasMentionPrefix(_Client.CurrentUser, ref ArgPos))
                return;

            var Result = await _Commands.ExecuteAsync(Context, ArgPos, null);
            if(!Result.IsSuccess)
            {
                _Log.Error($"Error with command '{Context.Message.Content}' : {Result.ErrorReason}");
            }
        }
        #endregion
    }
}
