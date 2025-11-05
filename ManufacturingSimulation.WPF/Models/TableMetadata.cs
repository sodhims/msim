using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ManufacturingSimulation.WPF.ViewModels.Admin
{
    /// <summary>
    /// Metadata about a database table for the admin interface
    /// </summary>
    public class TableMetadata : INotifyPropertyChanged
    {
        private int _recordCount;

        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public Type EntityType { get; set; }
        public string Category { get; set; }
        public string PrimaryKey { get; set; }
        public bool HasStudentId { get; set; }
        
        public List<string> EditableColumns { get; set; } = new();
        public List<string> ReadOnlyColumns { get; set; } = new();
        public Dictionary<string, ForeignKeyInfo> ForeignKeys { get; set; } = new();
        
        public int RecordCount
        {
            get => _recordCount;
            set
            {
                if (_recordCount != value)
                {
                    _recordCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description { get; set; }
        public int SortOrder { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Information about a foreign key relationship
    /// </summary>
    public class ForeignKeyInfo
    {
        public string ReferencedTable { get; set; }
        public string ReferencedColumn { get; set; }
        public string DisplayColumn { get; set; }
        public Type ReferencedEntityType { get; set; }
    }

    /// <summary>
    /// Category grouping for tables in the tree view
    /// </summary>
    public class TableCategory : INotifyPropertyChanged
    {
        private ObservableCollection<TableMetadata> _tables;

        public string CategoryName { get; set; }
        
        public ObservableCollection<TableMetadata> Tables
        {
            get => _tables;
            set
            {
                if (_tables != value)
                {
                    _tables = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
