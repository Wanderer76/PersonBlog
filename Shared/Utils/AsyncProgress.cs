using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Utils
{
    public class AsyncProgress<T>
    {
        private readonly Func<T, Task> _callback;
        public AsyncProgress(Func<T, Task> progressCallback)
        {
            _callback = progressCallback;
        }

        public Task ReportAsync(T value)
        {
            return _callback(value);
        }

        public static implicit operator Func<T, Task>(AsyncProgress<T> progress) => progress._callback;

        public static implicit operator AsyncProgress<T>(Func<T, Task> func) => new(func);
    }
}
