using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Communication
{
    public class FileCommsHandler
    {
        private readonly ConversionHandler _conversionHandler;
        public readonly FileHandler _fileHandler;
        private readonly FileStreamHandler _fileStreamHandler;
        private readonly SocketHelper _socketHelper;

        public FileCommsHandler(NetworkStream networkStream)
        {
            _conversionHandler = new ConversionHandler();
            _fileHandler = new FileHandler();
            _fileStreamHandler = new FileStreamHandler();
            _socketHelper = new SocketHelper(networkStream);
        }

        public async Task SendFile(string path)
        {
            if (_fileHandler.FileExists(path))
            {
                var fileName = _fileHandler.GetFileName(path);
                // ---> Enviar el largo del nombre del archivo
                await _socketHelper.Send(_conversionHandler.ConvertIntToBytes(fileName.Length));
                // ---> Enviar el nombre del archivo
                await _socketHelper.Send(_conversionHandler.ConvertStringToBytes(fileName));

                // ---> Obtener el tamaño del archivo
                long fileSize = _fileHandler.GetFileSize(path);
                // ---> Enviar el tamaño del archivo
                var convertedFileSize = _conversionHandler.ConvertLongToBytes(fileSize);
                await _socketHelper.Send(convertedFileSize);
                // ---> Enviar el archivo (pero con file stream)
                await SendFileWithStream(fileSize, path);
            }
            else
            {
                throw new Exception("File does not exist");
            }
        }

        public async Task<string> ReceiveFile(string userName)
        {
            // ---> Recibir el largo del nombre del archivo
            int fileNameSize = _conversionHandler.ConvertBytesToInt(
                await _socketHelper.Receive(Protocol.FixedDataSize));
            // ---> Recibir el nombre del archivo
            string fileName = _conversionHandler.ConvertBytesToString(await _socketHelper.Receive(fileNameSize));
            string extension = fileName.Split('.').Last();
            userName = userName + "." + extension;
            string fileName2 = Path.Combine("Fotos", userName);
            // if the file exists then delete it
            if (_fileHandler.FileExists(fileName2))
                _fileHandler.DeleteFile(fileName2);
            // ---> Recibir el largo del archivo
            long fileSize = _conversionHandler.ConvertBytesToLong(
                await _socketHelper.Receive(Protocol.FixedFileSize));
            // ---> Recibir el archivo
            await ReceiveFileWithStreams(fileSize, userName);
            return extension;
        }

        private async Task SendFileWithStream(long fileSize, string path)
        {
            long fileParts = Protocol.CalculateFileParts(fileSize);
            long offset = 0;
            long currentPart = 1;

            while (fileSize > offset)
            {
                byte[] data;
                if (currentPart == fileParts)
                {
                    var lastPartSize = (int)(fileSize - offset);
                    data = _fileStreamHandler.Read(path, offset, lastPartSize);
                    offset += lastPartSize;
                }
                else
                {
                    data = _fileStreamHandler.Read(path, offset, Protocol.MaxPacketSize);
                    offset += Protocol.MaxPacketSize;
                }

                await _socketHelper.Send(data);
                currentPart++;
            }
        }

        private async Task ReceiveFileWithStreams(long fileSize, string fileName)
        {
            long fileParts = Protocol.CalculateFileParts(fileSize);
            long offset = 0;
            long currentPart = 1;

            while (fileSize > offset)
            {
                byte[] data;
                if (currentPart == fileParts)
                {
                    var lastPartSize = (int)(fileSize - offset);
                    data = await _socketHelper.Receive(lastPartSize);
                    offset += lastPartSize;
                }
                else
                {
                    data = await _socketHelper.Receive(Protocol.MaxPacketSize);
                    offset += Protocol.MaxPacketSize;
                }
                _fileStreamHandler.Write(fileName, data);
                currentPart++;
            }
        }
    }
}

