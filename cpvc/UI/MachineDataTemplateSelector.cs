using System.Windows;
using System.Windows.Controls;

namespace CPvC
{
    public class MachineDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate selectedTemplate = null;

            switch (item)
            {
                case MachineViewModel _:
                    selectedTemplate = MachineTemplate;
                    break;

                case MainViewModel _:
                    selectedTemplate = HomeTemplate;
                    break;
            }

            return selectedTemplate;
        }

        public DataTemplate MachineTemplate { get; set; }
        public DataTemplate HomeTemplate { get; set; }
    }
}
