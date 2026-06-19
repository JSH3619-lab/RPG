using System.IO.Compression;
using System.Globalization;
using System.Security;
using System.Text;
using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Excel;

public sealed class RegistrationExcelExporter
{
    public string DefaultFileName => $"DRAM 품목정보({DateTime.Now:yyMMdd}).xlsx";

    public byte[] Export(IReadOnlyList<GeneratedPartRow> rows)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypesXml);
            WriteEntry(archive, "_rels/.rels", RootRelsXml);
            WriteEntry(archive, "xl/workbook.xml", WorkbookXml);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelsXml);
            WriteEntry(archive, "xl/styles.xml", StylesXml);
            WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(rows));
        }

        return stream.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string BuildWorksheetXml(IReadOnlyList<GeneratedPartRow> rows)
    {
        var sheetData = new StringBuilder();
        var includeSalesCode = rows.Any(row => IsModuleKind(row.Kind));
        var headers = includeSalesCode
            ? new[] { "구분", "품목코드", "품목명", "영업코드", "품목일반정보", "품목규격" }
            : new[] { "구분", "품목코드", "품목명", "품목일반정보", "품목규격" };
        var dataRows = rows.Select(row => BuildRowValues(row, includeSalesCode)).ToArray();

        AppendRow(sheetData, 1, headers, 1);

        for (var index = 0; index < dataRows.Length; index++)
        {
            AppendRow(sheetData, index + 2, dataRows[index], 0);
        }

        return $$"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <sheetViews>
            <sheetView workbookViewId="0"/>
          </sheetViews>
          <sheetFormatPr defaultRowHeight="15"/>
          <cols>
        {{BuildColumnsXml(headers, dataRows, includeSalesCode)}}
          </cols>
          <sheetData>
        {{sheetData}}
          </sheetData>
        </worksheet>
        """;
    }

    private static IReadOnlyList<string> BuildRowValues(GeneratedPartRow row, bool includeSalesCode)
    {
        return includeSalesCode
            ? new[]
            {
                FormatKind(row.Kind),
                row.PartCode,
                row.Name,
                string.Empty,
                row.GeneralInfo,
                row.Specification
            }
            : new[]
            {
                FormatKind(row.Kind),
                row.PartCode,
                row.Name,
                row.GeneralInfo,
                row.Specification
            };
    }

    private static string BuildColumnsXml(IReadOnlyList<string> headers, IReadOnlyList<string>[] dataRows, bool includeSalesCode)
    {
        var minWidths = includeSalesCode
            ? new[] { 8d, 18d, 18d, 10d, 14d, 24d }
            : new[] { 8d, 18d, 18d, 14d, 24d };
        var maxWidths = includeSalesCode
            ? new[] { 18d, 42d, 42d, 20d, 36d, 255d }
            : new[] { 18d, 42d, 42d, 36d, 255d };

        var builder = new StringBuilder();
        for (var index = 0; index < headers.Count; index++)
        {
            var column = index + 1;
            var longestTextWidth = Math.Max(
                GetTextWidth(headers[index]),
                dataRows.Length == 0 ? 0d : dataRows.Max(row => GetTextWidth(row[index])));
            var width = Math.Clamp(longestTextWidth + 2d, minWidths[index], maxWidths[index]);
            builder.AppendLine($"            <col min=\"{column}\" max=\"{column}\" width=\"{FormatWidth(width)}\" customWidth=\"1\"/>");
        }

        return builder.ToString().TrimEnd();
    }

    private static double GetTextWidth(string? value)
    {
        return (value ?? string.Empty)
            .Replace('\u00A0', ' ')
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .Select(line => line.Sum(character => character <= '\x007F' ? 1d : 1.7d))
            .DefaultIfEmpty(0d)
            .Max();
    }

    private static string FormatWidth(double width)
    {
        return width.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static void AppendRow(StringBuilder builder, int rowIndex, IReadOnlyList<string> values, uint styleIndex)
    {
        builder.Append($"    <row r=\"{rowIndex}\">");
        for (var columnIndex = 0; columnIndex < values.Count; columnIndex++)
        {
            var cellRef = $"{GetColumnName(columnIndex + 1)}{rowIndex}";
            var escaped = Escape(values[columnIndex]);
            builder.Append($"<c r=\"{cellRef}\" t=\"inlineStr\" s=\"{styleIndex}\"><is><t>{escaped}</t></is></c>");
        }
        builder.AppendLine("</row>");
    }

    private static string GetColumnName(int columnNumber)
    {
        var dividend = columnNumber;
        var columnName = string.Empty;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private static string FormatKind(string kind)
    {
        return kind switch
        {
            "Incoming" or "입고" => "입고",
            "Comp" => "Comp",
            "Comp BIN" => "Comp BIN",
            "Module" => "MDL",
            "Module Dummy" => "MDL Dummy",
            "Module BIN" => "MDL BIN",
            _ => kind
        };
    }

    private static bool IsModuleKind(string kind)
    {
        return string.Equals(kind, "Module", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(kind, "Module Dummy", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(kind, "Module BIN", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(kind, "MDL", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(kind, "MDL Dummy", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(kind, "MDL BIN", StringComparison.OrdinalIgnoreCase);
    }

    private static string Escape(string? value) => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

    private const string ContentTypesXml = """
    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
    <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
      <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
      <Default Extension="xml" ContentType="application/xml"/>
      <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
      <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
      <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
    </Types>
    """;

    private const string RootRelsXml = """
    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
    </Relationships>
    """;

    private const string WorkbookXml = """
    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
    <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
              xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
      <sheets>
        <sheet name="등록데이터" sheetId="1" r:id="rId1"/>
      </sheets>
    </workbook>
    """;

    private const string WorkbookRelsXml = """
    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
      <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
    </Relationships>
    """;

    private const string StylesXml = """
    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
    <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
      <fonts count="2">
        <font>
          <sz val="11"/>
          <name val="Arial"/>
        </font>
        <font>
          <b/>
          <sz val="11"/>
          <name val="Arial"/>
          <color rgb="FFFFFFFF"/>
        </font>
      </fonts>
      <fills count="3">
        <fill><patternFill patternType="none"/></fill>
        <fill><patternFill patternType="gray125"/></fill>
        <fill>
          <patternFill patternType="solid">
            <fgColor rgb="FF1F4E78"/>
            <bgColor indexed="64"/>
          </patternFill>
        </fill>
      </fills>
      <borders count="2">
        <border>
          <left/><right/><top/><bottom/><diagonal/>
        </border>
        <border>
          <left style="thin"><color auto="1"/></left>
          <right style="thin"><color auto="1"/></right>
          <top style="thin"><color auto="1"/></top>
          <bottom style="thin"><color auto="1"/></bottom>
          <diagonal/>
        </border>
      </borders>
      <cellStyleXfs count="1">
        <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
      </cellStyleXfs>
      <cellXfs count="2">
        <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1">
          <alignment vertical="center"/>
        </xf>
        <xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1">
          <alignment horizontal="center" vertical="center"/>
        </xf>
      </cellXfs>
      <cellStyles count="1">
        <cellStyle name="Normal" xfId="0" builtinId="0"/>
      </cellStyles>
    </styleSheet>
    """;
}
