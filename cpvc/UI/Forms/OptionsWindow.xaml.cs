using System;
using System.Windows;
using System.Windows.Controls;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        public OptionsWindow(Window owner)
        {
            InitializeComponent();

            Owner = owner;
        }

        private void BrowseForFolder(TextBox textBox)
        {
            using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string folder = folderDialog.SelectedPath;
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        textBox.Text = folder;
                    }
                }
            }
        }

        private void SetTextBox(TextBox textBox, string value)
        {
            if (Settings.MachinesFolder != null)
            {
                textBox.Text = value;
            }
        }

        private void GetTextBox(TextBox textBox, Action<string> propSet)
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                propSet(textBox.Text);
            }
        }

        // Don't use binding for these three properties, as we only want to set them when "OK" is clicked.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetTextBox(_machinesFolderTextBox, Settings.MachinesFolder);
            SetTextBox(_discsFolderTextBox, Settings.DiscsFolder);
            SetTextBox(_tapesFolderTextBox, Settings.TapesFolder);
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            GetTextBox(_machinesFolderTextBox, x => Settings.MachinesFolder = x);
            GetTextBox(_discsFolderTextBox, x => Settings.DiscsFolder = x);
            GetTextBox(_tapesFolderTextBox, x => Settings.TapesFolder = x);

            Close();
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void _machinesFolderBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFolder(_machinesFolderTextBox);
        }

        private void _discsFolderBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFolder(_discsFolderTextBox);
        }

        private void _tapesFolderBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFolder(_tapesFolderTextBox);
        }
    }
}
