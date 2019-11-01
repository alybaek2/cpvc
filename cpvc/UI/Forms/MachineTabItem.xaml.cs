using System.Windows.Controls;
using System.Windows.Input;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for MachineControl.xaml
    /// </summary>
    public partial class MachineTabItem : TabItem
    {
        public MachineTabItem(Machine machine)
        {
            InitializeComponent();

            DataContext = machine;
            _fullScreenImage.Source = machine.Display.Bitmap;
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Machine machine = (Machine)DataContext;
            if (machine == null)
            {
                return;
            }

            machine.ToggleRunning();
        }
    }
}
