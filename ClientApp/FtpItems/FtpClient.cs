using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp.FtpItems
{
    public class FtpClient
    {
        private const int MIN_BUFFER_SIZE = 1024;
        private const int MAX_BUFFER_SIZE = 8_290_304;

        private string _ip;
        private int _comPort;
        private int _filePort;

        private TcpClient _comClient;
        private TcpClient _fileClient;

        private StreamReader _comReader;
        private StreamWriter _comWriter;


        public FtpClient(string ip, int comPort, int filePort)
        {
            _ip = ip;
            _comPort = comPort;
            _filePort = filePort;
        }

        public async Task<bool> TryConnect()
        {
            try
            {
                _comClient = new TcpClient();
                await _comClient.ConnectAsync(_ip, _comPort);
                _fileClient = new TcpClient();
                await _fileClient.ConnectAsync(_ip, _filePort);

                Debug.WriteLine("Клиент успешно подключен к серверу.");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(string, string)> UploadFile(string localPath, string remoteFilename)
        {
            _comWriter = new StreamWriter(_comClient.GetStream(), Encoding.UTF8) { AutoFlush = true };
            _comReader = new StreamReader(_comClient.GetStream(), Encoding.UTF8);
            string response;

            using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read))
            {
                var bufferSize = 8_290_304;
                _fileClient.SendBufferSize = bufferSize;

                await _comWriter.WriteLineAsync($"STOR {remoteFilename} {bufferSize}");
                response = await _comReader.ReadLineAsync();
                Debug.WriteLine($"Server state: {response}");

                int totalRead = 0;

                var buffer = new byte[bufferSize];
                int bytesRead;

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    totalRead += bytesRead;
                    await _fileClient.GetStream().WriteAsync(buffer, 0, bytesRead);

                    response = await _comReader.ReadLineAsync();
                    await fileStream.FlushAsync();
                    await _fileClient.GetStream().FlushAsync();

                    Debug.WriteLine($"Sended: {totalRead}/{fileStream.Length}");
                }

                Debug.WriteLine("All chunks sended successfully.");
            }

            response = await _comReader.ReadLineAsync();
            Debug.WriteLine($"Server state: {response}");

            string letters = await _comReader.ReadLineAsync();
            Debug.WriteLine($"Ответ сервера (подсчитанные буквы): {letters}");

            string times = await _comReader.ReadLineAsync();

            _comReader.Close();
            _comWriter.Close();
            Close();

            return (letters, times);
        }

        public void Close()
        {
            _comClient.Close();
            _fileClient.Close();
        }
    }
}
