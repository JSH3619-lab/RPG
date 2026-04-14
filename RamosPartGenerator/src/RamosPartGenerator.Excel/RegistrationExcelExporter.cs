using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Excel;

public sealed class RegistrationExcelExporter
{
    public string DefaultFileName => $"RamosPartRegistration_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

    public void ExportPlaceholder(string path, IEnumerable<GeneratedPartRow> rows)
    {
        using var writer = new StreamWriter(path, false);
        writer.WriteLine("This is a placeholder export file.");
        foreach (var row in rows)
        {
            writer.WriteLine($"{row.Kind}\t{row.PartCode}\t{row.Name}\t{row.GeneralInfo}\t{row.Specification}");
        }
    }
}
