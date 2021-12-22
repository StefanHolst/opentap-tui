using System.Collections.Generic;
using System.ComponentModel;
using OpenTap.Plugins.BasicSteps;

namespace OpenTap.Tui
{
    [Display("Export Results")]
    public class ExportDialogInput
    {
        public enum ExportSubmit
        {
            Ok = 1,
            Cancel = 2,
        }

        [AvailableValues(nameof(AvailableExporters))]
        public ITableExport Exporter { get; set; }

        [Browsable(false)]
        public List<ITableExport> AvailableExporters { get; set; }
        
        [FilePath(FilePathAttribute.BehaviorChoice.Save)]
        public string Path { get; set; }
        
        [Submit]
        [Layout(LayoutMode.FullRow | LayoutMode.FloatBottom, 1, 1000)]
        public ExportSubmit Submit { get; set; } = ExportSubmit.Ok;
    }
}