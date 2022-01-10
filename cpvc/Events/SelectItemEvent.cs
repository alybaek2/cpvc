using System.Collections.Generic;
using System.ComponentModel;

namespace CPvC
{
    public class SelectItemEventArgs : HandledEventArgs
    {
        public SelectItemEventArgs(List<string> items)
        {
            Items = items;
        }

        public List<string> Items { get; private set; }

        public string SelectedItem { get; set; }
    }

    public delegate void SelectItemEventHandler(object sender, SelectItemEventArgs e);
}
