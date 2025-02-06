using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoXisoExtractor
{
    internal class ROM
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public ROM(string name, string path) { 
            Name = name;
            Path = path;
        }


    }
}
