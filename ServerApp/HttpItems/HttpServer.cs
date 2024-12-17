using ServerApp.WebItems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp.HttpItems
{
    public class HttpServer
    {
        private string _url;
        private string _textBufferPath;

        public event Action<Dictionary<char, int>, List<double>> taskSolved = delegate { };
        public Stopwatch workTimer = new Stopwatch();

        private List<LCCNode> _lccNodes = new List<LCCNode>();
        private HttpListener _listener = null;

        private FtpTask _ftpTask = null;
        private List<double> _workTimes;

        public HttpServer(string url, string textBufferPath)
        {
            _url = url;
            _textBufferPath = textBufferPath;
        }


        public void StartServer()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();

            Console.WriteLine("Http Сервер запущен и ожидает подключений...");
        }

        public void FtpTaskReceived()
        {
            _ftpTask = new FtpTask(_textBufferPath);
        }

        public async Task HandleRequest()
        {
            var context = await _listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST" && request.Url.AbsolutePath.EndsWith("/register"))
            {
                await RegisterLCCNode(request, response);
            }
            else if (request.HttpMethod == "POST" && request.Url.AbsolutePath.EndsWith("/task"))
            {
                if (_ftpTask != null)
                {
                    if (_ftpTask.Untached)
                    {
                        workTimer.Reset();
                        workTimer.Start();
                        _workTimes = new List<double>() { 0 };
                        _ftpTask.SetTasksQueue(_lccNodes.Count);
                    }

                    await SendTask(response);
                }
                else
                    response.StatusCode = 523;
            }
            else if (request.HttpMethod == "POST" && request.Url.AbsolutePath.EndsWith("/taskresult"))
            {
                if (_ftpTask != null)
                {
                    await GetTaskResult(request, response);

                    if (_ftpTask.Solved)
                        SendResult();
                }
            }

            response.Close();
        }

        private async Task RegisterLCCNode(HttpListenerRequest request, HttpListenerResponse response)
        {
            _lccNodes.Add(new LCCNode());
            Console.WriteLine($"HTTP-Клиент {request.RemoteEndPoint.Address} зарегистрирован.");
            byte[] buffer = Encoding.UTF8.GetBytes("Registered successfully");
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        private async Task SendTask(HttpListenerResponse response)
        {
            try
            {
                var task = _ftpTask.NextTask();
                if (task == string.Empty)
                {
                    response.StatusCode = 523;
                    response.Close();
                }
                else
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(task);
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

                    Console.WriteLine($"Задача отправлена http-клиенту.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке задачи http-клиенту: {ex.Message}");
            }
        }

        private async Task GetTaskResult(HttpListenerRequest request, HttpListenerResponse response)
        {
            string requestBody = new System.IO.StreamReader(request.InputStream).ReadToEnd();
            var clientResult = requestBody.Split(',').Select(pair => pair.Split(':')).ToDictionary(pair => pair[0][0], pair => int.Parse(pair[1]));
            _ftpTask.ApplyResult(clientResult);
            _workTimes.Add(workTimer.ElapsedMilliseconds);

            Console.WriteLine($"Http-Клиент {request.RemoteEndPoint.Address} отправил результаты:");
            foreach (var kv in clientResult)
                Console.WriteLine($"{kv.Key}: {kv.Value}");

            response.StatusCode = 200;
            byte[] buffer = Encoding.UTF8.GetBytes("Task result received successfully");
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        private void SendResult()
        {
            workTimer.Stop();
            var fullResult = _ftpTask.GetResult();
            _ftpTask = null;

            Console.WriteLine("Задача была успешно решена за " + workTimer.ElapsedMilliseconds.ToString() + " мсек.");
            Console.WriteLine("Полный результат:");
            foreach (var kv in fullResult)
                Console.WriteLine($"{kv.Key}: {kv.Value}");

            taskSolved(fullResult, _workTimes);
        }
    }
}
