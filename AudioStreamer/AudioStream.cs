using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace AudioStreamer;

public abstract class AudioStream : IDisposable
{
    public abstract bool Started { get; }

    public abstract WaveFormat WaveFormat { get; }

    public bool IsDisposed { get; private set; }

    public int NumReaders => readers.Count;


    private readonly List<AudioStreamReader> readers = [];

    internal readonly object ReaderBufferLock = new object();
    private int bufferSize;
    private Memory<byte> readBuffer;
    private byte[] readBufferArray;

    public AudioStream(int bufferSize)
    {
        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        this.bufferSize = bufferSize;
        readBufferArray = new byte[bufferSize];
        readBuffer = readBufferArray.AsMemory();
    }

    public abstract void Start();

    public abstract void Stop();

    public AudioStreamReader CreateReader()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        var reader = new AudioStreamReader(this, bufferSize);
        readers.Add(reader);

        Console.WriteLine($"Added reader, new total readers: {readers.Count}");

        return reader;
    }

    public bool RemoveReader(AudioStreamReader reader)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        if (reader.AudioStream != this)
            throw new ArgumentException("Reader does not belong to this stream");

        if (readers.Remove(reader))
        {
            Console.WriteLine($"Removed reader, new total readers: {readers.Count}");
            return true;
        }

        return false;
    }

    public abstract void WarmupReader();

    protected abstract int ReadInternal(byte[] buffer, int offset, int count);

    internal int ReadFromSource(int count)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        if (count > bufferSize)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be less than or equal to the buffer size");

        int bytesRead = ReadInternal(readBufferArray, 0, count);

        if (bytesRead == 0)
            return 0;

        foreach (var reader in readers)
        {
            reader.CopyToBuffer(readBuffer.Slice(0, bytesRead).Span);
        }

        return bytesRead;
    }

    protected void ResizeBuffers(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        bufferSize = size;

        foreach (var reader in readers)
        {
            reader.ResizeBuffer(size);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        if (disposing)
        {
            foreach (var reader in readers)
            {
                reader.Dispose();
            }

            readers.Clear();
        }

        IsDisposed = true;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Use AudioStreamReader instead.");
    }
}
