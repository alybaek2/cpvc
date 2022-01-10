using System.ComponentModel;

namespace CPvC
{
    public class PromptForNameEventArgs : HandledEventArgs
    {
        public PromptForNameEventArgs(string existingName)
        {
            ExistingName = existingName;
        }

        public string ExistingName { get; private set; }

        public string SelectedName { get; set; }
    }

    public delegate void PromptForNameEventHandler(object sender, PromptForNameEventArgs e);

}
