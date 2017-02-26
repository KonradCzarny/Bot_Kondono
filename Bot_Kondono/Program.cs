
using Discord;
using Discord.Commands;
using Discord.Audio;
using Discord.Modules;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.API.Client.Rest;


namespace Bot_Kondono
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Start();
        }
       
        // Audio client
        private IAudioClient _vClient;
        private bool playingSong = false;


        private DiscordClient _client;

        public void Start()
        {

            _client =new DiscordClient(x =>
            {
                x.AppName = "Bot Kondono";
                x.LogLevel=LogSeverity.Info;
                x.LogHandler = Log;
            });

            _client.UsingCommands(x =>
            {
                x.PrefixChar = '!';             //command prefix
                x.AllowMentionPrefix = true;   //allows @discordbot {command}
                x.HelpMode=HelpMode.Public;

            });
            CreateCommands();

            // Discord.Audio
            _client.UsingAudio(x =>
            {
                x.Mode = AudioMode.Outgoing;
            });

            _client.ExecuteAndWait(async () =>
            {
                await _client.Connect("YOUR BOT TOKEN GOES HERE", TokenType.Bot); // Place your bot token here
            });
        }

        private void CreateCommands()
        {
            //ping
            var cService = _client.GetService<CommandService>();
            cService.CreateCommand("ping")
                .Description("Wanna play some ping-pong?")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage("pong");
                });

            //hello
            cService.CreateCommand("hello")
                .Description("Greets user. Type !hello <username>")
                .Parameter("user", ParameterType.Required)
                .Do(async (e) =>
                {
                    var toReturn = $"{e.User.Name} says hi to {e.GetArg("user")}!";
                    await e.Channel.SendMessage(toReturn);
                });

            //cat
            cService.CreateCommand("cat")
                .Description("Kitty :3")
                .Do(async (e) =>
                {
                    await e.Channel.SendFile("cat.jpg");
                });

            //play
            cService.CreateCommand("play")
                .Description("Plays a song")
                .Do(async (e) =>
                {
                    // Checking if there is already playing a song;
                    if (playingSong == true) return;

                    // Checking if the person who used the !play command is on a voice channel:
                    Channel voiceChan = e.User.VoiceChannel;
                    if (voiceChan == null)
                    {
                        await e.Channel.SendMessage("You are not connected to the voice channel.");
                        return;
                    }
                    

                    await e.Channel.SendMessage("Playing file...");
                   
                    playingSong = true;

                    // File location
                    await SendAudio(@"PATH TO THE SONG GOES HERE", voiceChan); //Simply put here a path to your song, or place it in debug folder and write its name here

                    await e.Channel.SendMessage("Finished playing file..");

                    // Song is finished, set the playingSong to false:
                    playingSong = false;
                });

            //stop
            cService.CreateCommand("stop")
                .Description("Stops a song")
                .Do(async e =>
                {
                    // If there is no song playing, then there is no need to stop.
                    if (playingSong == false) return;

                    // If the SendAudio method, "playingSong" is  in the while loop, setting this to false will make it jump out of the loop.
                    playingSong = false;
                    await e.Channel.SendMessage("Skipping the song.");
                });


        }

        private async Task SendAudio(string filepath, Channel voiceChannel)
        {
            
        // Using !play command, starts this method
        _vClient = await _client.GetService<AudioService>().Join(voiceChannel);


        try
        {

            var channelCount = _client.GetService<AudioService>().Config.Channels; 
            var OutFormat = new WaveFormat(48000, 16, channelCount); 

            using (var MP3Reader = new Mp3FileReader(filepath)) // Disposable MP3FileReader reading audio from the filePath 
            using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Disposable Resampler converying MP3 data to PCM
            {
                resampler.ResamplerQuality = 60; 
                int blockSize = OutFormat.AverageBytesPerSecond / 50; 
                byte[] buffer = new byte[blockSize];
                int byteCount;

                // opus.dll and libsodium.dll libraried are needed for this to work
                while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0 && playingSong)
                {
                    if (byteCount < blockSize)
                    {
                        
                        for (int i = byteCount; i < blockSize; i++)
                            buffer[i] = 0;
                    }

                    _vClient.Send(buffer, 0, blockSize); 
                }
                await _vClient.Disconnect();
            }
        }
        catch
        {
            System.Console.WriteLine("Something went wrong. :(");
        }
        await _vClient.Disconnect();
    }

        public void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine($"[{e.Severity}] [{e.Source}] {e.Message}");  //[INFO] [DISCORD] Client Connected
        }
    }
}
