using System;

namespace AudioStreamer.CliTest;

public class TestStream(uint bufferSize) : AudioStream(bufferSize)
{
    public override bool Started => true;

    private byte count = 0;

    public override void Start()
    {
    }

    public override void Stop()
    {
    }

    protected override int ReadInternal(Span<byte> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            if (count == byte.MaxValue)
                count = 0;

            buffer[i] = count++;
        }

        return buffer.Length;
    }
}
