using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GwendolineBot.Database.Models.Music
{
    public class Playlist
    {
        [Key]
        public string Name { get; set; }
        public List<Song> SongList { get; set; }

        public Playlist()
        {
            SongList = new List<Song>();
        }
    }
}
