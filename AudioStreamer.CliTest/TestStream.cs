using System;
using NAudio.Wave;

namespace AudioStreamer.CliTest;

public class TestStream(uint bufferSize) : AudioStream(bufferSize)
{
    public override bool Started => true;

    public override WaveFormat WaveFormat => new WaveFormat();

    private byte count = 0;

    public override void Start()
    {
    }

    public override void Stop()
    {
    }

    protected override int ReadInternal(byte[] bufferArray, int arrayOffset, int arrayCount)
    {
        for (int i = 0; i < arrayCount; i++)
        {
            if (count == byte.MaxValue)
                count = 0;

            bufferArray[i + arrayOffset] = count++;
        }

        return arrayCount;
    }

    public override void PrepareForReading()
    {
    }
}
