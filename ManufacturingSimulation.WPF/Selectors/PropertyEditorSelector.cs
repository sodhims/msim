using System;
using System.Windows;
using System.Windows.Controls;
using ManufacturingSimulation.WPF.ViewModels.Admin;

namespace ManufacturingSimulation.WPF.Selectors
{
    public class PropertyEditorSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PropertyViewModel propertyVM && container is FrameworkElement element)
            {
                var propertyType = propertyVM.PropertyInfo.PropertyType;

                // Try to find the appropriate template, but don't crash if missing
                try
                {
                    // Boolean
                    if (propertyType == typeof(bool) || propertyType == typeof(bool?))
                    {
                        return element.TryFindResource("BooleanEditorTemplate") as DataTemplate;
                    }

                    // DateTime
                    if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                    {
                        return element.TryFindResource("DateTimeEditorTemplate") as DataTemplate;
                    }

                    // Numeric types
                    if (propertyType == typeof(int) || propertyType == typeof(int?) ||
                        propertyType == typeof(double) || propertyType == typeof(double?) ||
                        propertyType == typeof(decimal) || propertyType == typeof(decimal?))
                    {
                        return element.TryFindResource("NumericEditorTemplate") as DataTemplate;
                    }

                    // Default: TextBox
                    return element.TryFindResource("TextEditorTemplate") as DataTemplate;
                }
                catch
                {
                    // If template not found, return null - WPF will use default
                    return null;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}