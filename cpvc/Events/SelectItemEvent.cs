using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class SelectItemEventArgs : EventArgs
    {
        public SelectItemEventArgs()
        {
        }

        public List<string> Items { get; set; }

        public string SelectedItem { get; set; }
    }

    public delegate void SelectItemEventHandler(object sender, SelectItemEventArgs e);
}
