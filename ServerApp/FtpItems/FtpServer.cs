using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp.FtpItems
{
    public class FtpServer
    {
        private string _fileBufferPath;

        public event Action taskReceived = delegate { };
        public bool connected = false;
        public bool solving = false;

        private TcpListener _comListener = null;
        private TcpListener _fileListener = null;

        private TcpClient _comClient = null;
        private TcpClient _fileClient = null;

        private StreamWriter _comWriter = null;

        public FtpServer(string ipAddress, int comPort, int filePort, string fileBufferPath)
        {
            _comListener = new TcpListener(IPAddress.Parse(ipAddress), comPort);
            _fileListener = new TcpListener(IPAddress.Parse(ipAddress), filePort);
            _fileBufferPath = fileBufferPath;
        }

        public void StartServer()
        {
            _comListener.Start();
            _fileListener.Start();
            Console.WriteLine("FTP сервер запущен. Ожидание подключений...");
        }

        public bool TryConnect()
        {
            _comClient = _comListener.AcceptTcpClient();
            _fileClient = _fileListener.AcceptTcpClient();

            if (_comClient == null || _fileClient == null)
            {
                connected = false;
                return false;
            }
            else
            {
                Console.WriteLine("FTP-Клиент подключен.");
                connected = true;
                return true;
            }
        }

        public async Task HandleClientAsync()
        {
            var comStream = _comClient.GetStream();
            var fileStream = _fileClient.GetStream();

            var comReader = new StreamReader(comStream, Encoding.UTF8);
            var fileReader = new StreamReader(fileStream, Encoding.UTF8);
            _comWriter = new StreamWriter(comStream, Encoding.UTF8) { AutoFlush = true };

            var command = await comReader.ReadLineAsync();
            Console.WriteLine($"Получена ftp-команда: {command}");
            if (command == null) return;

            await ProcessCommand(command, fileReader);
        }

        private async Task ProcessCommand(string command, StreamReader fileReader)
        {
            var commandParts = command.Split(' ');

            switch (commandParts[0].ToUpper())
            {
                case "STOR":
                    await _comWriter.WriteLineAsync("220 Service Ready");
                    await StoreFileAsync(commandParts[1], int.Parse(commandParts[2]), fileReader);
                    break;
                default:
                    await _comWriter.WriteLineAsync("500 Syntax error, command unrecognized.");
                    break;
            }
        }

        private async Task StoreFileAsync(string filename, int bufferSize, StreamReader fileReader)
        {
            _fileClient.ReceiveBufferSize = bufferSize;

            using (var fileStream = new FileStream(_fileBufferPath, FileMode.Truncate, FileAccess.Write))
            {
                var buffer = new byte[bufferSize];
                int bytesRead;

                while ((bytesRead = await fileReader.BaseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    await _comWriter.WriteLineAsync("BLOCK_READED_SUCCESSFULLY");
                    await fileStream.FlushAsync();
                    await fileReader.BaseStream.FlushAsync();
                    if (bytesRead < buffer.Length) break;
                }
            }

            await _comWriter.WriteLineAsync("FILE_READED_STATUS: SUCCESS");
            Console.WriteLine("All text loaded from FTP-Client successuflly");

            solving = true;
            taskReceived();
        }

        public async void SendTaskResult(Dictionary<char, int> result, List<double> times)
        {
            var responseString = string.Join(", ", result.Select(kv => $"{kv.Key}: {kv.Value}"));
            var timesString = string.Join(", ", times);

            await _comWriter.WriteLineAsync(responseString);
            await _comWriter.WriteLineAsync(timesString);

            Console.WriteLine("Файл успешно загружен и обработан.");
            Close();
            await Task.Delay(5000);
            connected = false;
            solving = false;
        }

        private void Close()
        {
            _comWriter.Close();

            _comClient.Close();
            _fileClient.Close();

            //_comListener.Stop();
            //_fileListener.Stop();
        }
    }
}
