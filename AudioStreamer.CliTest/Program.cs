using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using AudioStreamer;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;

internal class Program
{
    private static void Main(string[] args)
    {
        LogMemoryUsage();

        using var file = File.Create("test.pcm");
        using var stream = new MediaFoundationAudioStream("https://stream.simulatorradio.com", resetReaderAtEof: false)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: 44100, channels: 2),
        };
        stream.Start();
        var buffer = new float[1024];

        while (true)
        {
            int numRead = stream.Read(buffer, 0, buffer.Length);

            if (numRead == 0)
                break;

            unsafe
            {
                fixed (float* ptr = buffer)
                {
                    var byteBuffer = new Span<byte>((byte*)ptr, numRead * sizeof(float));
                    file.Write(byteBuffer);
                }
            }
        }

        LogMemoryUsage();
    }

    private static void LogMemoryUsage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memUsage = Process.GetCurrentProcess().PrivateMemorySize64;
        Console.WriteLine($"Memory usage: {memUsage / 1024d / 1024d} MB");
    }
}
