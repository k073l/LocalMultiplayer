using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using AudioStreamer;
using AudioStreamer.CliTest;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;

internal class Program
{
    private static void Main(string[] args)
    {
        LogMemoryUsage();

        var testStream = new TestStream(8);
        /*var testStream = new MediaFoundationAudioStream("https://stream.simulatorradio.com", resetReaderAtEof: true)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: 44100, channels: 2),
        };*/
        //var testStream = new MediaFoundationAudioStream("https://onlinetestcase.com/wp-content/uploads/2023/06/2-MB-MP3.mp3", resetReaderAtEof: false);
        testStream.Start();
        AudioStreamReader[] readers =
        [
            testStream.CreateReader(),
            testStream.CreateReader()
        ];

        Span<byte> buffer1 = stackalloc byte[4];
        Span<byte> buffer2 = stackalloc byte[4];

        LogMemoryUsage();

        /*using var file = File.Create("test.pcm");
        using var file2 = File.Create("test2.pcm");
        while (true)
        {
            int numRead1 = readers[0].Read(buffer1);
            int numRead2 = readers[1].Read(buffer2);
            Assert(numRead1 == numRead2, "Read count should be equal");
            Assert(buffer1.Slice(0, numRead1).SequenceEqual(buffer2.Slice(0, numRead2)), "Buffers should be match");

            if (numRead1 == 0)
                break;

            file.Write(buffer1.Slice(0, numRead1));
            file2.Write(buffer2.Slice(0, numRead2));
        }*/

        for (int i = 0; i < 8; i++)
        {
            int numRead1 = readers[0].Read(buffer1);
            int numRead2 = readers[1].Read(buffer2);
            Assert(numRead1 == numRead2, "Read count should be equal");
            //Assert(buffer1.Slice(0, numRead1).SequenceEqual(buffer2.Slice(0, numRead2)), $"Buffers should be match at index {i}");

            readers[0].ReadExactly(buffer1);
        }

        testStream.Dispose();
        testStream = null;

        foreach (var reader in readers)
        {
            reader.Dispose();
        }

        LogMemoryUsage();
    }

    private static string SpanToString<T>(ReadOnlySpan<T> span)
    {
        var builder = new StringBuilder("[");

        for (int i = 0; i < span.Length; i++)
        {
            builder.Append(span[i]);

            if (i < span.Length - 1)
                builder.Append(", ");
        }

        builder.Append("]");
        return builder.ToString();
    }

    private static void LogMemoryUsage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memUsage = Process.GetCurrentProcess().PrivateMemorySize64;
        Console.WriteLine($"Memory usage: {memUsage / 1024d / 1024d} MB");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
