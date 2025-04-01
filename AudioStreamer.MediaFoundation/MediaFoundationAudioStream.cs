using System;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

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
    public override bool Started => reader != null;

    private readonly string url = url;
    private readonly bool resetReaderAtEof = resetReaderAtEof;
    private MediaFoundationReader? reader;

    /// <summary>
    /// The audio format of the audio stream.
    /// </summary>
    public WaveFormat WaveFormat => reader?.WaveFormat ?? throw new InvalidOperationException("The stream has not been started.");

    public override int NumChannels => WaveFormat.Channels;

    public override int SampleRate => WaveFormat.SampleRate;

    public override int BitsPerSample => WaveFormat.BitsPerSample;

    private MediaFoundationReader CreateMFReader()
    {
        return new MediaFoundationReader(url, new MediaFoundationReader.MediaFoundationReaderSettings
        {
            RequestFloatOutput = true
        });
    }

    protected override int ReadInternal(Span<byte> outBuffer)
    {
        if (reader == null)
            throw new InvalidOperationException("The stream has not been started.");

        int numBytes;

        while (true)
        {
            numBytes = reader.Read(outBuffer);

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
        ResizeBuffer(WaveFormat.AverageBytesPerSecond * 2);
    }

    public override void Stop()
    {
        if (reader == null)
            return;

        reader.Dispose();
        reader = null;
    }
}
