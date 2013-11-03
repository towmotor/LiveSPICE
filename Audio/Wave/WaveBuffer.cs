﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Audio
{
    /// <summary>
    /// Helper to manage the memory associated with a WAVEHDR.
    /// </summary>
    class WaveBuffer : IDisposable
    {
        protected GCHandle handle;
        protected WAVEHDR header;
        protected GCHandle pin;
        protected int size;

        protected bool disposed = false;

        public IntPtr Data { get { return header.lpData; } }
        public int Size { get { return size; } }

        public bool Done { get { return (header.dwFlags & WaveHdrFlags.WHDR_DONE) != 0; } }

        private SampleBuffer samples;
        public SampleBuffer Samples { get { return samples; } }
        
        public WaveBuffer(WAVEFORMATEX Format, int Count)
        {
            handle = GCHandle.Alloc(this);

            size = BlockAlignedSize(Format, Count);
            samples = new SampleBuffer(Count) { Tag = this };

            header = new WAVEHDR();
            header.lpData = Marshal.AllocHGlobal(size);
            header.dwUser = (IntPtr)handle;
            header.dwBufferLength = (uint)size;
            header.dwFlags = 0;
            pin = GCHandle.Alloc(header, GCHandleType.Pinned);
        }

        ~WaveBuffer() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        public virtual void Dispose(bool Disposing)
        {
            if (disposed) return;

            if (pin.IsAllocated)
                pin.Free();

            if (header.lpData != IntPtr.Zero)
                Marshal.FreeHGlobal(header.lpData);

            if (handle.IsAllocated)
                handle.Free();

            disposed = true;
        }
        
        private static int BlockAlignedSize(WAVEFORMATEX Format, int Count)
        {
            int align = Format.nBlockAlign;
            int size = Count * Format.wBitsPerSample / 8;

            return size + (align - (size % align)) % align;
        }
    }
}
