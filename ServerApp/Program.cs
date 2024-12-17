using ServerApp.FtpItems;
using ServerApp.HttpItems;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ServerApp
{
    class Server
    {
        static async Task Main()
        {
            Console.WriteLine("FTP/HTTP Server >");

            string bufferPath = "E:\\LabRabs\\MultyThreading\\course_9\\WebLettersCounter\\ServerApp\\TextData\\textBuffer.txt";

            FtpServer ftpServer = new FtpServer("127.0.0.1", 20, 21, bufferPath);
            HttpServer httpServer = new HttpServer("http://localhost:8081/", bufferPath);

            ftpServer.taskReceived += httpServer.FtpTaskReceived;
            httpServer.taskSolved += ftpServer.SendTaskResult;

            ftpServer.StartServer();
            httpServer.StartServer();

            await LoopServer(ftpServer, httpServer);
        }

        private static async Task LoopServer(FtpServer ftpServer, HttpServer httpServer)
        {
            Stopwatch sw = Stopwatch.StartNew();

            while (true)
            {
                await httpServer.HandleRequest();

                if (sw.ElapsedMilliseconds < 5000) continue;
                else sw.Stop();

                if (!ftpServer.connected)
                    ftpServer.TryConnect();
                else
                {
                    if (!ftpServer.solving)
                        await ftpServer.HandleClientAsync();
                }
            }
        }
    }
}
