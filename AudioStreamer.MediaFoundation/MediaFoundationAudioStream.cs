using System;
using NAudio.Wave;

namespace AudioStreamer.MediaFoundation;

/// <summary>
/// Audio source that supports any audio format that MediaFoundation supports.
///
/// Supported audio formats include aac, mp3, wav, among others.
///
/// A full list of supported file formats can be found here:
/// https://learn.microsoft.com/en-us/windows/win32/medfound/supported-media-formats-in-media-foundation
/// </summary>
public class MediaFoundationAudioStream(string url, bool resetReaderAtEof) : AudioStream(bufferSize: 512000)
{
    public WaveFormat? ResampleFormat { get; set; }

    public override bool Started => reader != null;

    private readonly string url = url;
    private readonly bool resetReaderAtEof = resetReaderAtEof;
    private MediaFoundationReader? reader;
    private MediaFoundationResampler? resampler;

    /// <summary>
    /// The audio format of the audio stream.
    /// </summary>
    public override WaveFormat WaveFormat => resampler?.WaveFormat ?? reader?.WaveFormat ?? throw new InvalidOperationException("The stream has not been started.");

    private MediaFoundationReader CreateMFReader()
    {
        return new MediaFoundationReader(url, new MediaFoundationReader.MediaFoundationReaderSettings
        {
            RequestFloatOutput = true
        });
    }

    private MediaFoundationResampler CreateMFResampler()
    {
        if (reader == null)
            throw new InvalidOperationException("The stream has not been started.");

        if (ResampleFormat == null)
            throw new InvalidOperationException("The resample format has not been set.");

        return new MediaFoundationResampler(reader, ResampleFormat);
    }

    public override void WarmupReader()
    {
        if (reader == null)
            throw new InvalidOperationException("The stream has not been started.");

        Span<byte> buffer = new byte[WaveFormat.BitsPerSample / 8 * WaveFormat.Channels];
        reader.Read(buffer);
    }

    protected override int ReadInternal(byte[] outBuffer, int offset, int count)
    {
        if (reader == null)
            throw new InvalidOperationException("The stream has not been started.");

        int numBytes;

        while (true)
        {
            if (resampler != null)
            {
                numBytes = resampler.Read(outBuffer, offset, count);
            }
            else
            {
                numBytes = reader.Read(outBuffer, offset, count);
            }

            if (numBytes == 0 && resetReaderAtEof)
            {
                Console.WriteLine("Resetting reader at EOF");

                Stop();
                Start();
                continue;
            }

            break;
        }

        return numBytes;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Stop();
        }
    }

    public override void Start()
    {
        if (reader != null)
            return;

        reader = CreateMFReader();

        if (ResampleFormat != null)
        {
            resampler = CreateMFResampler();
        }

        ResizeBuffers(WaveFormat.AverageBytesPerSecond * 2);
    }

    public override void Stop()
    {
        if (reader == null)
            return;

        reader.Dispose();
        resampler?.Dispose();
        reader = null;
        resampler = null;
    }
}
