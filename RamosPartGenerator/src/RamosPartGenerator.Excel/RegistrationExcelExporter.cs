using System.IO.Compression;
using System.Security;
using System.Text;
using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Excel;

public sealed class RegistrationExcelExporter
{
    public string DefaultFileName => $"DRAM 품목정보({DateTime.Now:yyyyMMdd}).xlsx";

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
        var headers = new[] { "구분", "품목코드", "품목명", "품목일반정보", "품목규격" };

        AppendRow(sheetData, 1, headers, 1);

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            AppendRow(sheetData, index + 2, new[]
            {
                FormatKind(row.Kind),
                row.PartCode,
                row.Name,
                row.GeneralInfo,
                row.Specification
            }, 0);
        }

        return $$"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <sheetViews>
            <sheetView workbookViewId="0"/>
          </sheetViews>
          <sheetFormatPr defaultRowHeight="15"/>
          <cols>
            <col min="1" max="1" width="14" customWidth="1"/>
            <col min="2" max="2" width="28" customWidth="1"/>
            <col min="3" max="3" width="28" customWidth="1"/>
            <col min="4" max="4" width="24" customWidth="1"/>
            <col min="5" max="5" width="56" customWidth="1"/>
          </cols>
          <sheetData>
        {{sheetData}}
          </sheetData>
        </worksheet>
        """;
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
            "Module BIN" => "MDL BIN",
            _ => kind
        };
    }

    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;

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
          <name val="Segoe UI"/>
        </font>
        <font>
          <b/>
          <sz val="11"/>
          <name val="Segoe UI"/>
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
          <alignment vertical="center" wrapText="1"/>
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
