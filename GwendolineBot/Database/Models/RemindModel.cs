using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GwendolineBot.Database.Models
{
    public class RemindModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public string Message { get; set; }
        public string ChannelName { get; set; }
        public bool IsRepeating { get; set; }
    }
}
