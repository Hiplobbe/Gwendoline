using System;
using System.Linq;
using System.Collections.Generic;

using GwendolineBot.Database;
using GwendolineBot.Database.Models;
using GwendolineBot.Database.Models.Music;

namespace GwendolineBot.Database.Handlers
{
    public static class DbHandler
    {
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(DbHandler));

        #region Reminders
        public static void DeleteReminder(int Id)
        {
            using (var DbContext = new SqlContext())
            {
                RemindModel rem = DbContext.Reminders.FirstOrDefault(x => x.Id == Id);

                if (rem != null)
                {
                    DeleteReminder(rem);
                }

                DbContext.SaveChangesAsync();

                _Log.Info($"Deleted reminder with Id: {Id}");
            }
        }

        public static void DeleteReminder(RemindModel remind)
        {
            using (var DbContext = new SqlContext())
            {
                _Log.Info($"Deleted reminder with Id: {remind.Id}");

                DbContext.Reminders.Remove(remind);

                DbContext.SaveChangesAsync();                
            }
        }        

        public static void InsertReminder(RemindModel rem)
        {
            using (var DbContext = new SqlContext())
            {
                DbContext.Reminders.Add(rem);

                DbContext.SaveChangesAsync();

                _Log.Info($"Saved new reminder with Id: {rem.Id}");
            }
        }        

        public static void UpdateReminder(int Id)
        {
            using (var DbContext = new SqlContext())
            {
                RemindModel model = DbContext.Reminders.FirstOrDefault(x => x.Id == Id);

                model.Time = model.Time.AddDays(7);

                DbContext.SaveChangesAsync();

                _Log.Info($"Updated reminder with Id: {Id}");
            }
        }

        public static void DeleteAllReminders()
        {
            using (var DbContext = new SqlContext())
            {
                DbContext.Reminders.RemoveRange(DbContext.Reminders);

                DbContext.SaveChangesAsync();

                _Log.Info($"Deleted all reminders");
            }
        }

        public static List<RemindModel> GetTodaysReminders()
        {
            using (var DbContext = new SqlContext())
            {
                return DbContext.Reminders.Where(x => x.Time.Date == DateTime.Now.Date).ToList();
            }
        }        

        public static List<RemindModel> GetAllReminders()
        {
            using (var DbContext = new SqlContext())
            {
                return DbContext.Reminders.ToList();
            }
        }        
        #endregion
        #region Playlists
        public static void AddPlaylist(Playlist pl)
        {
            using (var DbContext = new SqlContext())
            {
                DbContext.Playlists.Add(pl);

                DbContext.SaveChangesAsync();
            }
        }        

        public static void UpdatePlaylist(Playlist pl)
        {
            using (var DbContext = new SqlContext())
            {
                DbContext.Playlists.Update(pl);

                DbContext.SaveChangesAsync();
            }
        }

        public static Playlist GetPlaylist(string name)
        {
            using (var DbContext = new SqlContext())
            {
                return DbContext.Playlists.FirstOrDefault(x => x.Name == name);
            }
        }

        public static bool PlaylistExists(string name)
        {
            using (var DbContext = new SqlContext())
            {
                return DbContext.Playlists.Any(x => x.Name == name);
            }
        }
        public static List<string> GetAllPlaylistNames()
        {
            using (var DbContext = new SqlContext())
            {
                return DbContext.Playlists.Select(x => x.Name).ToList();
            }
        }
        #endregion
        #region Drinking Rules
        public static void AddDrinkRule(DrinkRule rule)
        {
            using (var DbContext = new SqlContext())
            {
                DbContext.DrinkingRules.Add(rule);
                DbContext.SaveChangesAsync();
            }
        }
        public static bool DeleteRule(int id)
        {
            using (var DbContext = new SqlContext())
            {
                var rule = DbContext.DrinkingRules.FirstOrDefault(x => x.Id == id);

                if(rule == null)
                {
                    return false;
                }

                DbContext.DrinkingRules.Remove(rule);
                DbContext.SaveChanges();
                return true;
            }
        }
        public static List<DrinkRule> GetRules(string game)
        {
            using (var DbContext = new SqlContext())
            {
                var rules = DbContext.DrinkingRules.Where(x => x.Game.ToLower() == game.ToLower()).OrderByDescending(x => x.Level).ToList();

                return rules;
            }
        }
        #endregion
    }
}
