using System.Buffers.Binary;

ref struct BigEndianWriter
{
    private Span<byte> _span;
    private int _pos;

    public BigEndianWriter(Span<byte> span)
    {
        _span = span;
        _pos = 0;
    }

    public void SetPosition(int pos) => _pos = pos;

    public void WriteByte(byte value) => _span[_pos++] = value;

    public void WriteInt16(short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(_span.Slice(_pos, 2), value);
        _pos += 2;
    }

    public void WriteInt32(int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(_span.Slice(_pos, 4), value);
        _pos += 4;
    }

    public void WriteInt64(long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(_span.Slice(_pos, 8), value);
        _pos += 8;
    }

    public void WriteSingle(float value)
    {
        BinaryPrimitives.WriteSingleBigEndian(_span.Slice(_pos, 4), value);
        _pos += 4;
    }

    public void Skip(int count) => _pos += count;

    public int Position => _pos;
}