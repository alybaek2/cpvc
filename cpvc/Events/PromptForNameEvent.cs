using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class PromptForNameEventArgs : EventArgs
    {
        public PromptForNameEventArgs()
        {
        }

        public string ExistingName { get; set; }

        public string SelectedName { get; set; }
    }

    public delegate void PromptForNameEventHandler(object sender, PromptForNameEventArgs e);

}
