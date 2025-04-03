using System;
using System.IO;
using NAudio.Wave;

namespace AudioStreamer;

public class AudioStreamReader : Stream, IWaveProvider, ISampleProvider
{
    public readonly AudioStream AudioStream;

    internal bool IsFirstRead = true;

    private byte[] bufferArray;
    private Memory<byte> buffer;
    private int position;
    private int numBuffered;

    internal AudioStreamReader(AudioStream stream, int bufferSize)
    {
        AudioStream = stream;
        bufferArray = new byte[bufferSize];
        buffer = bufferArray.AsMemory();
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

        return ReadFromBufferAndAdvance(buffer.AsSpan(offset, count));
    }

    public int Read(float[] outBuffer, int offset, int count)
    {
        if (AudioStream.IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        if (outBuffer.Length < offset + count)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be less than or equal to the target array");

        if (buffer.Length < offset + count)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be less than or equal to the reader's buffer size");

        if (AudioStream.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            unsafe
            {
                fixed (float* bufferPtr = outBuffer)
                {
                    Span<byte> byteBuffer = new(bufferPtr + (offset * sizeof(float)), count * sizeof(float));
                    return ReadFromBufferAndAdvance(byteBuffer) / sizeof(float);
                }
            }
        }
        else
        {
            throw new NotImplementedException("Only IEEE float streams are supported");
        }
    }

    internal void ResizeBuffer(int size)
    {
        if (size <= buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than the current buffer size");

        // todo: implement reducing the size if needed. becomes a bit complicated due to circular buffer array

        Array.Resize(ref bufferArray, size);
        buffer = bufferArray.AsMemory();
    }

    internal void CopyToBuffer(Span<byte> bytes)
    {
        if (bytes.Length > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(bytes), "Bytes must be less than or equal to the reader's buffer size");

        int numCopied = 0;
        int writePosition = position + numBuffered;
        writePosition %= buffer.Length;

        while (numCopied < bytes.Length)
        {
            int bytesRemainingInBuffer = buffer.Length - position;
            int bytesToCopy = Math.Min(bytesRemainingInBuffer, bytes.Length - numCopied);

            bytes.Slice(numCopied, bytesToCopy).CopyTo(buffer.Span.Slice(writePosition, bytesToCopy));

            numCopied += bytesToCopy;
            numBuffered += bytesToCopy;
            writePosition += bytesToCopy;

            if (writePosition == buffer.Length)
                writePosition = 0;
        }
    }

    private void BufferIfNeeded(int count)
    {
        lock (AudioStream.ReaderBufferLock)
        {
            if (numBuffered >= count)
                return;

            int bytesNeeded = count - numBuffered;
            AudioStream.ReadFromSource(bytesNeeded);
        }
    }

    private int ReadFromBufferAndAdvance(Span<byte> outBuffer)
    {
        BufferIfNeeded(outBuffer.Length);

        if (numBuffered == 0)
            return 0;

        int totalRead = 0;

        while (totalRead < outBuffer.Length)
        {
            int bytesRemainingInBuffer = buffer.Length - position;
            int bytesToCopy = Math.Min(numBuffered, Math.Min(bytesRemainingInBuffer, outBuffer.Length - totalRead));

            buffer.Span.Slice(position, bytesToCopy).CopyTo(outBuffer.Slice(totalRead, bytesToCopy));
            totalRead += bytesToCopy;
            position += bytesToCopy;
            numBuffered -= bytesToCopy;

            if (position == buffer.Length)
                position = 0;

            if (numBuffered == 0)
                break;
        }

        return totalRead;
    }
}
