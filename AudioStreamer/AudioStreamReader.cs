using System;
using System.IO;
using NAudio.Wave;

namespace AudioStreamer;

public class AudioStreamReader : Stream, IWaveProvider, ISampleProvider
{
    public readonly AudioStream AudioStream;

    internal int ParentBufferPosition;
    internal uint ParentBufferIteration;
    internal bool IsFirstRead = true;

    internal AudioStreamReader(AudioStream stream)
    {
        AudioStream = stream;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public WaveFormat WaveFormat => AudioStream.WaveFormat;

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (AudioStream.IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        return AudioStream.Read(this, buffer.AsSpan(offset, count));
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (AudioStream.IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        if (buffer.Length < offset + count)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (AudioStream.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            unsafe
            {
                fixed (float* bufferPtr = buffer)
                {
                    Span<byte> byteBuffer = new(bufferPtr + (offset * sizeof(float)), count * sizeof(float));
                    return AudioStream.Read(this, byteBuffer) / sizeof(float);
                }
            }
        }
        else
        {
            throw new NotImplementedException("Only IEEE float streams are supported");
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            AudioStream.RemoveReader(this);
        }
    }
}
