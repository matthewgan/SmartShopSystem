using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using System.IO;
using NAudio.Wave;


public class BaiduSpeech
{
    public string APP_ID = "11232649";
    private string API_KEY = "UqurNdBHezavskGvjWlGugpq";
    private string SECRET_KEY = "ac4UYeO4Tg2a9YwsaLuE0DsDxHyvR2Id";
    Baidu.Aip.Speech.Tts client;

    public BaiduSpeech()
    {
        client = new Baidu.Aip.Speech.Tts(API_KEY, SECRET_KEY);
    }

    public string Tts(string textinput)
    {
        var options = new Dictionary<string, object>()
            {
                {"spd", 5},
                {"vol", 6},
                {"per", 4}
            };

        var result = client.Synthesis(textinput, options);

        if (result.ErrorCode == 0)
        {
            string path = "welcome.mp3";
            File.WriteAllBytes(path, result.Data);
            return path;
        }
        else
            return string.Empty;
    }

    public void Play(string filename)
    {
        if (filename != string.Empty)
        {
            using (var audioFile = new AudioFileReader(filename))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
    }

    public void Tts2Play(string textinput)
    {
        Task t = new Task(() => { Play(Tts(textinput)); }
        );
        t.Start();
        t.Wait();
    }

    /// <summary>
    /// 调用windowsmedia播放WAV声音文件
    /// </summary>
    /// <param name="path"></param>
    public void PlaySound(string path)
    {
        SoundPlayer player = new SoundPlayer();
        player.SoundLocation = path;
        player.Load();
        player.Play();
    }
}

