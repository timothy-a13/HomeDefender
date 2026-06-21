using System.Text;

namespace BlazorApp1.Data
{
    public class StringStream
    {
        private readonly Stream _stream;
        private readonly UnicodeEncoding _streamEncoding;

        public StringStream(Stream stream)
        {
            _stream = stream;
            _streamEncoding = new UnicodeEncoding();
        }

        public async ValueTask<string> ReadAsync(CancellationToken token)
        {
            byte[] buffer = new byte[2];
            await _stream.ReadAsync(buffer.AsMemory(0, 2), token);
            int len = buffer[0] * 256 + buffer[1], pos = 0;

            buffer = new byte[len];
            for (int i = 0; pos < len && i < 3; i++)
                pos += await _stream.ReadAsync(buffer.AsMemory(pos, len - pos), token);

            if (pos != len) throw new Exception("Massage Length Error.");

            return _streamEncoding.GetString(buffer);
        }

        public async Task<int> WriteAsync(string output, CancellationToken token)
        {
            byte[] buffer = _streamEncoding.GetBytes(output);
            int len = buffer.Length > ushort.MaxValue ? ushort.MaxValue : buffer.Length;

            _stream.Write(new byte[2] { (byte)(len / 256), (byte)(len & 255) }, 0, 2);

            await _stream.WriteAsync(buffer.AsMemory(0, len), token);
            await _stream.FlushAsync(token);

            return buffer.Length + 2;
        }
    }
}
