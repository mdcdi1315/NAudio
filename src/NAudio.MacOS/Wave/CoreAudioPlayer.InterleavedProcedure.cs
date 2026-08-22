
using System;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private sealed class InterleavedProcedure : PlayerProcedure
    {
        public InterleavedProcedure(AudioDevice dev) : base(dev) { }

        // Index of the one buffer this procedure is allowed to fill, matching
        // the single stream enabled through SetStreamUsage. The output
        // AudioBufferList always carries a buffer per stream of the device,
        // disabled streams included, and the HAL discards anything written
        // into those - so filling them all would consume the source once per
        // stream and throw away everything but this one.
        public uint EnabledBufferIndex { get; set; }

        protected override bool ProvideData(uint cBuffers, nint outOutputData, IPlayerSource source)
        {
            AudioBuffer buffer;
            for (uint I = 0; I < cBuffers; I++)
            {
                buffer = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
                if (buffer.mData == IntPtr.Zero)
                {
                    // Unused stream, move to the next one
                    continue;
                }
                if (I != EnabledBufferIndex)
                {
                    // A stream we did not enable. Silence it rather than
                    // leaving whatever the HAL last had in there.
                    buffer.GetSpan().Clear();
                    continue;
                }
                int read;
                Span<byte> allocatedSpan = buffer.GetSpan();
                while (allocatedSpan.Length > 0)
                {
                    read = source.Read(allocatedSpan);
                    if (read == 0) { return true; }
                    allocatedSpan = allocatedSpan.Slice(read);
                }
            }
            return false;
        }
    }
}