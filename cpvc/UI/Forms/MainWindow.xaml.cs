﻿using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
            _settings = new Settings();
            _fileSystem = new FileSystem();
            _mainViewModel = new MainViewModel(_settings, _fileSystem, SelectItem, PromptForFile, PromptForBookmark, PromptForName);
            _audio = new Audio(_mainViewModel.ReadAudio);

            InitializeComponent();
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

            DataContext = _mainViewModel;

            StartAudio();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Stop audio prior to closing machines to ensure no audio callbacks are triggered
            // during Dispose calls.
            StopAudio();

            _mainViewModel.CloseAll();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemTilde)
            {
                _mainViewModel.EnableTurbo(true);
            }

            byte? cpcKey = _keyMap.GetKey(e.Key);
            if (cpcKey.HasValue)
            {
                _mainViewModel.Key(cpcKey.Value, true);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemTilde)
            {
                _mainViewModel.EnableTurbo(false);
            }

            byte? cpcKey = _keyMap.GetKey(e.Key);
            if (cpcKey.HasValue)
            {
                _mainViewModel.Key(cpcKey.Value, false);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.Close(null);
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainViewModel.OpenMachine(PromptForFile, null, _fileSystem);
            }
            catch (Exception ex)
            {
                ReportError(ex.Message);
            }
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.Close(null);
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
            Machine machine = _mainViewModel?.ActiveMachineViewModel?.Machine as Machine;

            using (machine.AutoPause())
            {
                // Set a checkpoint here so the UI shows the current timeline position correctly.
                machine.SetCheckpoint();

                using (BookmarkSelectWindow dialog = new BookmarkSelectWindow(this, machine))
                {
                    bool? result = dialog.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        if (dialog.SelectedReplayEvent != null)
                        {
                            _mainViewModel.OpenReplayMachine(machine, dialog.SelectedReplayEvent);

                            return null;
                        }
                        else if (dialog.SelectedJumpEvent?.Bookmark != null)
                        {
                            return dialog.SelectedJumpEvent;
                        }
                    }

                    return null;
                }
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

        private void ReportError(string message)
        {
            // Need to replace this with a messagebox that is centred over its parent. This option
            // doesn't seem to be available with MessageBox.Show().
            MessageBox.Show(this, message, "CPvC", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainViewModel.NewMachine(PromptForFile, _fileSystem);
            }
            catch (Exception ex)
            {
                ReportError(ex.Message);
            }
        }

        private void MachinePreviewGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Machine machine)
            {
                try
                {
                    _mainViewModel.ActiveMachine = machine;
                }
                catch (Exception ex)
                {
                    ReportError(String.Format("Unable to open {0}.\n\n{1}", machine.Name, ex.Message));
                }
            }
        }

        private void ScreenGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ICoreMachine machine)
            {
                _mainViewModel.ActiveMachineViewModel.ToggleRunningCommand.Execute(null);
            }
        }

        private void OpenPreviewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MachinePreviewGrid_MouseLeftButtonUp(sender, null);
        }

        private void RemoveMachineMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MachineViewModel viewModel)
            {
                _mainViewModel.Remove(viewModel);
            }
        }

        private void OpenMachines_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            if (e.Item is Machine machine)
            {
                e.Accepted = !machine.RequiresOpen;
            }
            else
            {
                e.Accepted = false;
            }
        }
    }
}
