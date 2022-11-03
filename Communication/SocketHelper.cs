using System.Net.Sockets;

namespace Communication
{
    public class SocketHelper
    {
        private readonly NetworkStream _networkStream;

        public SocketHelper(NetworkStream networkStream)
        {
            _networkStream = networkStream;
        }

        public async Task Send(byte[] data)
        {
            int offset = 0;
            // while (offset < data.Length)
            // {
                await _networkStream.WriteAsync(
                    data,
                    offset,
                    data.Length - offset).ConfigureAwait(false);
                // if (sent == 0)
                //     throw new Exception("Connection lost");
                // offset += sent;
            // }
        }

        public async Task<byte[]> Receive(int length)
        {
            int offset = 0;
            var data = new byte[length];
            while (offset < length)
            {
                var received = await _networkStream.ReadAsync(
                    data,
                    offset,
                    length - offset).ConfigureAwait(false);
                if (received == 0)
                    throw new Exception("Connection lost");
                offset += received;
            }

            return data;
        }
    }
}