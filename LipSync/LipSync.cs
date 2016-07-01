using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using GTA;

class LipSync : SimpleUI
{
    LipSync()
    {
        // monitor audio
        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        foreach (var d in devices) UI.Notify(String.Format("{0}, {1}", d.FriendlyName, d.State));
        MMDevice device = null;
        if (devices.Count > 1) device = devices[1];
        var playing = false;
        int count = 0;
        Tick += (sender, e) =>
        {
            if (device != null)
            {
                if (count < 10)
                {
                    count++;
                }
                else
                {
                    count = 0;
                    var level = device.AudioMeterInformation.MasterPeakValue;
                    if (playing && level < 0.2)
                    {
                        playing = false;
                        UI.Notify("stop talking");
                    }
                    if (!playing && level > 0.2)
                    {
                        playing = true;
                        UI.Notify("start talking");
                    }
                }
            }
        };
    }
}
