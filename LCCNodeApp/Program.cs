using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LCCNodeApp
{
    class LCCNode
    {
        const string SERVER_URL = "http://localhost:8081/";

        private static readonly HttpClient client = new HttpClient();
        private static bool registered = false;

        static async Task Main()
        {
            while (!registered)
            {
                await Register();
                //await Task.Delay(1000);
            }

            while (true)
            {
                await CheckForTask();
            }
        }

        static async Task Register()
        {
            try
            {
                var response = await client.PostAsync(SERVER_URL + "register", null);
                if (response.IsSuccessStatusCode)
                {
                    registered = true;
                    Console.WriteLine($"Клиент успешно зарегистрирован.");
                }
                else
                {
                    registered = false;
                    Console.WriteLine("Ошибка регистрации клиента.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось найти сервер: {ex.Message}");
            }
        }

        static async Task CheckForTask()
        {
            try
            {
                var response = await client.PostAsync(SERVER_URL + "task", null);

                if (response.IsSuccessStatusCode)
                {
                    var task = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Получена задача.");
                    await SolveTask(task);
                }
                else
                {
                    //Console.WriteLine("Не удалось получить задачу.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запросе задачи: {ex.Message}");
            }
        }

        private static async Task SolveTask(string task)
        {
            var charCount = task.GroupBy(c => c).Where(g => char.IsLetter(g.Key)).ToDictionary(g => g.Key, g => g.Count());
            var charCountString = string.Join(",", charCount.Select(kv => $"{kv.Key}:{kv.Value}"));

            var content = new StringContent(charCountString, Encoding.UTF8, "application/json");
            var confirmationResponse = await client.PostAsync(SERVER_URL + "taskresult", content);

            if (confirmationResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Результат решения задачи отправлен успешно.");
            }
            else
            {
                Console.WriteLine("Не удалось отправить результат решения задачи.");
            }
        }
    }
}
