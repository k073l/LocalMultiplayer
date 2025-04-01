using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using AudioStreamer;
using AudioStreamer.CliTest;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;

LogMemoryUsage();

//var testStream = new TestStream(8);
var testStream = new MediaFoundationAudioStream("https://stream.simulatorradio.com", resetReaderAtEof: true);
//var testStream = new MediaFoundationAudioStream("https://onlinetestcase.com/wp-content/uploads/2023/06/2-MB-MP3.mp3", resetReaderAtEof: false);
testStream.Start();
AudioStreamReader[] readers =
[
    testStream.CreateReader(),
    testStream.CreateReader()
];

Span<byte> buffer1 = stackalloc byte[1024];
Span<byte> buffer2 = stackalloc byte[1024];

LogMemoryUsage();

var file = File.Create("test.pcm");
var file2 = File.Create("test2.pcm");
for (int i = 0; i < 100000; i++)
{
    int numRead = readers[0].Read(buffer1);
    readers[1].Read(buffer2);

    Debug.Assert(buffer1.SequenceEqual(buffer2));

    if (numRead == 0)
        break;

    file.Write(buffer1.Slice(0, numRead));
    file2.Write(buffer2.Slice(0, numRead));
}

LogMemoryUsage();

string ReaderToString(AudioStreamReader reader) => $"{reader.ParentBufferIteration}-{reader.ParentBufferPosition}";

string SpanToString<T>(ReadOnlySpan<T> span)
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

void LogMemoryUsage()
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var memUsage = Process.GetCurrentProcess().PrivateMemorySize64;
    Console.WriteLine($"Memory usage: {memUsage / 1024d / 1024d} MB");
}

/*var sw = Stopwatch.StartNew();
using var audioSource = new MediaFoundationAudioStream("https://stream.simulatorradio.com", resetReaderAtEof: true);
audioSource.Start();
sw.Stop();

Console.WriteLine($"Loaded in {sw.ElapsedMilliseconds}ms");

//Memory<byte> buffer = new Memory<byte>(new byte[audioSource.WaveFormat.AverageBytesPerSecond / 2]);
WaveBuffer buffer = new WaveBuffer(new byte[audioSource.WaveFormat!.AverageBytesPerSecond / 2]);
var file = File.Create("test.pcm");

while (true)
{
    int bytesRead = audioSource.Read(buffer.ByteBuffer);
    Span<byte> bytes = buffer.ByteBuffer.AsSpan().Slice(0, bytesRead);

    Console.WriteLine($"Read {bytes.Length} bytes");

    if (bytes.Length == 0)
    {
        break;
    }

    float min = float.MaxValue;
    float max = float.MinValue;

    sw = Stopwatch.StartNew();
    for (int i = 0; i < bytes.Length; i += 4)
    {
        float sample = BitConverter.ToSingle(bytes.Slice(i, 4));

        min = Math.Min(min, sample);
        max = Math.Max(max, sample);
    }
    sw.Stop();

    Console.WriteLine($"Min: {min}, Max: {max}, Time: {sw.Elapsed.TotalMilliseconds}ms");

    file.Write(bytes);
}

audioSource.Dispose();
file.Dispose();*/
