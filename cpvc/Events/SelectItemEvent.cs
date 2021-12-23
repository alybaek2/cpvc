using System;
using System.Collections.Generic;

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
