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

        public void Send(byte[] data)
        {
            int offset = 0;
            while (offset < data.Length)
            {
                var sent = _networkStream.WriteAsync(
                    data,
                    offset,
                    data.Length - offset);
                    // si se caga el evio es ACA
                // if (sent == 0)
                //     throw new Exception("Connection lost");
                // offset += sent;
            }
        }

        public byte[] Receive(int length)
        {
            int offset = 0;
            var data = new byte[length];
            while (offset < length)
            {
                var received = _networkStream.ReadAsync(
                    data,
                    offset,
                    length - offset).ConfigureAwait(false).GetAwaiter().GetResult();
                if (received == 0)
                    throw new Exception("Connection lost");
                offset += received;
            }

            return data;
        }
    }
}