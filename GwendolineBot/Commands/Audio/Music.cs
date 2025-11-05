using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Discord;
using Discord.Audio;
using Discord.Commands;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;

using GwendolineBot;
using GwendolineBot.Database.Handlers;
using GwendolineBot.Database.Models.Music;
using System.Runtime.InteropServices;

namespace GwendolineBot.Commands.Audio
{
    //TODO: Add support for soundcloud
    //TODO: Add support for Spotify

    public class Music : ModuleBase<SocketCommandContext>
    {
        private static readonly log4net.ILog _Log = log4net.LogManager.GetLogger(typeof(Music));

        private static IAudioClient audioClient;
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

        private static bool shuffle = false;
        private static int lastIndex = 0;
        private static Playlist SongPlayList = new Playlist();

        #region Commands
        [Command("join")]
        public async Task JoinVoice()
        {
            if (audioClient == null)
                audioClient = GetAudioClient().GetAwaiter().GetResult();
        }

        [Command("shuffle")]
        public async Task ShuffleList()
        {
            shuffle = !shuffle;

            if (shuffle)
            {
                SendEmbed("The list will now be shuffled!");
            }
            else
            {
                SendEmbed("The list will not be shuffled!");
            }            
        }

        [Command("leave"), Alias("stop")]
        public async Task LeaveVoice()
        {  
            if (audioClient != null)
            {
                SendEmbed("Stopping music...");
                StopMusic();
                await audioClient.StopAsync();
                audioClient = null;                
            }                
        }

        [Command("pause")]
        public async Task PauseVoice()
        {
            if (audioClient != null)
            {
                StopMusic();
            }
        }

        [Command("ShowPlaylist"), Alias("showpl")]
        public async Task ShowPlaylists()
        {
            List<string> songList = SongPlayList.SongList.Select(x => x.Title).ToList();

            SendEmbedList(songList);
        }

        [Command("ShowPlaylists"), Alias("showpls","spls")]
        public async Task ShowPlaylists(string listname)
        {
            List<string> names = DbHandler.GetAllPlaylistNames();

            SendEmbedList(names);
        }        

        [Command("loadlist"), Alias("loadpl")]
        public async Task LoadPlaylist(string listname)
        {
            if(DbHandler.PlaylistExists(listname))
            {
                SongPlayList = DbHandler.GetPlaylist(listname);
            }
            else
            {
                SendEmbed("I cannot find a playlist with that name");
            }
        }

        [Command("savelist"), Alias("spl")]
        public async Task SavePlaylist(string listname = "")
        {
            if(SongPlayList.SongList.Count == 0)
            {
                SendEmbed("The playlist can't be empty!");

                return;
            }
            else if(!String.IsNullOrEmpty(SongPlayList.Name) && DbHandler.PlaylistExists(SongPlayList.Name))
            {
                DbHandler.UpdatePlaylist(SongPlayList);

                SendEmbed("The playlist has been updated");
            }
            else if(!String.IsNullOrEmpty(listname))
            {
                DbHandler.AddPlaylist(SongPlayList);

                SendEmbed("The playlist has been saved");
            }
            else
            {
                SendEmbed("Please choose a name for the playlist");

                return;
            }
        }

        [Command("clearlist"), Alias("clearpl","cpl")]
        public async Task ClearPlaylist()
        {
            SongPlayList = new Playlist();

            SendEmbed("Clearing playlist..");
            StopMusic();
        }

        [Command("play"), Alias("add")]
        public async Task AddSong([Remainder] string input)
        {
            Song song = SearchYoutube(input).GetAwaiter().GetResult();

            if (song != null)
            {
                SongPlayList.SongList.Add(song);
                SendEmbed("Added " + song.Title + " to the playlist");
            }
            else
            {
                SendEmbed("Couldn't find any song to add.");
            }

            if (audioClient == null && SongPlayList.SongList.Count == 1)
                PlayLink(song);
        }

        [Command("next")]
        public async Task NextSong()
        {
            Song song = GetNextSong();
            
            if (audioClient != null && song != null)
            {
                SendEmbed("Now playing " + song.Title);

                await StopMusic();

                await PlayLink(song);                
            }
            else if (song == null)
            {
                SendEmbed("There are no beatsies in the list!");

                LeaveVoice();
            }
        }
        #endregion
        #region Private

        private async Task PlayLink(Song song)
        {
            if (audioClient == null)
                await JoinVoice();

            if (song != null && audioClient != null)
            {
                await PlaySongTask(song);
            }
            else
            {
                SendEmbed("I must be in voice to play some fat beatsies!");
            }
        }

