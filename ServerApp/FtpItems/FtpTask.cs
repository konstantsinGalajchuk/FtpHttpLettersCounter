using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ServerApp.WebItems
{
    public class FtpTask
    {
        private const int MAX_BUFFER_SIZE = 8_290_304;

        private string _textBufferPath;
        private Dictionary<char, int> _result = new Dictionary<char, int>();
        private int _textSize;

        private int _solutionsCount;
        private int _tasksCount;
        private int _currentTaskIndex;
        private int[] _tasksSizes;
        
        private bool _untached = true;
        private bool _solved = false;

        public bool Untached
        {
            get => _untached;
        }

        public bool Solved
        {
            get => _solved;
        }


        public FtpTask(string textBufferPath)
        {
            _textBufferPath = textBufferPath;
            _textSize = GetTextSize();
        }

        public string GetText(int from, int length)
        {
            StringBuilder extractedString = new StringBuilder();
            int totalCharactersRead = 0;

            using (var reader = new StreamReader(_textBufferPath, Encoding.UTF8))
            {
                reader.BaseStream.Seek(from, SeekOrigin.Begin);
                reader.DiscardBufferedData();

                char[] buffer = new char[length];
                int charsRead;

                while ((charsRead = reader.Read(buffer, 0, length)) > 0)
                {
                    totalCharactersRead += charsRead;
                    extractedString.Append(buffer, 0, charsRead);

                    if (totalCharactersRead >= length)
                        break;
                }
            }

            return extractedString.ToString();
        }


        public int GetTextSize()
        {
            int characterCount = 0;

            using (FileStream fileStream = new FileStream(_textBufferPath, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8, true, 8096))
            {
                char[] buffer = new char[8096];
                int bytesRead;

                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    characterCount += bytesRead;
                }
            }

            return characterCount;
        }

        public Dictionary<char, int> GetResult() => _result;

        public void SetTasksQueue(int tasksCount)
        {
            _untached = false;
            _solved = false;
            _currentTaskIndex = 0;
            _solutionsCount = 0;

            if (_textSize / tasksCount > MAX_BUFFER_SIZE)
            {
                _tasksCount = _textSize / MAX_BUFFER_SIZE + 1;
                _tasksSizes = new int[_tasksCount];

                for (int i = 0; i < _tasksCount - 1; i++)
                    _tasksSizes[i] = MAX_BUFFER_SIZE;
                _tasksSizes[_tasksCount - 1] = _textSize - ((_tasksCount - 1) * MAX_BUFFER_SIZE);
            }
            else
            {
                int over = 0;
                int size = Math.DivRem(_textSize, tasksCount, out over);
                _tasksCount = _textSize / size;

                _tasksSizes = new int[_tasksCount];
                for (int i = 0; i < _tasksCount; i++)
                {
                    if (over > 0)
                    {
                        _tasksSizes[i] = size + 1;
                        over--;
                    }
                    else
                        _tasksSizes[i] = size;
                }
            }

            for (char c = 'а'; c <= 'я'; c++) _result[c] = 0;
            _result['ё'] = 0;
        }

        public string NextTask()
        {
            if (_currentTaskIndex == _tasksCount)
            {
                return string.Empty;
            }
            else
            {
                int from = GetFromIndex(_tasksSizes, _currentTaskIndex);
                int len = _tasksSizes[_currentTaskIndex];

                string task = GetText(from, len).ToLower();
                _currentTaskIndex++;

                return task;
            }
        }

        private int GetFromIndex(int[] sizes, int index)
        {
            int sum = 0;

            if (index > 0)
            {
                for (int i = 0; i < index; i++)
                    sum += sizes[i];
            }
            
            return sum;
        }

        public void ApplyResult(Dictionary<char, int> result)
        {
            foreach (char c in result.Keys)
            {
                if (_result.ContainsKey(c))
                    _result[c] += result[c];
            }

            _solutionsCount++;
            if (_solutionsCount == _tasksCount) _solved = true;
        }
    }
}
