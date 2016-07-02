using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NativeUI;
using GTA;
using System.Diagnostics;

class LipSync : SimpleUI
{
    public int[] player_values;
    public Stopwatch stopwatch = new Stopwatch();

    private int[] ReadWav(string location)
    {
        var filestream = new System.IO.FileStream(location, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        var reader = new System.IO.BinaryReader(filestream);
        int chunk_id = reader.ReadInt32();
        Debug.Assert(chunk_id == 0x46464952); // "RIFF"
        int chunk_size = reader.ReadInt32();
        int format = reader.ReadInt32();
        Debug.Assert(format == 0x45564157); // "WAVE"
        int subchunk1_id = reader.ReadInt32();
        Debug.Assert(subchunk1_id == 0x20746d66); // "fmt "
        int subchunk1_size = reader.ReadInt32();
        Debug.Assert(subchunk1_size == 16 || subchunk1_size == 18);
        int audio_format = reader.ReadInt16();
        Debug.Assert(audio_format == 1); // for PCM (uncompressed)
        int num_channels = reader.ReadInt16();
        int sample_rate = reader.ReadInt32();
        int byte_rate = reader.ReadInt32();
        int block_aligh = reader.ReadInt16();
        int bits_per_sample = reader.ReadInt16();
        Debug.Assert(bits_per_sample == 16); // we will only work with 16 bit data
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
        var result = new int[subchunk2_size / step];
        var index = 0;
        for (int pos = 0; pos + step < subchunk2_size; pos += step)
        {
            var buffer = reader.ReadBytes(step);
            var sample_buffer = new short[step / 2];
            Buffer.BlockCopy(buffer, 0, sample_buffer, 0, step);
            result[index++] = sample_buffer.Select(x => Math.Abs(x)).Max();
        }
        return result;
    }

    public override bool IsScriptKeyPressed(KeyEventArgs e)
    {
        return (e.KeyCode == Keys.J);
    }

    public override UIMenu Menu()
    {
        var menu = new UIMenu("Lip Sync", "Main Menu");
        return menu;
    }

    public LipSync() : base()
    {
        var location = "D:\\vocals.wav"; // your file here
        var player = new SoundPlayer();
        player.SoundLocation = location;
        player_values = ReadWav(location);
        player.Load();
        player.Play();
        stopwatch.Restart();
        var playing = false;
        Tick += (sender, e) =>
        {
            {
                int level = 0;
                var player_index = (int)(stopwatch.ElapsedMilliseconds / Interval);
                if (player_index < player_values.Length)
                {
                    level = player_values[player_index];
                }
                else
                {
                    stopwatch.Stop();
                }
                //UI.Notify(level.ToString());
                if (playing && level < 500)
                {
                    playing = false;
                    UI.Notify(String.Format("{0} - stop talking", player_index));
                }
                else if (!playing && level > 500)
                {
                    playing = true;
                    UI.Notify(String.Format("{0} - start talking", player_index));
                }
            }
        };
    }
}
