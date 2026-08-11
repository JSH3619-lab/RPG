using System.IO.Compression;
using System.Text;
using RamosPartGenerator.Excel;

namespace RamosPartGenerator.Tests;

public class ErpRegisteredPartsReaderTests
{
    [Fact]
    public void Read_SharedStrings_UsesLastHeaderRowAndTrimsAndSkipsEmptyCells()
    {
        var sharedStringsXml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" count="4" uniqueCount="4">
              <si><t>검색조건</t></si>
              <si><t>품목코드</t></si>
              <si><t>RMRDAG58A1A-CDWRRWM7G</t></si>
              <si><t xml:space="preserve"> CODE-WITH-SPACES </t></si>
            </sst>
            """;
        var sheetXml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>
                <row r="1"><c r="A1" t="s"><v>1</v></c><c r="B1" t="s"><v>0</v></c></row>
                <row r="4"><c r="A4" t="s"><v>0</v></c></row>
                <row r="8"><c r="A8" t="s"><v>1</v></c></row>
                <row r="9"><c r="A9" t="s"><v>2</v></c></row>
                <row r="10"><c r="A10" t="s"><v>3</v></c></row>
                <row r="11"><c r="A11" t="s"/></row>
                <row r="12"><c r="A12" t="s"><v>2</v></c></row>
              </sheetData>
            </worksheet>
            """;
        var tempPath = Path.Combine(Path.GetTempPath(), $"erp-reader-test-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var stream = BuildWorkbook(sheetXml, sharedStringsXml))
            {
                File.WriteAllBytes(tempPath, stream.ToArray());
            }

            var codes = new ErpRegisteredPartsReader().Read(tempPath);

            Assert.Equal(2, codes.Count);
            Assert.Contains("RMRDAG58A1A-CDWRRWM7G", codes);
            Assert.Contains("CODE-WITH-SPACES", codes);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Read_InlineStrings_FindsHeaderWithTrimAndIgnoresEmptyValues()
    {
        var sheetXml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>
                <row r="1"><c r="A1" t="inlineStr"><is><t>조회 조건</t></is></c></row>
                <row r="3"><c r="A3" t="inlineStr"><is><t xml:space="preserve">품목코드 </t></is></c></row>
                <row r="4"><c r="A4" t="inlineStr"><is><t>PART-A</t></is></c></row>
                <row r="5"><c r="A5" t="inlineStr"><is><t></t></is></c></row>
                <row r="6"><c r="A6" t="inlineStr"><is><t xml:space="preserve"> PART-B </t></is></c></row>
              </sheetData>
            </worksheet>
            """;

        using var stream = BuildWorkbook(sheetXml);
        var codes = new ErpRegisteredPartsReader().Read(stream);

        Assert.Equal(new HashSet<string> { "PART-A", "PART-B" }, codes);
    }

    [Fact]
    public void Read_WithoutPartCodeHeader_Throws()
    {
        var sheetXml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>
                <row r="1"><c r="A1" t="inlineStr"><is><t>다른 헤더</t></is></c></row>
                <row r="2"><c r="A2" t="inlineStr"><is><t>PART-A</t></is></c></row>
              </sheetData>
            </worksheet>
            """;

        using var stream = BuildWorkbook(sheetXml);

        Assert.Throws<InvalidOperationException>(() => new ErpRegisteredPartsReader().Read(stream));
    }

    private static MemoryStream BuildWorkbook(string sheetXml, string? sharedStringsXml = null)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="품목정보등록(Multi)(S)" sheetId="1" r:id="rId1"/>
                  </sheets>
                </workbook>
                """);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);
            if (sharedStringsXml is not null)
            {
                WriteEntry(archive, "xl/sharedStrings.xml", sharedStringsXml);
            }

            WriteEntry(archive, "xl/worksheets/sheet1.xml", sheetXml);
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
