using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GwendolineBot.Database.Models
{
    public class DrinkRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DrinkingLevel Level { get; set; }
        public string Game { get; set; }        
        public string Rule { get; set; }

        public DrinkRule()
        {

        }

        public DrinkRule(string game, DrinkingLevel lvl, string rule)
        {
            Game = game;
            Level = lvl;
            Rule = rule;
        }
    }

    public enum DrinkingLevel
    {
        One,
        Two,
        Three,
        Five,
        Glass
    }
}
