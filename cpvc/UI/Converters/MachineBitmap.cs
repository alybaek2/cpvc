using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CPvC.UI.Converters
{
    public class MachineBitmap : IValueConverter
    {
        private readonly ConditionalWeakTable<Machine, WriteableBitmap> _bitmaps;
        static private Int32Rect _drawRect = new Int32Rect(0, 0, Display.Width, Display.Height);

        public MachineBitmap()
        {
            _bitmaps = new ConditionalWeakTable<Machine, WriteableBitmap>();
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (!(value is Machine machine))
            {
                return null;
            }

            if (!_bitmaps.TryGetValue(machine, out WriteableBitmap bitmap))
            {
                bitmap = new WriteableBitmap(Display.Width, Display.Height, 0, 0, PixelFormats.Indexed8, Display.Palette);

                _bitmaps.Add(machine, bitmap);

                if (machine is LocalMachine)
                {
                    machine.PropertyChanged += LocalMachine_PropertyChanged;
                }

                machine.DisplayUpdated += Machine_DisplayUpdated;

                UpdateScreen(machine, bitmap);
            }

            return bitmap;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }

        private void UpdateScreen(Machine machine, WriteableBitmap bitmap)
        {
            Action action = new Action(() => CopyScreen(machine, bitmap));
            bitmap.Dispatcher.BeginInvoke(action, null);
        }

        private void Machine_DisplayUpdated(object sender, EventArgs e)
        {
            Machine machine = (Machine)sender;

            if (_bitmaps.TryGetValue(machine, out WriteableBitmap bitmap))
            {
                UpdateScreen(machine, bitmap);
            }
        }

        private void LocalMachine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            LocalMachine machine = (LocalMachine)sender;
            if (e.PropertyName == nameof(LocalMachine.IsOpen))
            {
                if (_bitmaps.TryGetValue(machine, out WriteableBitmap bitmap))
                {
                    UpdateColour(bitmap, !machine.IsOpen);

                    bitmap.Lock();
                    bitmap.AddDirtyRect(_drawRect);
                    bitmap.Unlock();
                }
            }
        }

        static private void UpdateColour(WriteableBitmap bitmap, bool grey)
        {
            IntPtr buffer = bitmap.BackBuffer;
            int size = bitmap.BackBufferStride * bitmap.PixelHeight;

            for (int i = 0; i < size; i++)
            {
                byte b = Marshal.ReadByte(buffer, i);
                if (grey)
                {
                    b |= 0x20;
                }
                else
                {
                    b &= 0x1f;
                }

                Marshal.WriteByte(buffer, i, b);
            }
        }

        static public void CopyScreen(Machine machine, WriteableBitmap bitmap)
        {
            bitmap.Lock();

            machine.GetScreen(bitmap.BackBuffer, (UInt64)(bitmap.BackBufferStride * bitmap.PixelHeight));

            if (machine is LocalMachine localMachine)
            {
                if (!localMachine.IsOpen)
                {
                    UpdateColour(bitmap, true);
                }
            }

            bitmap.AddDirtyRect(_drawRect);

            bitmap.Unlock();
        }
    }
}
