using System;
using System.IO;
using System.Text;

// TCP chunks are not messages. Decode UTF-8 only after the newline is complete.
public sealed class LineMessageBuffer
{
    private readonly byte[] pending;
    private readonly UTF8Encoding encoding = new UTF8Encoding(false, true);
    private int used;
    public LineMessageBuffer(int maxLineBytes = 65536)
    {
        if (maxLineBytes < 1) throw new ArgumentOutOfRangeException("maxLineBytes");
        pending = new byte[maxLineBytes];
    }
    public void Append(byte[] bytes, int count, Action<string> receive)
    {
        if (bytes == null || receive == null) throw new ArgumentNullException();
        if (count < 0 || count > bytes.Length) throw new ArgumentOutOfRangeException("count");
        int offset = 0;
        while (offset < count)
        {
            int newline = Array.IndexOf(bytes, (byte)'\n', offset, count - offset);
            int length = (newline < 0 ? count : newline) - offset;
            if (length > pending.Length - used) throw new InvalidDataException("Server message exceeds receive limit.");
            Buffer.BlockCopy(bytes, offset, pending, used, length);
            used += length;
            if (newline < 0) return;
            string message = encoding.GetString(pending, 0, used).Trim();
            used = 0;
            if (message.Length != 0) receive(message);
            offset = newline + 1;
        }
    }
}
