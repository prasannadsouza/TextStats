using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextStats.ParseLogic.Model
{
    public class TextFile
    {

        public string? CheckSum { get; set; }
        public Guid? Guid { get; set; }
        public string? FileName { get; set; }
        public long? NumberOfLines { get; set; }
        public long? NumberOfWords { get; set; }
        public List<WordFrequency>? WordFrequencies { get; set; }
    }
}
