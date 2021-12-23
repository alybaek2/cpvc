using System;

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
