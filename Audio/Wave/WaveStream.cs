﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace Audio
{
    class WaveStream : Stream
    {                     
        private SampleHandler callback;
        private WAVEFORMATEX format;
        private WaveIn[] waveIn;
        private WaveOut[] waveOut;
        
        private Thread proc;

        public override double SampleRate { get { return format.nSamplesPerSec; } }

        private int buffer;

        public WaveStream(SampleHandler SampleCallback, WaveChannel[] Input, WaveChannel[] Output, double Latency) : base(Input, Output)
        {
            callback = SampleCallback;

            int Rate = 48000;
            int Bits = 16;
            int Channels = 1;
            format = new WAVEFORMATEX(Rate, Bits, Channels);

            buffer = (int)Math.Ceiling(Latency / 2 * Rate * Channels);
            waveIn = Input.Select(i => new WaveIn(i.Device, format, buffer)).ToArray();
            waveOut = Output.Select(i => new WaveOut(i.Device, format, buffer)).ToArray();

            proc = new Thread(new ThreadStart(Proc));
            proc.Start();
        }
        private volatile bool stop = false;

        public override void Stop()
        {
            stop = true;
            foreach (WaveIn i in waveIn)
                i.Stop();
            foreach (WaveOut i in waveOut)
                i.Stop();

            proc.Join();
        }

        private void Proc()
        {
            WaitHandle[] events = waveIn.Select(i => i.Callback).Concat(waveOut.Select(i => i.Callback)).ToArray();

            while (!stop)
            {
                //if (!WaitHandle.WaitAll(events, 100))
                //    continue;

                // Read from the inputs.
                SampleBuffer[] input = new SampleBuffer[waveIn.Length];
                for (int i = 0; i < waveIn.Length; ++i)
                {
                    WaveInBuffer b = waveIn[i].GetBuffer();
                    if (b == null)
                        return;
                    using (RawLock l = new RawLock(b.Samples, false, true))
                        ConvertSamples(b.Data, format, l, l.Count);
                    b.Record();
                    input[i] = b.Samples;
                }

                // Get an available buffer from the outputs.
                SampleBuffer[] output = new SampleBuffer[waveOut.Length];
                for (int i = 0; i < waveOut.Length; ++i)
                {
                    WaveOutBuffer b = waveOut[i].GetBuffer();
                    if (b == null)
                        return;
                    output[i] = b.Samples;
                }

                // Call the callback.
                callback(buffer, input, output, format.nSamplesPerSec);

                // Play the results.
                for (int i = 0; i < output.Length; ++i)
                {
                    WaveOutBuffer b = (WaveOutBuffer)output[i].Tag;
                    using (RawLock l = new RawLock(b.Samples, true, false))
                        ConvertSamples(l, b.Data, format, l.Count);
                    b.Play();
                }
            }
        }
        
        private static void ConvertSamples(IntPtr In, WAVEFORMATEX Format, IntPtr Out, int Count)
        {
            switch (Format.wBitsPerSample)
            {
                case 16: Util.LEi16ToLEf64(In, Out, Count); break;
                case 32: Util.LEi32ToLEf64(In, Out, Count); break;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
        }

        private static void ConvertSamples(IntPtr In, IntPtr Out, WAVEFORMATEX Format, int Count)
        {
            switch (Format.wBitsPerSample)
            {
                case 16: Util.LEf64ToLEi16(In, Out, Count); break;
                case 32: Util.LEf64ToLEi32(In, Out, Count); break;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
        }
    }
}
