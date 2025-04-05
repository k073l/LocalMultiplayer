using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace AudioStreamer;

public abstract class AudioStream : IDisposable, IWaveProvider, ISampleProvider
{
    public abstract bool Started { get; }

    public abstract bool StreamAvailable { get; }

    public abstract WaveFormat WaveFormat { get; }

    public bool IsDisposed { get; private set; }

    public abstract void Start();

    public abstract void Stop();

    public abstract void WarmupReader();

    protected abstract int ReadInternal(byte[] buffer, int offset, int count);

    /// <summary>
    /// Used when reading floats but they need to be converted to bytes
    /// </summary>
    private byte[]? byteBuffer;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        if (!Started)
            throw new InvalidOperationException("The stream has not been started.");

        return ReadInternal(buffer, offset, count);
    }

    public int Read(float[] floatBuffer, int offset, int count)
    {
        if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            byteBuffer ??= new byte[floatBuffer.Length * sizeof(float)];

            if (byteBuffer.Length * sizeof(float) < floatBuffer.Length)
            {
                Array.Resize(ref byteBuffer, floatBuffer.Length * sizeof(float));
            }

            var numBytes = ReadInternal(byteBuffer, 0, count * sizeof(float));

            Span<float> floatSpan = MemoryMarshal.Cast<byte, float>(byteBuffer.AsSpan(0, numBytes));
            floatSpan.CopyTo(floatBuffer.AsSpan(offset));

            return numBytes / sizeof(float);
        }
        else
        {
            throw new NotImplementedException("Only IEEE float streams are supported");
        }
    }
}
