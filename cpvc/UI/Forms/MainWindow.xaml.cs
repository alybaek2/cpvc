using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private readonly FileSystem _fileSystem;
        private Audio _audio;
        private KeyboardMapping _keyMap;

        private readonly ISettings _settings;

        private readonly MainViewModel _mainViewModel;

        private IWavePlayer _wavePlayer;

        public MainWindow()
        {
            _settings = new Settings();
            _fileSystem = new FileSystem();
            _mainViewModel = new MainViewModel(_settings, _fileSystem);

            _mainViewModel.PromptForFile += MainViewModel_PromptForFile;
            _mainViewModel.SelectItem += MainViewModel_SelectItem;
            _mainViewModel.PromptForBookmark += MainViewModel_PromptForBookmark;
            _mainViewModel.PromptForName += MainViewModel_PromptForName;
            _mainViewModel.SelectRemoteMachine += MainViewModel_SelectRemoteMachine;
            _mainViewModel.SelectServerPort += MainViewModel_SelectServerPort;
            _mainViewModel.ConfirmClose += MainViewModel_ConfirmClose;
            _mainViewModel.CreateSocket += MainViewModel_CreateSocket;

            InitializeComponent();

            _audio = new Audio(_mainViewModel.ReadAudio);

            // Create audio device
            WaveOutEvent waveOut = new WaveOutEvent
            {
                DeviceNumber = -1,
                DesiredLatency = 70
            };

            waveOut.Init(_audio);
            _wavePlayer = waveOut;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_wavePlayer != null)
            {
                _wavePlayer.Pause();
                _wavePlayer.Dispose();
                _wavePlayer = null;

                _audio.Dispose();
                _audio = null;
            }
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
            _keyMap.Map(Key.NumPad0, Keys.Function0);
            _keyMap.Map(Key.NumPad1, Keys.Function1);
            _keyMap.Map(Key.NumPad2, Keys.Function2);
            _keyMap.Map(Key.NumPad3, Keys.Function3);
            _keyMap.Map(Key.NumPad4, Keys.Function4);
            _keyMap.Map(Key.NumPad5, Keys.Function5);
            _keyMap.Map(Key.NumPad6, Keys.Function6);
            _keyMap.Map(Key.NumPad7, Keys.Function7);
            _keyMap.Map(Key.NumPad8, Keys.Function8);
            _keyMap.Map(Key.NumPad9, Keys.Function9);
            _keyMap.Map(Key.Enter, Keys.Return);
            _keyMap.Map(Key.Space, Keys.Space);
            _keyMap.Map(Key.OemComma, Keys.LessThan);
            _keyMap.Map(Key.OemPeriod, Keys.GreaterThan);

            //_keyMap.Map(Key.NumPad4, Keys.Joy0Left);
            //_keyMap.Map(Key.NumPad6, Keys.Joy0Right);
            //_keyMap.Map(Key.NumPad8, Keys.Joy0Up);
            //_keyMap.Map(Key.NumPad2, Keys.Joy0Down);
            //_keyMap.Map(Key.NumPad0, Keys.Joy0Fire2);
            //_keyMap.Map(Key.NumPad5, Keys.Joy0Fire1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitKeyboardMap();

            DataContext = _mainViewModel;

            _wavePlayer.Play();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_mainViewModel.CloseAll())
            {
                e.Cancel = true;
            }
            else
            {
                _wavePlayer.Pause();
                _wavePlayer = null;
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                _mainViewModel.ReverseStartCommand.Execute(_mainViewModel.ActiveMachine);
            }
            else if (e.Key == Key.F2)
            {
                if (_mainViewModel.ActiveMachine is ITurboableMachine machine)
                {
                    _mainViewModel.EnableTurbo(machine, true);
                }
            }

            byte? cpcKey = _keyMap.GetKey(e.Key);
            if (cpcKey.HasValue)
            {
                if (_mainViewModel.ActiveMachine is IInteractiveMachine machine)
                {
                    _mainViewModel.KeyPress(machine, cpcKey.Value, true);
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                _mainViewModel.ReverseStopCommand.Execute(_mainViewModel.ActiveMachine);
            }
            else if (e.Key == Key.F2)
            {
                if (_mainViewModel.ActiveMachine is ITurboableMachine machine)
                {
                    _mainViewModel.EnableTurbo(machine, false);
                }
            }

            byte? cpcKey = _keyMap.GetKey(e.Key);
            if (cpcKey.HasValue)
            {
                if (_mainViewModel.ActiveMachine is IInteractiveMachine machine)
                {
                    _mainViewModel.KeyPress(machine, cpcKey.Value, false);
                }
            }
        }

        private string PromptForFile(FileTypes type, bool existing)
        {
            using (System.Windows.Forms.FileDialog fileDialog = existing ? ((System.Windows.Forms.FileDialog)new System.Windows.Forms.OpenFileDialog()) : ((System.Windows.Forms.FileDialog)new System.Windows.Forms.SaveFileDialog()))
            {
                switch (type)
                {
                    case FileTypes.Disc:
                        fileDialog.DefaultExt = "zip";
                        fileDialog.Filter = "Disc files (*.dsk;*.zip)|*.dsk;*.zip|All files (*.*)|*.*";
                        break;
                    case FileTypes.Tape:
                        fileDialog.DefaultExt = "zip";
                        fileDialog.Filter = "Tape files (*.cdt;*.tzx;*.zip)|*.cdt;*.tzx;*.zip|All files (*.*)|*.*";
                        break;
                    case FileTypes.Machine:
                        fileDialog.DefaultExt = "cpvc";
                        fileDialog.Filter = "CPvC files (*.cpvc)|*.cpvc|All files (*.*)|*.*";
                        break;
                    default:
                        throw new Exception(String.Format("Unknown FileTypes value {0}.", type));
                }

                fileDialog.AddExtension = true;

                string initialFolder = _settings.GetFolder(type);
                if (initialFolder != null)
                {
                    fileDialog.InitialDirectory = initialFolder;
                }

                System.Windows.Forms.DialogResult result = fileDialog.ShowDialog();
                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    return null;
                }

                // Remember the last folder for the selected filetype.
                string folder = System.IO.Path.GetDirectoryName(fileDialog.FileName);
                _settings.SetFolder(type, folder);

                return fileDialog.FileName;
            }
        }

        private string SelectItem(List<string> items)
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

        private HistoryEvent PromptForBookmark()
        {
            LocalMachine machine = _mainViewModel?.ActiveMachine as LocalMachine;

            using (machine.AutoPause())
            {
                BookmarkSelectWindow dialog = new BookmarkSelectWindow(this, machine);
                bool? result = dialog.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    if (dialog.SelectedReplayEvent != null)
                    {
                        string name = String.Format("{0} (Replay)", machine.Name);
                        _mainViewModel.OpenReplayMachine(name, dialog.SelectedReplayEvent);

                        return null;
                    }
                    else if (dialog.SelectedJumpEvent is BookmarkHistoryEvent bookmarkHistoryEvent)
                    {
                        return bookmarkHistoryEvent;
                    }
                }

                return null;
            }
        }

        private string PromptForName(string existingName)
        {
            RenameWindow dialog = new RenameWindow(this, existingName);
            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return dialog.NewName;
            }

            return null;
        }

        private UInt16? SelectServerPort(UInt16 defaultPort)
        {
            StartServerWindow dialog = new StartServerWindow(this, defaultPort);
            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return dialog.Port;
            }

            return null;
        }

        private RemoteMachine SelectRemoteMachine(ServerInfo serverInfo)
        {
            bool? result;
            if (serverInfo == null)
            {
                ConnectWindow connectWindow = new ConnectWindow(this);
                result = connectWindow.ShowDialog();

                if ((result.HasValue && !result.Value) || connectWindow.ServerNameAndPort.Length <= 0)
                {
                    return null;
                }

                string[] tokens = connectWindow.ServerNameAndPort.Split(':');
                UInt16 port = 6128;
                if (tokens.Length > 1)
                {
                    port = Convert.ToUInt16(tokens[1]);
                }

                serverInfo = new ServerInfo(tokens[0], port);

                if (!_mainViewModel.RecentServers.Any(s => s.ServerName == serverInfo.ServerName && s.Port == serverInfo.Port))
                {
                    _mainViewModel.RecentServers.Add(new ServerInfo(serverInfo.ServerName, serverInfo.Port));
                }
            }

            RemoteWindow dialog = new RemoteWindow(this, serverInfo);
            result = dialog.ShowDialog();

            return (result.HasValue && result.Value) ? dialog.Machine : null;
        }

        private void MachinePreviewGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is IMachine machine)
            {
                _mainViewModel.OpenCommand.Execute(machine);
                _mainViewModel.ActiveMachine = machine;
            }
        }

        private void ScreenGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is IMachine machine)
            {
                _mainViewModel.ToggleRunningCommand.Execute(machine);
            }
        }

        private void CollectionViewSource_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            if (e.Item is IMachine machine)
            {
                e.Accepted = !(machine is IPersistableMachine persistableMachine) || persistableMachine.IsOpen;
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void MainViewModel_ConfirmClose(object sender, ConfirmCloseEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(this, e.Message, "CPvC", MessageBoxButton.YesNo, MessageBoxImage.Question);

            e.Result = result == MessageBoxResult.Yes;
        }

        private void MainViewModel_CreateSocket(object sender, CreateSocketEventArgs e)
        {
            e.CreatedSocket = new Socket();
        }

        private void MainViewModel_PromptForBookmark(object sender, PromptForBookmarkEventArgs e)
        {
            e.SelectedBookmark = PromptForBookmark();
        }

        private void MainViewModel_PromptForFile(object sender, PromptForFileEventArgs e)
        {
            e.Filepath = PromptForFile(e.FileType, e.Existing);
        }

        private void MainViewModel_PromptForName(object sender, PromptForNameEventArgs e)
        {
            e.SelectedName = PromptForName(e.ExistingName);
        }

        private void MainViewModel_SelectItem(object sender, SelectItemEventArgs e)
        {
            e.SelectedItem = SelectItem(e.Items);
        }

        private void MainViewModel_SelectRemoteMachine(object sender, SelectRemoteMachineEventArgs e)
        {
            e.SelectedMachine = SelectRemoteMachine(e.ServerInfo);
        }

        private void MainViewModel_SelectServerPort(object sender, SelectServerPortEventArgs e)
        {
            e.SelectedPort = SelectServerPort(e.DefaultPort);
        }
    }
}
