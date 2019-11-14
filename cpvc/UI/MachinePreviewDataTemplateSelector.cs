using System.Windows;
using System.Windows.Controls;

namespace CPvC.UI
{
    public class MachinePreviewDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate
            SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element == null || item == null)
            {
                return null;
            }

            if (item is Machine)
            {
                return element.FindResource("OpenMachinePreview") as DataTemplate;
            }
            else if (item is MachineInfo)
            {
                return element.FindResource("ClosedMachinePreview") as DataTemplate;
            }

            return null;
        }
    }
}
