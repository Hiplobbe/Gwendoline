using System;
using System.IO;

using Microsoft.EntityFrameworkCore;

using GwendolineBot.Database.Models;
using GwendolineBot.Database.Models.Music;
using System.Runtime.InteropServices;

namespace GwendolineBot.Database
{
    public class SqlContext : DbContext 
    {
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(SqlContext));

        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<RemindModel> Reminders { get; set; }
        public DbSet<DrinkRule> DrinkingRules { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder Options)
        {
            try
            {
                string location = Directory.GetCurrentDirectory();

                Directory.CreateDirectory($"{location}/Database");
                Options.UseSqlite($"Data Source={location}/Database/Database.Sqlite");
            }
            catch (Exception ex)
            {
                _Log.Error("Error when configuring database: " + ex.Message);
            }
        }

        internal void Save()
        {
            try
            {
                SaveChangesAsync();
            }
            catch(Exception ex)
            {
                _Log.Error("Error when saving database: " + ex.Message);
            }
        }
    }
}
