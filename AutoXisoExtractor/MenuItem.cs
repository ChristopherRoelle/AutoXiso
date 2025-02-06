using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoXisoExtractor
{
    internal class MenuItem
    {
        public string Name { get; set; }
        public string Descriptor { get; set; }
        public bool RequiresROMs { get; set; }
        private Action? Action { get; set; }

        public MenuItem(string name, string descriptor, bool requiresROMs, Action action)
        {
            Name = name;
            Descriptor = descriptor;
            RequiresROMs = requiresROMs;
            Action = action;
        }

        public void PerformAction()
        {
            Action?.Invoke();
        }
        

    }
}
