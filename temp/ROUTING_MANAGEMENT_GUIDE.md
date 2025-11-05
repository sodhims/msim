# Routing Management Window - Usage Guide

## Overview
The Routing Management Window provides a visual, user-friendly interface for managing product manufacturing routings in your WPF Manufacturing Simulation System.

## Features

### 1. **Product Selection**
- Left panel displays all products for the current student
- Shows product name and description
- Click any product to view/edit its routing

### 2. **Visual Routing Display**
- Each operation shown as a numbered card
- Clear display of:
  - Operation sequence number (in blue circle)
  - Work center assignment
  - Cycle time
  - Setup time

### 3. **Add Operations**
- Click "➕ Add Operation" button
- New operation added with default values:
  - Next sequential number
  - First work center selected
  - 10 minutes cycle time
  - 5 minutes setup time

### 4. **Edit Operations**
- **Work Center**: Select from dropdown
- **Cycle Time**: Edit directly in text box
- **Setup Time**: Edit directly in text box
- All changes tracked automatically

### 5. **Reorder Operations**
- 🔼 Move Up: Move operation earlier in sequence
- 🔽 Move Down: Move operation later in sequence
- Sequence numbers automatically renumbered

### 6. **Delete Operations**
- 🗑️ Delete button on each operation
- Confirmation dialog prevents accidental deletion
- Remaining operations automatically renumbered

### 7. **Save Changes**
- "Save All Changes" button in header
- Only enabled when there are unsaved changes
- Saves all routing modifications to database
- Confirmation message on success

### 8. **Change Tracking**
- Window tracks all modifications
- "Unsaved changes" indicator in status bar
- Prompt on close if changes not saved

## How to Open

```csharp
// From your main window or menu
var routingWindow = new RoutingManagementWindow(_context, _currentStudent);
routingWindow.ShowDialog();
```

## Integration Example

Add a menu item or button to your main window:

```xml
<Button Content="Manage Routings" 
        Click="ManageRoutings_Click"
        Style="{StaticResource PrimaryButtonStyle}"/>
```

```csharp
private void ManageRoutings_Click(object sender, RoutedEventArgs e)
{
    var routingWindow = new RoutingManagementWindow(_context, _currentStudent);
    routingWindow.ShowDialog();
    
    // Optionally refresh your data after closing
    RefreshData();
}
```

## Data Model Requirements

The window expects these entities in your database context:

### Product
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int StudentId { get; set; }
}
```

### WorkCenter
```csharp
public class WorkCenter
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int StudentId { get; set; }
}
```

### Routing
```csharp
public class Routing
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int WorkCenterId { get; set; }
    public int OperationSeq { get; set; }
    public double CycleTimeMinutes { get; set; }
    public double SetupTimeMinutes { get; set; }
}
```

### Student
```csharp
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

## Best Practices

1. **Always save changes** before closing or switching products
2. **Verify work center assignments** - ensure correct machines selected
3. **Set realistic times** - cycle and setup times affect simulation accuracy
4. **Logical sequencing** - order operations to match real manufacturing flow
5. **Test in simulation** - run simulation after routing changes to verify

## Troubleshooting

### No Products Showing
- Verify current student has products in database
- Check StudentId filtering is correct

### Cannot Add Operations
- Ensure product is selected
- Verify work centers exist for student

### Changes Not Saving
- Check database connection
- Verify EF Core context is properly configured
- Look for validation errors in output window

### Work Center Dropdown Empty
- Ensure work centers exist for current student
- Check WorkCenters table has data

## Advanced Customization

### Styling
All styles defined in Window.Resources can be customized:
- `OperationCardStyle` - Operation card appearance
- `PrimaryButtonStyle` - Main action buttons
- `SecondaryButtonStyle` - Secondary buttons
- `IconButtonStyle` - Up/down/delete buttons

### Validation
Add validation by extending `RoutingOperationViewModel`:

```csharp
public double CycleTimeMinutes
{
    get => Routing.CycleTimeMinutes;
    set
    {
        if (value < 0)
        {
            MessageBox.Show("Cycle time must be positive");
            return;
        }
        if (Routing.CycleTimeMinutes != value)
        {
            Routing.CycleTimeMinutes = value;
            OnPropertyChanged();
        }
    }
}
```

### Additional Features
Consider adding:
- Bulk import/export routings
- Copy routing from one product to another
- Routing templates
- Operation cost calculations
- Visual flowchart view

## Status Bar Information

Bottom status bar shows:
- **Left**: Current student name
- **Right**: Current operation status
  - "Ready" - No action
  - "Unsaved changes" - Modifications pending
  - "Loaded X operations" - After product selection
  - Error messages if operations fail

## Keyboard Shortcuts

While not implemented by default, you can add:
- Ctrl+S: Save changes
- Delete: Delete selected operation
- Ctrl+Up/Down: Move operation
- Ctrl+N: Add new operation

## Performance Notes

- Window loads all products and work centers on startup
- Operations loaded only when product selected
- Changes tracked in memory until saved
- Database updated only on explicit save

## Future Enhancements

Possible additions:
1. Drag-and-drop reordering
2. Duplicate operation feature
3. Batch editing multiple operations
4. Routing validation rules
5. Visual routing flowchart
6. Export routing to Excel/PDF
7. Routing comparison view
8. Historical routing versions
