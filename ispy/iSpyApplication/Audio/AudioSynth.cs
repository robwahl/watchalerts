﻿using iSpyApplication.Audio.streams;
using iSpyApplication.Audio.talk;
using iSpyApplication.Controls;
using NAudio.Wave;
using System.IO;
using System.Linq;
using System.Threading;

namespace iSpyApplication.Audio
{
    internal static class AudioSynth
    {
        public static void Play(string fileName, CameraWindow cw)
        {
            var t = new Thread(() => SynthToCam(fileName, cw));
            t.Start();
        }

        private static void SynthToCam(string fileName, CameraWindow cw)
        {
            using (var waveStream = new MemoryStream())
            {
                //write some silence to the stream to allow camera to initialise properly
                var silence = new byte[1 * 22050];
                waveStream.Write(silence, 0, silence.Count());

                //read in and convert the wave stream into our format
                using (var reader = new WaveFileReader(fileName))
                {
                    var newFormat = new WaveFormat(11025, 16, 1);
                    byte[] buff = new byte[22050];

                    using (var conversionStream = new WaveFormatConversionStream(newFormat, reader))
                    {
                        do
                        {
                            int i = conversionStream.Read(buff, 0, 22050);
                            waveStream.Write(buff, 0, i);
                            if (i < 22050)
                                break;
                        } while (true);
                    }
                }

                //write some silence to the stream to allow camera to end properly
                waveStream.Write(silence, 0, silence.Count());

                waveStream.Seek(0, SeekOrigin.Begin);

                var ds = new DirectStream(waveStream) { RecordingFormat = new WaveFormat(11025, 16, 1) };
                var talkTarget = TalkHelper.GetTalkTarget(cw.Camobject, ds);

                ds.Start();
                talkTarget.Start();
                while (ds.IsRunning)
                {
                    Thread.Sleep(100);
                }
                ds.Stop();
                talkTarget.Stop();
                talkTarget = null;
                ds = null;

                waveStream.Close();
            }
        }
    }
}