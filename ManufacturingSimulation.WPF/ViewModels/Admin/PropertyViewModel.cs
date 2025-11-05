using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ManufacturingSimulation.WPF.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for a single property in the detail editor
    /// </summary>
    public class PropertyViewModel : INotifyPropertyChanged
    {
        private object _editValue;
        private bool _hasChanges;

        public PropertyInfo PropertyInfo { get; set; }
        public object Entity { get; set; }
        public string DisplayName { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsForeignKey { get; set; }
        public bool IsPrimaryKey { get; set; }

        public object OriginalValue => PropertyInfo?.GetValue(Entity);

        public object EditValue
        {
            get
            {
                if (_editValue == null && PropertyInfo != null)
                {
                    _editValue = PropertyInfo.GetValue(Entity);
                }
                return _editValue;
            }
            set
            {
                if (!Equals(_editValue, value))
                {
                    _editValue = value;
                    HasChanges = !Equals(_editValue, OriginalValue);
                    OnPropertyChanged();
                }
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (_hasChanges != value)
                {
                    _hasChanges = value;
                    OnPropertyChanged();
                }
            }
        }

        public Type PropertyType => PropertyInfo?.PropertyType;

        public bool IsNumeric => PropertyType != null && (
            PropertyType == typeof(int) || PropertyType == typeof(int?) ||
            PropertyType == typeof(long) || PropertyType == typeof(long?) ||
            PropertyType == typeof(double) || PropertyType == typeof(double?) ||
            PropertyType == typeof(decimal) || PropertyType == typeof(decimal?) ||
            PropertyType == typeof(float) || PropertyType == typeof(float?)
        );

        public bool IsBoolean => PropertyType == typeof(bool) || PropertyType == typeof(bool?);

        public bool IsDateTime => PropertyType == typeof(DateTime) || PropertyType == typeof(DateTime?);

        public bool IsString => PropertyType == typeof(string);

        public void ApplyChanges()
        {
            if (PropertyInfo != null && Entity != null && !IsReadOnly && HasChanges)
            {
                try
                {
                    // Convert value to correct type if needed
                    var convertedValue = ConvertValue(EditValue, PropertyType);
                    PropertyInfo.SetValue(Entity, convertedValue);
                    HasChanges = false;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to set property {PropertyInfo.Name}: {ex.Message}", ex);
                }
            }
        }

        public void RevertChanges()
        {
            EditValue = OriginalValue;
            HasChanges = false;
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
            {
                // Check if type is nullable
                if (Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType)
                {
                    return null;
                }
                throw new ArgumentException($"Cannot assign null to non-nullable type {targetType.Name}");
            }

            var valueType = value.GetType();
            if (targetType.IsAssignableFrom(valueType))
            {
                return value;
            }

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // String to numeric conversions
            if (value is string stringValue)
            {
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    if (Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType)
                    {
                        return null;
                    }
                }

                if (underlyingType == typeof(int))
                    return int.Parse(stringValue);
                if (underlyingType == typeof(long))
                    return long.Parse(stringValue);
                if (underlyingType == typeof(double))
                    return double.Parse(stringValue);
                if (underlyingType == typeof(decimal))
                    return decimal.Parse(stringValue);
                if (underlyingType == typeof(float))
                    return float.Parse(stringValue);
                if (underlyingType == typeof(bool))
                    return bool.Parse(stringValue);
                if (underlyingType == typeof(DateTime))
                    return DateTime.Parse(stringValue);
            }

            // Try standard conversion
            return Convert.ChangeType(value, underlyingType);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
