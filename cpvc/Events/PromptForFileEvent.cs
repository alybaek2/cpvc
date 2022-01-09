using System;

namespace CPvC
{
    public class PromptForFileEventArgs : EventArgs
    {
        public PromptForFileEventArgs(FileTypes fileType, bool existing)
        {
            FileType = fileType;
            Existing = existing;
        }

        public FileTypes FileType { get; private set; }
        public bool Existing { get; private set; }

        public string Filepath { get; set; }
    }

    public delegate void PromptForFileEventHandler(object sender, PromptForFileEventArgs e);
}
