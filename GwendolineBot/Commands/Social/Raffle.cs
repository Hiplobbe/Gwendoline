using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;


namespace GwendolineBot.Commands.Social
{
    public class Raffle : ModuleBase<SocketCommandContext>
    {
        private static DateTime lastDraw = DateTime.MinValue;
        private static Dictionary<ulong,string> vetoList = new Dictionary<ulong, string>();
        private static List<string> ticketList = new List<string>();

        [Command("addticket"), Alias("addtick")]
        [Summary("Adds a ticket to the raffle.")]
        public async Task AddTicket(params string[] tickets)
        {
            foreach (string tick in tickets)
            {
                ticketList.Add(tick);
            }

            if (tickets.Length > 1)
            {
                Helper.StandardEmbedList("Raffle", "Raffle", tickets.ToList(), Context, "I added the tickets");
            }
            else
            {
                SendRaffleMessage($"Ticket {tickets[0]} was added");
            }
        }

        [Command("raffle")]
        [Summary("Takes a ticket from the raffle and removes it from the list")]
        public async Task RaffleTicket()
        {
            if (ticketList.Count > 0 && ValidateRaffleRight())
            {
                int ticketIndex = Helper.RandomNumber(ticketList.Count);

                SendRaffleMessage($"Ticket taken was {ticketList[ticketIndex]}");

                ticketList.RemoveAt(ticketIndex);

                lastDraw = DateTime.Now;
            }
            else if(!ValidateRaffleRight())
            {
                string validTime = lastDraw.AddMinutes(5).ToShortTimeString();

                SendRaffleMessage($"Wait until {validTime} for next raffle draw");
            }
            else
            {
                SendRaffleMessage("There are no tickets in the raffle!");
            }
        }

        [Command("cleartickets"), Alias("cltick", "cleartick")]
        [Summary("Removes all the tickets from the list and refreshes the veto rights")]
        public async Task ClearTickets()
        {
            lastDraw = DateTime.MinValue;
            vetoList.Clear();
            ticketList.Clear();

            SendRaffleMessage("The ticket list is now empty");
        }

        [Command("showtickets"), Alias("showtick")]
        [Summary("Shows the tickets in the current raffle.")]
        public async Task ShowTickets()
        {
            if (ticketList.Count > 0)
            {
                Helper.StandardEmbedList("Raffle", "Raffle", ticketList, Context, "These are all the tickets in the raffle");
            }
            else
            {
                SendRaffleMessage("There are no tickets in the raffle");
            }
        }

        [Command("showvetos"), Alias("showveto")]
        [Summary("Shows the users that has vetoed this raffle")]
        public async Task ShowVetos()
        {
            if (vetoList.Count > 0)
            {
                Helper.StandardEmbedList("Raffle", "Raffle", vetoList.Select(v => v.Value).ToList(), Context, "These are the users that has vetoed");
            }
            else
            {
                SendRaffleMessage("No one has vetoed yet");
            }
        }

        [Command("vetoticket"), Alias("veto")]
        [Summary("Removes a ticket from the raffle, can only be done once per user")]
        public async Task VetoTicket(string ticket)
        {
            if (ticketList.Any(t => t.ToLower() == ticket.ToLower()))
            {
                if (!vetoList.Any(v => v.Key == Context.User.Id))
                {
                    ticketList.RemoveAll(t => t.ToLower() == ticket.ToLower());

                    vetoList.Add(Context.User.Id, Context.User.Username);

                    SendRaffleMessage($"{ticket} was removed by {Context.User.Mention}");
                }
                else
                {
                    SendRaffleMessage($"{Context.User.Username} has already vetoed!");
                }
            }
            else
            {
                SendRaffleMessage($"Cloud not find the ticket {ticket} in the raffle");
            }
        }

        private bool ValidateRaffleRight()
        {
            if(Context.User.Id == Convert.ToUInt64(Program.AppConfig["AdminId"]) || DateTime.Now > lastDraw.AddMinutes(5))
            {
                return true;
            }

            return false;
        }

        private void SendRaffleMessage(string message)
        {
            Helper.StandardEmbed("Raffle", "Raffle", message, Context);
        }
    }
}
