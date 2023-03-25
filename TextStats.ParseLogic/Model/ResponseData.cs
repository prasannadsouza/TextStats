using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextStats.ParseLogic.Model
{
    public class ResponseData<T>
    {
        public T? Data { get; set; }
        public string? Error { get; set; }
    }
}
