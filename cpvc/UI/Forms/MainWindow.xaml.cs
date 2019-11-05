using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IUserInterface, IDisposable
    {
        private readonly FileSystem _fileSystem;
        private readonly MainWindowLogic _logic;
        private Audio _audio;
        private KeyboardMapping _keyMap;

        public MainWindow()
        {
            InitializeComponent();

            _fileSystem = new FileSystem();
            _logic = new MainWindowLogic(this, _fileSystem);
            _audio = new Audio(_logic.ReadAudio);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_audio != null)
            {
                _audio.Stop();
                _audio.Dispose();
                _audio = null;
            }
        }

        private void StartAudio()
        {
            _audio.Start();
        }

        private void StopAudio()
        {
            _audio.Stop();
            _audio = null;
        }

        public void InitKeyboardMap()
        {
            _keyMap = new KeyboardMapping();

            _keyMap.Map(Key.A, Keys.A);
            _keyMap.Map(Key.B, Keys.B);
            _keyMap.Map(Key.C, Keys.C);
            _keyMap.Map(Key.D, Keys.D);
            _keyMap.Map(Key.E, Keys.E);
            _keyMap.Map(Key.F, Keys.F);
            _keyMap.Map(Key.G, Keys.G);
            _keyMap.Map(Key.H, Keys.H);
            _keyMap.Map(Key.I, Keys.I);
            _keyMap.Map(Key.J, Keys.J);
            _keyMap.Map(Key.K, Keys.K);
            _keyMap.Map(Key.L, Keys.L);
            _keyMap.Map(Key.M, Keys.M);
            _keyMap.Map(Key.N, Keys.N);
            _keyMap.Map(Key.O, Keys.O);
            _keyMap.Map(Key.P, Keys.P);
            _keyMap.Map(Key.Q, Keys.Q);
            _keyMap.Map(Key.R, Keys.R);
            _keyMap.Map(Key.S, Keys.S);
            _keyMap.Map(Key.T, Keys.T);
            _keyMap.Map(Key.U, Keys.U);
            _keyMap.Map(Key.V, Keys.V);
            _keyMap.Map(Key.W, Keys.W);
            _keyMap.Map(Key.X, Keys.X);
            _keyMap.Map(Key.Y, Keys.Y);
            _keyMap.Map(Key.Z, Keys.Z);
            _keyMap.Map(Key.D0, Keys.Num0);
            _keyMap.Map(Key.D1, Keys.Num1);
            _keyMap.Map(Key.D2, Keys.Num2);
            _keyMap.Map(Key.D3, Keys.Num3);
            _keyMap.Map(Key.D4, Keys.Num4);
            _keyMap.Map(Key.D5, Keys.Num5);
            _keyMap.Map(Key.D6, Keys.Num6);
            _keyMap.Map(Key.D7, Keys.Num7);
            _keyMap.Map(Key.D8, Keys.Num8);
            _keyMap.Map(Key.D9, Keys.Num9);
            _keyMap.Map(Key.Escape, Keys.Escape);
            _keyMap.Map(Key.Tab, Keys.Tab);
            _keyMap.Map(Key.CapsLock, Keys.CapsLock);
            _keyMap.Map(Key.LeftShift, Keys.Shift);
            _keyMap.Map(Key.RightShift, Keys.Shift);
            _keyMap.Map(Key.LeftCtrl, Keys.Control);
            _keyMap.Map(Key.LeftAlt, Keys.Copy);
            _keyMap.Map(Key.Up, Keys.CursorUp);
            _keyMap.Map(Key.Down, Keys.CursorDown);
            _keyMap.Map(Key.Left, Keys.CursorLeft);
            _keyMap.Map(Key.Right, Keys.CursorRight);
            _keyMap.Map(Key.OemOpenBrackets, Keys.LeftBrace);
            _keyMap.Map(Key.OemCloseBrackets, Keys.RightBrace);
            _keyMap.Map(Key.OemSemicolon, Keys.Asterix);
            _keyMap.Map(Key.OemQuotes, Keys.Plus);
            _keyMap.Map(Key.OemMinus, Keys.EqualsSign);
            _keyMap.Map(Key.OemPipe, Keys.At);
            _keyMap.Map(Key.OemPlus, Keys.Caret);
            _keyMap.Map(Key.Back, Keys.Delete);
            _keyMap.Map(Key.F10, Keys.Function0);
            _keyMap.Map(Key.F1, Keys.Function1);
            _keyMap.Map(Key.F2, Keys.Function2);
            _keyMap.Map(Key.F3, Keys.Function3);
            _keyMap.Map(Key.F4, Keys.Function4);
            _keyMap.Map(Key.F5, Keys.Function5);
            _keyMap.Map(Key.F6, Keys.Function6);
            _keyMap.Map(Key.F7, Keys.Function7);
            _keyMap.Map(Key.F8, Keys.Function8);
            _keyMap.Map(Key.F9, Keys.Function9);
            _keyMap.Map(Key.Enter, Keys.Return);
            _keyMap.Map(Key.Space, Keys.Space);
            _keyMap.Map(Key.OemComma, Keys.LessThan);
            _keyMap.Map(Key.OemPeriod, Keys.GreaterThan);

            _keyMap.Map(Key.NumPad4, Keys.Joy0Left);
            _keyMap.Map(Key.NumPad6, Keys.Joy0Right);
            _keyMap.Map(Key.NumPad8, Keys.Joy0Up);
            _keyMap.Map(Key.NumPad2, Keys.Joy0Down);
            _keyMap.Map(Key.NumPad0, Keys.Joy0Fire2);
            _keyMap.Map(Key.NumPad5, Keys.Joy0Fire1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitKeyboardMap();

            DataContext = _logic;

            StartAudio();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _logic.CloseAll();

            StopAudio();
        }

        private void _exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.CloseAll();

            Close();
        }

        private void _driveAButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.LoadDisc(0);
        }

        private void _driveBButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.LoadDisc(1);
        }

        private void _tapeButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.LoadTape();
        }

        private void _pauseButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.Pause();
        }

        private void _resumeButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.Resume();
        }

        private void _resetButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.Reset();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemTilde)
            {
                _logic.EnableTurbo(true);
            }

            byte? cpcKey = _keyMap.GetKey(e.Key);
            if (cpcKey.HasValue)
            {
                _logic.Key(cpcKey.Value, true);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemTilde)
            {
                _logic.EnableTurbo(false);
            }

            byte? cpcKey = _keyMap.GetKey(e.Key);
            if (cpcKey.HasValue)
            {
                _logic.Key(cpcKey.Value, false);
            }
        }

        private void _openButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.OpenMachine();
        }

        private void _machineTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem tabItem = (TabItem)_machineTabControl.SelectedItem;
            if (tabItem != null)
            {
                _logic.Machine = (Machine)tabItem.DataContext;
            }
        }

        private void _addBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.AddBookmark();
        }

        private void _seekToLastBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.SeekToLastBookmark();
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.Close();
        }

        private void _openMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.OpenMachine();
        }

        private void _closeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.Close();
        }

        private void _pauseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.Pause();
        }

        private void _resumeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.Resume();
        }

        private void _resetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.Reset();
        }

        private void _addBookmarkMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.AddBookmark();
        }

        private void _previousBookmarkMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.SeekToLastBookmark();
        }

        private void _driveAMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.LoadDisc(0);
        }

        private void _driveBMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.LoadDisc(1);
        }

        private void _tapeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.LoadTape();
        }

        // IUserInterface implementation
        public void AddMachine(Machine machine)
        {
            MachineTabItem machineTabItem = new MachineTabItem(machine);

            _machineTabControl.Items.Add(machineTabItem);
            _machineTabControl.SelectedItem = machineTabItem;
        }

        public void RemoveMachine(Machine machine)
        {
            List<TabItem> tabs = new List<TabItem>();
            foreach (TabItem tabItem in _machineTabControl.Items)
            {
                if (tabItem.DataContext == machine)
                {
                    tabs.Add(tabItem);
                }
            }

            foreach (TabItem tabItem in tabs)
            {
                _machineTabControl.Items.Remove(tabItem);
            }
        }

        public string PromptForFile(FileTypes type, bool existing)
        {
            string defaultExtension;
            string fileFilter;
            string initialFolder;

            switch (type)
            {
                case FileTypes.Disc:
                    defaultExtension = "zip";
                    fileFilter = "Disc files (*.dsk;*.zip)|*.dsk;*.zip|All files (*.*)|*.*";
                    initialFolder = Settings.DiscsFolder;
                    break;
                case FileTypes.Tape:
                    defaultExtension = "zip";
                    fileFilter = "Tape files (*.cdt;*.tzx;*.zip)|*.cdt;*.tzx;*.zip|All files (*.*)|*.*";
                    initialFolder = Settings.TapesFolder;
                    break;
                case FileTypes.Machine:
                    defaultExtension = "cpvc";
                    fileFilter = "CPvC files (*.cpvc)|*.cpvc|All files (*.*)|*.*";
                    initialFolder = Settings.MachinesFolder;
                    break;
                default:
                    throw new Exception(String.Format("Unknown FileTypes value {0}.", type));
            }

            using (System.Windows.Forms.FileDialog fileDialog = existing ? ((System.Windows.Forms.FileDialog)new System.Windows.Forms.OpenFileDialog()) : ((System.Windows.Forms.FileDialog)new System.Windows.Forms.SaveFileDialog()))
            {
                fileDialog.DefaultExt = defaultExtension;
                fileDialog.AddExtension = true;

                if (initialFolder != null)
                {
                    fileDialog.InitialDirectory = initialFolder;
                }


                fileDialog.Filter = fileFilter;

                System.Windows.Forms.DialogResult r = fileDialog.ShowDialog();
                if (r != System.Windows.Forms.DialogResult.OK)
                {
                    return null;
                }

                // "Remember to the selected file.
                string filename = fileDialog.FileName;
                string folder = System.IO.Path.GetDirectoryName(filename);
                switch (type)
                {
                    case FileTypes.Disc:
                        Settings.DiscsFolder = folder;
                        break;
                    case FileTypes.Tape:
                        Settings.TapesFolder = folder;
                        break;
                    case FileTypes.Machine:
                        Settings.MachinesFolder = folder;
                        break;
                }

                return filename;
            }
        }

        public string SelectItem(List<string> items)
        {
            SelectItemWindow dialog = new SelectItemWindow
            {
                Owner = this
            };

            dialog.SetListItems(items);

            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return dialog.GetSelectedItem();
            }

            return null;
        }

        public Machine GetActiveMachine()
        {
            TabItem tabItem = (TabItem)_machineTabControl.SelectedItem;
            if (tabItem != null)
            {
                return (Machine)tabItem.DataContext;
            }

            return null;
        }

        public void OpenProperties()
        {
            Machine machine = _logic.Machine;

            using (MachinePropertiesWindow dialog = new MachinePropertiesWindow(this, machine))
            using (machine.AutoPause())
            {
                // Set a checkpoint here so the UI shows the current timeline position correctly.
                machine.SetCheckpoint();

                dialog.ShowDialog();
            }
        }

        public HistoryEvent PromptForBookmark()
        {
            Machine machine = _logic.Machine;

            using (BookmarkSelectWindow dialog = new BookmarkSelectWindow(this, machine))
            using (machine.AutoPause())
            {
                // Set a checkpoint here so the UI shows the current timeline position correctly.
                machine.SetCheckpoint();

                bool? result = dialog.ShowDialog();
                if (result.HasValue && result.Value && dialog.SelectedEvent != null)
                {
                    return dialog.SelectedEvent;
                }

                return null;
            }
        }

        public void ReportError(string message)
        {
            // Need to replace this with a messagebox that is centred over its parent. This option
            // doesn't seem to be available with MessageBox.Show().
            MessageBox.Show(this, message, "CPvC", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void _propertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.OpenProperties();
        }

        private void _newMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.NewMachine();
        }

        private void _jumpToBookmarkMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _logic.SelectBookmark();
        }
    }
}
