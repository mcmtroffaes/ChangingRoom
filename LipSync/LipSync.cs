using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using System.Diagnostics;

class LipSync : Script
{
    public double[] player_values;
    public Stopwatch stopwatch = new Stopwatch();

    private double[] ReadWav(string location)
    {
        var filestream = new System.IO.FileStream(location, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        var reader = new System.IO.BinaryReader(filestream);
        int chunk_id = reader.ReadInt32();
        if (chunk_id != 0x46464952) // "RIFF"
        {
            UI.Notify("invalid wav header - RIFF id not found");
            return null;
        }
        int chunk_size = reader.ReadInt32();
        int format = reader.ReadInt32();
        if (format != 0x45564157) // "WAVE"
        {
            UI.Notify("invalid wav header - WAVE id not found");
            return null;
        }
        int subchunk1_id = reader.ReadInt32();
        if (subchunk1_id != 0x20746d66) // "fmt "
        {
            UI.Notify("invalid wav header - fmt id not found");
            return null;
        }
        int subchunk1_size = reader.ReadInt32();
        if (subchunk1_size != 16 && subchunk1_size != 18)
        {
            UI.Notify("invalid wav header - bad subchunk1 size");
            return null;
        }
        int audio_format = reader.ReadInt16();
        if (audio_format != 1) // for PCM (uncompressed)
        {
            UI.Notify("only PCM (uncompressed) 16-bit wav format is supported");
            return null;
        }
        int num_channels = reader.ReadInt16();
        int sample_rate = reader.ReadInt32();
        int byte_rate = reader.ReadInt32();
        int block_align = reader.ReadInt16();
        int bits_per_sample = reader.ReadInt16();
        if (bits_per_sample != 16) // we will only work with 16 bit data
        {
            UI.Notify("only PCM (uncompressed) 16-bit wav format is supported");
            return null;
        }
        Debug.Assert(byte_rate == sample_rate * num_channels * (bits_per_sample / 8));
        if (subchunk1_size == 18)
        {
            int extra_size = reader.ReadInt16();
            reader.ReadBytes(extra_size);
        }
        int subchunk2_id;
        int subchunk2_size;
        while (true)
        {
            subchunk2_id = reader.ReadInt32();
            subchunk2_size = reader.ReadInt32();
            if (subchunk2_id == 0x61746164) break; // "data"
            reader.ReadBytes(subchunk2_size);
        }
        int step = 2 * ((byte_rate * Interval) / 2000); // ensure step is multiple of 2
        var result = new double[subchunk2_size / step];
        var index = 0;
        for (int pos = 0; pos + step < subchunk2_size; pos += step)
        {
            var buffer = reader.ReadBytes(step);
            var sample_buffer = new short[step / 2];
            Buffer.BlockCopy(buffer, 0, sample_buffer, 0, step);
            result[index++] = sample_buffer.Select(x => Math.Abs(x)).Max() / 32768.0;
        }
        return result;
    }

    public bool IsScriptKeyPressed(KeyEventArgs e)
    {
        return (e.KeyCode == Keys.J);
    }

    public LipSync() : base()
    {
        Interval = 100;
        var player = new SoundPlayer();
        var playing = false;
        KeyUp += (sender, e) =>
        {
            if (IsScriptKeyPressed(e))
            {
                stopwatch.Stop();
                player.Stop();
                var location = "D:\\vocals.wav"; // your file here
                player.SoundLocation = location;
                player_values = ReadWav(location);
                if (player_values != null)
                {
                    player.Load();
                    player.Play();
                    stopwatch.Restart();
                }
            }
        };
        Tick += (sender, e) =>
        {
            {
                if (!stopwatch.IsRunning) return;
                double level = 0.0;
                var player_index = (int)(stopwatch.ElapsedMilliseconds / Interval);
                if (player_index < player_values.Length)
                {
                    level = player_values[player_index];
                }
                else
                {
                    player.Stop();
                    stopwatch.Stop();
                }
                if (playing && level < 0.1)
                {
                    playing = false;
                    //UI.Notify(String.Format("{0} - stop talking", player_index));
                    Function.Call(Hash.STOP_ANIM_TASK, Game.Player.Character.Handle, "mp_facial", "mic_chatter", -2.0f);
                }
                else if (!playing && level > 0.1)
                {
                    playing = true;
                    //UI.Notify(String.Format("{0} - start talking", player_index));
                    Function.Call(Hash.TASK_PLAY_ANIM, Game.Player.Character.Handle, "mp_facial", "mic_chatter", 8.0f, -2.0f, -1, 33, 0.0f, 0, 0, 0);
                }
            }
        };
    }
}