        private async Task StopMusic()
        {
            var cs = cancelTokenSource;
            cancelTokenSource = new CancellationTokenSource();
            cs.Cancel();            
        }
        private async Task PlaySongTask(Song song)
        {
            CancellationToken token = cancelTokenSource.Token;

            try
            {
                using (var ffmpeg = await CreateStream(song.Url))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Music, bufferMillis: 2))
                {
                    try
                    {
                        await output.CopyToAsync(discord, token);                        
                    }
                    catch(OperationCanceledException cancel)
                    {
                        _Log.Info("Canceled music task");
                    }
                    catch (Exception ex)
                    {
                        _Log.Error($"Issue with streaming audio : {ex.Message}");
                    }
                    finally
                    {
                        ffmpeg.Close();
                        ffmpeg.Dispose();
                        await discord.FlushAsync();                        

                        if (token.IsCancellationRequested)
                        {
                            await discord.ClearAsync(token);
                            token.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            NextSong();
                        }
                    }
                }
            }
            catch (OperationCanceledException cancel)
            {
                _Log.Info("Canceled music task");
            }
            catch (Exception ex)
            {
                _Log.Error($"Issue with playing audio : {ex.Message}");
            }
        }
        private async Task<Song> SearchYoutube(string searchTerm)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Program.AppConfig["API:GoogleApiKey"]             
            });
            
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = searchTerm;
            searchListRequest.MaxResults = 10;
            searchListRequest.Type = "video";

            // Call the search.list method to retrieve results matching the specified query term.
            try
            {
                var searchListResponse = await searchListRequest.ExecuteAsync();

                // Add each result to the appropriate list, and then display the lists of
                // matching videos, channels, and playlists.
                foreach (var searchResult in searchListResponse.Items)
                {
                    switch (searchResult.Id.Kind)
                    {
                        case "youtube#video":
                            return new Song {Title= searchResult.Snippet.Title, Url = "https://www.youtube.com/watch?v=" + searchResult.Id.VideoId };

                        case "youtube#playlist":
                            //TODO: Add playlists in different method
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                _Log.Error($"Issue with youtube search : {ex.Message}");
            }           

            return null;
        }
        private async Task<Process> CreateStream(string url)
        {
            string[] yUri = (await GetYoutubeAudio(url).ConfigureAwait(false)).Split('\n');

            //TODO: Remove me
            Console.WriteLine($"ffmpeg -err_detect ignore_err -i \"{yUri[2]}\" -f s16le -ar 48000 -vn -ac 2 pipe:1 -loglevel error");

            var args = $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -err_detect ignore_err -i {yUri[2]} -f s16le -ar 48000 -vn -ac 2 pipe:1 -loglevel error";

            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
            });
        }
        private async Task<string> GetYoutubeAudio(string url)
        {
            // escape the minus on the video argument
            // to prevent youtube-dl to handle it like an argument
            if (url != null && url.StartsWith("-"))
                url = '\\' + url;

            using (Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "youtube-dl",
                    Arguments = $"-4 --geo-bypass -f bestaudio -e --get-url --get-id --get-thumbnail --get-duration --no-check-certificate --default-search \"ytsearch:\" \"{url}\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                },
            })
            {
                _Log.Info($"Executing {process.StartInfo.FileName} {process.StartInfo.Arguments}");

                process.Start();
                var str = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                var err = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(err))
                    _Log.Error(err);
                return str;
            }
        }
        private async Task<IAudioClient> GetAudioClient()
        {
            //TODO: Add support for multiple channels?
            try
            {
                var channel = (Context.User as IVoiceState)?.VoiceChannel;

                if (channel != null)
                {
                    return await channel.ConnectAsync();
                }
            }
            catch (Exception ex)
            {
                _Log.Error(ex);
            }

            return null;
        }
        private Song GetNextSong()
        {
            SongPlayList.SongList.RemoveAt(lastIndex);

            if (SongPlayList.SongList.Count > 0)
            {
                int index = 0;

                if(shuffle)
                {
                    index = Helper.RandomNumber(SongPlayList.SongList.Count);

                    while (index == lastIndex)
                    {
                        index = Helper.RandomNumber(SongPlayList.SongList.Count);
                    }
                }

                lastIndex = index;

                return SongPlayList.SongList[index];
            }

            return null;
        }
        private void SendEmbedList(List<string> names)
        {
            Helper.StandardEmbedList("Music Player", "Games", names, Context, "Here are all the playlists I have saved...");
        }
        private void SendEmbed(string message)
        {
            Helper.StandardEmbed("Music Player", "Games", message, Context);            
        }
        #endregion
    }
}
