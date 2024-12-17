using ClientApp.FtpItems;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using ClientApp.Models;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using Microsoft.Win32;

namespace ClientApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int DISPLAY_CHARACTERS_COUNT = 100_000;

        private string _textDataPath = "E:\\LabRabs\\MultyThreading\\course_9\\WebLettersCounter\\ClientApp\\TextData\\ftpText.txt";
        private long _linearTime;
        private long _distributedTime;

        public long LinearTime
        {
            get
            {
                return _linearTime;
            }
            set
            {
                _linearTime = value;
                OnPropertyChanged(nameof(LinearTime));
            }
        }

        public long DistributedTime
        {
            get
            {
                return _distributedTime;
            }
            set
            {
                _distributedTime = value;
                OnPropertyChanged(nameof(DistributedTime));
            }
        }

        private ObservableCollection<LetterStatItem> _lettersStats = new ObservableCollection<LetterStatItem>();

        public ObservableCollection<LetterStatItem> LetterStats
        {
            get
            {
                return _lettersStats;
            }
            set
            {
                _lettersStats = value;
                OnPropertyChanged(nameof(LetterStats));
            }
        }

        private double[] _linearTimes;
        private double[] _distributedTimes;
        private ChartValues<ObservablePoint> _linearData = new ChartValues<ObservablePoint>();
        private ChartValues<ObservablePoint> _distributedData = new ChartValues<ObservablePoint>();

        public ChartValues<ObservablePoint> LinearData
        {
            get => _linearData;
            set
            {
                _linearData = value;
                OnPropertyChanged(nameof(LinearData));
            }
        }

        public ChartValues<ObservablePoint> DistributedData
        {
            get => _distributedData;
            set
            {
                _distributedData = value;
                OnPropertyChanged(nameof(DistributedData));
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;


            chart.AxisY[0].MinValue = double.NaN;
            chart.AxisY[0].MaxValue = double.NaN;
            chart.AxisY[0].LabelFormatter = value => value.ToString("N0") + " мс";
        }

        public void GenerateRandomText(string filePath, int letterCount)
        {
            char[] russianLetters = new char[] { 'а', 'б', 'в', 'г', 'д', 'е', 'ё', 'ж', 'з', 'и', 'й', 'к', 'л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ъ', 'ы', 'ь', 'э', 'ю', 'я', ' ' };
            Random rnd = new Random();
            StringBuilder sb = new StringBuilder();

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                for (int i = 0; i < letterCount; i++)
                {
                    int randomIndex = rnd.Next(russianLetters.Length);
                    char randomChar = russianLetters[randomIndex];

                    writer.Write(randomChar);

                    if (i < DISPLAY_CHARACTERS_COUNT)
                        sb.Append(randomChar);
                }
            }

            MainTextBox.Text = sb.ToString();
            if (letterCount > DISPLAY_CHARACTERS_COUNT)
                MainTextBox.Text += "\n...";
        }


        private void OnSendButton_Click(object sender, RoutedEventArgs e)
        {
            SendTextFile();
        }

        private async void SendTextFile()
        {
            string fileName = "FtpText.txt";

            var client = new FtpClient("127.0.0.1", 20, 21);

            bool connected = false;
            while (!connected)
            {
                connected = await client.TryConnect();
                await Task.Delay(1000);
            }

            (string letters, string times) = await client.UploadFile(_textDataPath, fileName);
            UpdateLetterStats(letters);
            _distributedTimes = Array.ConvertAll(times.Split(' '), double.Parse);
            DistributedTime = (long)_distributedTimes.Last();
            UpdateChartData(_linearTimes, _distributedTimes);
        }

        private void UpdateLetterStats(string statistics)
        {
            LetterStats.Clear();

            var pairs = statistics.Split(',');

            foreach (var pair in pairs)
            {
                var parts = pair.Trim().Split(':');
                if (parts.Length == 2 && char.TryParse(parts[0].Trim(), out char letter)
                    && int.TryParse(parts[1].Trim(), out int count))
                {
                    LetterStats.Add(new LetterStatItem
                    {
                        Letter = letter,
                        Count = count
                    });
                }
            }

            var sorted = LetterStats.OrderByDescending(x => x.Count).ToList();
            LetterStats.Clear();
            foreach (var item in sorted)
            {
                LetterStats.Add(item);
            }
        }

        private void UpdateLetterStats(Dictionary<char, int> letterStatistics)
        {
            LetterStats.Clear();

            foreach (var entry in letterStatistics)
            {
                LetterStats.Add(new LetterStatItem
                {
                    Letter = entry.Key,
                    Count = entry.Value
                });
            }

            var sorted = LetterStats.OrderByDescending(x => x.Count).ToList();
            LetterStats.Clear();
            foreach (var item in sorted)
            {
                LetterStats.Add(item);
            }
        }

        public void UpdateChartData(double[] linearArray, double[] distributedArray)
        {
            if (linearArray == null) linearArray = new double[1] { 1 };
            if (distributedArray == null) distributedArray = new double[1] { 1 };

            LinearData.Clear();
            DistributedData.Clear();

            chart.AxisY[0].MinValue = 0;
            chart.AxisY[0].MaxValue = Math.Max(linearArray.Max(), distributedArray.Max());

            double stepSize = 100.0 / (linearArray.Length - 1);

            for (int i = 0; i < linearArray.Length; i++)
            {
                double percentage = i * stepSize;
                LinearData.Add(new ObservablePoint(percentage, Convert.ToDouble(linearArray[i])));
            }

            stepSize = 100.0 / (distributedArray.Length - 1);

            for (int i = 0; i < distributedArray.Length; i++)
            {
                double percentage = i * stepSize;
                DistributedData.Add(new ObservablePoint(percentage, Convert.ToDouble(distributedArray[i])));
            }
        }

        private void LoadFromFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Выберите текстовый файл"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string sourceFilePath = openFileDialog.FileName;

                try
                {
                    File.Copy(sourceFilePath, _textDataPath, true);

                    using (StreamReader reader = new StreamReader(sourceFilePath))
                    {
                        string line;
                        int lineCount = 0;
                        StringBuilder sb = new StringBuilder();

                        while ((line = reader.ReadLine()) != null && lineCount < DISPLAY_CHARACTERS_COUNT)
                        {
                            sb.AppendLine(line);
                        }

                        MainTextBox.Text = sb.ToString();
                        if (sb.Length > DISPLAY_CHARACTERS_COUNT)
                            MainTextBox.Text += "\n...";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при копировании файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnSolveLocalButton_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<char, int> counts = new Dictionary<char, int>();
            for (char c = 'а'; c <= 'я'; c++) counts[c] = 0;
            counts['ё'] = 0;
            List<double> times = new List<double>();
            Stopwatch sw = Stopwatch.StartNew();

            using (var reader = new StreamReader(_textDataPath, Encoding.UTF8))
            {
                int bufferSize = 8192;
                char[] buffer = new char[bufferSize];
                int charsRead;
                int timesStep = (int)(reader.BaseStream.Length / 10);
                int totalReaded = 0;
                times.Add(0);
                
                while ((charsRead = reader.Read(buffer, 0, bufferSize)) > 0)
                {
                    for (int i = 0; i < charsRead; i++)
                    {
                        char c = char.ToLower(buffer[i]);
                        if (char.IsLetter(c) && counts.ContainsKey(c))
                        {
                            counts[c]++;
                        }
                    }

                    totalReaded += bufferSize;
                    if (totalReaded >= timesStep)
                    {
                        timesStep += totalReaded;
                        times.Add(sw.ElapsedMilliseconds);
                    }
                }
            }
            sw.Stop();

            UpdateLetterStats(counts);
            LinearTime = sw.ElapsedMilliseconds;
            _linearTimes = times.ToArray();
            UpdateChartData(_linearTimes, _distributedTimes);
        }

        private void GraphTabItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UpdateChartData(_linearTimes, _distributedTimes);
        }

        private void OnGenerateTextButton_Click(object sender, RoutedEventArgs e)
        {
            int count = int.Parse(lettersCountBox.Text.Replace("_", ""));
            GenerateRandomText(_textDataPath, count);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
