
using Discord;
using Discord.Commands;
using Discord.Audio;
using Discord.Modules;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
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
        public static IAudioClient _vClient;

        // Bool, when playing a song, set it to true, so you don't play two songs at the same time >_>
        private static bool playingSong = false;
        public static DiscordClient _client;

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
                x.PrefixChar = '!';
                x.AllowMentionPrefix = true;   //allows @discordbot {command}
                x.HelpMode=HelpMode.Public;

            });
            CreateCommands();

            // Discord.Audio stuff.  Got to set the mode to outgoing.
            _client.UsingAudio(x =>
            {
                x.Mode = AudioMode.Outgoing;
            });

            _client.ExecuteAndWait(async () =>
            {
                await _client.Connect("TOKEN HERE", TokenType.Bot); // Place your token bot token here
            });
        }

        public void CreateCommands()
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
                .Description("Greets user")
                .Parameter("user", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    var toReturn = $"Hi {e.GetArg("user")}!";
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
                    // Check to see if a song is already playing.  If it is, return;
                    if (playingSong == true) return;

                    // Check to see if the person who used the !play command is on a voice channel:
                    Channel voiceChan = e.User.VoiceChannel;
                    if (voiceChan == null)
                    {
                        // If they aren't, call them out on their stupidity.
                        // Note:  Some times discord bugs, and the bot won't see  you in a voice channel.
                        // If this happens, disconnect and reconnect to voice while the bot is online.
                        await e.Channel.SendMessage("You want me to play a song for you, but you're not even connected to voice? Pfftt.");
                        return;
                    }
                    // Okay, they're on a voice channel.

                    await e.Channel.SendMessage("Playing file...");
                    // Set the "PlayingSong" to true.
                    playingSong = true;

                    // File location
                    await SendAudio(@"song.mp3", voiceChan);

                    await e.Channel.SendMessage("Finished playing file..");

                    // Song is finished, set the playingSong to false:
                    playingSong = false;
                });
            //skip
            cService.CreateCommand("skip")
                .Description("Skips a song")
                .Do(async e =>
                {
                    // If there is no song playing, no need to skip.
                    if (playingSong == false) return;

                    // In the SendAudio method, "playingSong" is being used in the while loop.
                    // Setting this to false, will make it jump out of the while loop.
                    playingSong = false;
                    await e.Channel.SendMessage("Skipping the song.");
                });


        }

        public static async Task SendAudio(string filepath, Channel voiceChannel)
        {
            // Using !play command, starts this method

            // The comment below is how you'd find the first voice channel on the server "Somewhere"
            //var voiceChannel = _client.FindServers("Somewhere").FirstOrDefault().VoiceChannels.FirstOrDefault();
            _vClient = await _client.GetService<AudioService>().Join(voiceChannel);

            // Simple try and catch.
            try
            {

                var channelCount = _client.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
                var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.

                using (var MP3Reader = new Mp3FileReader(filepath)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
                {
                    resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                    int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                    byte[] buffer = new byte[blockSize];
                    int byteCount;
                    // Add in the "&& playingSong" so that it only plays while true. For our cheesy skip command.
                    // WARNING
                    // YOU NEED
                    // vvvvvvvvvvvvvvv
                    // opus.dll
                    // libsodium.dll
                    // ^^^^^^^^^^^^^^^
                    // If you do not have these, this will not work.
                    while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0 && playingSong) // Read audio into our buffer, and keep a loop open while data is present
                    {
                        if (byteCount < blockSize)
                        {
                            // Incomplete Frame
                            for (int i = byteCount; i < blockSize; i++)
                                buffer[i] = 0;
                        }

                        _vClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
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
