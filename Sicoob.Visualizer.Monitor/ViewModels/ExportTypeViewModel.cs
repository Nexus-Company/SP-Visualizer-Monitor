using Sicoob.Visualizer.Monitor.Comuns.Models.Enums;
using System.Collections.ObjectModel;

namespace Sicoob.Visualizer.Monitor.Wpf.ViewModels;
public class ExportTypeViewModel
{
    public ObservableCollection<Type> Types { get; set; }
    public ExportTypes Checked { get => Types.FirstOrDefault(tp => tp.IsChecked)?.ExportType ?? ExportTypes.Xlsx; }
    public ExportTypeViewModel()
    {
        Types = new ObservableCollection<Type>
            {
                new Type(ExportTypes.Xlsx, isChecked: true),
                new Type(ExportTypes.Csv),
                new Type(ExportTypes.Pdf),
                new Type(ExportTypes.Html,false),
            };
    }

    public class Type
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsChecked { get; set; }
        public ExportTypes ExportType { get; set; }
        public Type(ExportTypes type, bool isEnabled = true, bool isChecked = false)
        {
            ExportType = type;
            IsEnabled = isEnabled;
            IsChecked = isChecked;
            Name = Enum.GetName(type) ?? string.Empty;
        }
    }
}