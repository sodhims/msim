using System.Windows;
using System.Windows.Controls;
using ManufacturingSimulation.WPF.ViewModels.Admin;

namespace ManufacturingSimulation.WPF.Views.Admin
{
    /// <summary>
    /// Selects the appropriate editor template based on property type
    /// </summary>
    public class PropertyEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StringEditorTemplate { get; set; }
        public DataTemplate NumericEditorTemplate { get; set; }
        public DataTemplate BooleanEditorTemplate { get; set; }
        public DataTemplate DateTimeEditorTemplate { get; set; }
        public DataTemplate ReadOnlyTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PropertyViewModel propertyVM)
            {
                if (propertyVM.IsReadOnly)
                {
                    return ReadOnlyTemplate;
                }

                if (propertyVM.IsBoolean)
                {
                    return BooleanEditorTemplate;
                }

                if (propertyVM.IsDateTime)
                {
                    return DateTimeEditorTemplate;
                }

                if (propertyVM.IsNumeric)
                {
                    return NumericEditorTemplate;
                }

                if (propertyVM.IsString)
                {
                    return StringEditorTemplate;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}
