using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class PromptForFileEventArgs : EventArgs
    {
        public PromptForFileEventArgs()
        {
        }

        public FileTypes FileType { get; set; }
        public bool Existing { get; set; }

        public string Filepath { get; set; }
    }

    public delegate void PromptForFileEventHandler(object sender, PromptForFileEventArgs e);
}
