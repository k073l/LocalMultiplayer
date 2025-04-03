using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace AudioStreamer;

public abstract class AudioStream : IDisposable, IWaveProvider
{
    public abstract bool Started { get; }

    public abstract WaveFormat WaveFormat { get; }

    internal int Position => position;
    internal uint Iteration => iteration;

    public bool IsDisposed { get; private set; }

    public int NumReaders => readers.Count;


    private readonly List<AudioStreamReader> readers = [];

    /// <summary>
    /// Main buffer that readers read from
    /// </summary>
    private Memory<byte> mainBuffer;

    /// <summary>
    /// Backing array for main buffer
    /// </summary>
    private byte[] mainBufferArray;

    /// <summary>
    /// End position of the last written data in main buffer
    /// </summary>
    private int position;

    /// <summary>
    /// Iteration counter, aka how many times the position in the main buffer has restarted from 0.
    /// </summary>
    private uint iteration;

    private readonly object readLock = new object();

    public AudioStream(uint bufferSize)
    {
        mainBufferArray = new byte[bufferSize];
        mainBuffer = new Memory<byte>();
    }

    public abstract void Start();

    public abstract void Stop();

    public AudioStreamReader CreateReader()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        var reader = new AudioStreamReader(this);
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

    internal int Read(AudioStreamReader reader, Span<byte> buffer)
    {
        if (reader.AudioStream != this)
            throw new ArgumentException("Reader does not belong to this stream");

        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        lock (readLock)
        {
            if (reader.IsFirstRead)
            {
                reader.IsFirstRead = false;

                var otherReader = readers.Count > 1 ? GetMostProgressedReader() : null;

                if (otherReader != null)
                {
                    reader.ParentBufferIteration = otherReader.ParentBufferIteration;
                    reader.ParentBufferPosition = otherReader.ParentBufferPosition;
                    Console.WriteLine("Copying position from other reader");
                }
                else
                {
                    // Set the initial position and iteration for the reader
                    if (buffer.Length < position)
                    {
                        // Desired bytes is less than available bytes in the current iteration
                        reader.ParentBufferIteration = iteration;
                        reader.ParentBufferPosition = position - buffer.Length;
                        Console.WriteLine("Setting position to end of current iteration minus desired bytes");
                    }
                    else
                    {
                        if (iteration > 0)
                        {
                            reader.ParentBufferIteration = iteration - 1;
                            reader.ParentBufferPosition = mainBuffer.Length - buffer.Length + position;
                            Console.WriteLine("Setting position to end of previous iteration plus desired bytes");
                        }
                        else
                        {
                            reader.ParentBufferIteration = 0;
                            reader.ParentBufferPosition = 0;
                            Console.WriteLine("Setting position to start of main buffer");
                        }
                    }
                }

                Console.WriteLine($"New reader started at iteration/pos {reader.ParentBufferIteration}/{reader.ParentBufferPosition}");
            }

            if (iteration - reader.ParentBufferIteration > 1)
                throw new ArgumentException($"Reader is too far behind the main buffer. (Reader iteration/pos: {reader.ParentBufferIteration}/{reader.ParentBufferPosition}, Main buffer iteration/pos: {iteration}/{position})");

            if (buffer.Length == 0)
                return 0;

            int readBytes = 0;

            while (readBytes < buffer.Length)
            {
                if (iteration - reader.ParentBufferIteration == 1)
                {
                    // Reader is 1 iteration behind, read from the end of the buffer
                    int numToCopy = Math.Min(buffer.Length, mainBuffer.Length - reader.ParentBufferPosition);
                    mainBuffer.Slice(reader.ParentBufferPosition, numToCopy).Span.CopyTo(buffer.Slice(readBytes, numToCopy));

                    readBytes += numToCopy;
                    reader.ParentBufferPosition += numToCopy;
                }
                else
                {
                    // Reader is on current iteration, read from the start of the buffer
                    if (reader.ParentBufferPosition < position)
                    {
                        int numToCopy = Math.Min(position - reader.ParentBufferPosition, buffer.Length - readBytes);
                        mainBuffer.Slice(reader.ParentBufferPosition, numToCopy).Span.CopyTo(buffer.Slice(readBytes, numToCopy));

                        readBytes += numToCopy;
                        reader.ParentBufferPosition += numToCopy;
                    }
                    else
                    {
                        // Reader is at the end of available data, read more data into buffer
                        int numBytesReadIntoBuffer = ReadIntoBuffer(buffer.Length - readBytes);

                        if (numBytesReadIntoBuffer == 0)
                            break;

                        continue;
                    }
                }

                if (reader.ParentBufferPosition == mainBuffer.Length)
                {
                    reader.ParentBufferIteration += 1;
                    reader.ParentBufferPosition = 0;
                }
            }

            return readBytes;
        }
    }

    private AudioStreamReader? GetMostProgressedReader()
    {
        if (readers.Count == 0)
            return null;

        return readers.OrderByDescending(x => x.ParentBufferIteration).ThenByDescending(x => x.ParentBufferPosition).FirstOrDefault();
    }

    private int ReadIntoBuffer(int numBytes)
    {
        if (numBytes >= mainBuffer.Length)
            throw new ArgumentOutOfRangeException(nameof(numBytes), "Can't read more than buffer size.");

        int totalRead = 0;

        while (numBytes > 0)
        {
            int numBytesRead = ReadInternal(mainBufferArray, position, Math.Min(numBytes, mainBuffer.Length - position));

            if (numBytesRead == 0)
                break;

            totalRead += numBytesRead;
            numBytes -= numBytesRead;

            if (position + numBytesRead == mainBuffer.Length)
            {
                iteration += 1;
                position = 0;
            }
            else
            {
                position += numBytesRead;
            }
        }

        return totalRead;
    }

    protected void ResizeBuffer(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        if (size == mainBuffer.Length)
            return;

        var newBufferArray = new byte[size];
        var newBuffer = new Memory<byte>(newBufferArray);
        mainBuffer.Span.Slice(0, Math.Min(position, size)).CopyTo(newBuffer.Span);
        mainBufferArray = newBufferArray;
        mainBuffer = newBuffer;

        foreach (var reader in readers)
        {
            reader.ParentBufferPosition = Math.Min(reader.ParentBufferPosition, position);
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

        mainBuffer = null;
        IsDisposed = true;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Use AudioStreamReader instead.");
    }
}
