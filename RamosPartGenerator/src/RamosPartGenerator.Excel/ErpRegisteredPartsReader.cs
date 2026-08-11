using System.IO.Compression;
using System.Text;
using System.Xml;

namespace RamosPartGenerator.Excel;

public sealed class ErpRegisteredPartsReader
{
    private const string PartCodeHeader = "품목코드";
    private const string RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public HashSet<string> Read(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Read(stream);
    }

    public HashSet<string> Read(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sharedStrings = ReadSharedStrings(archive);
        var columnA = ReadColumnA(GetRequiredEntry(archive, ResolveFirstSheetPath(archive)), sharedStrings);

        var headerIndex = columnA.FindLastIndex(value => value == PartCodeHeader);
        if (headerIndex < 0)
        {
            throw new InvalidOperationException($"'{PartCodeHeader}' 헤더 행을 찾을 수 없습니다. ERP 품목정보등록(Multi)(S) 파일인지 확인해 주세요.");
        }

        var codes = new HashSet<string>(StringComparer.Ordinal);
        for (var index = headerIndex + 1; index < columnA.Count; index++)
        {
            if (columnA[index].Length > 0)
            {
                codes.Add(columnA[index]);
            }
        }

        return codes;
    }

    private static string ResolveFirstSheetPath(ZipArchive archive)
    {
        string? sheetRelationshipId = null;
        using (var reader = CreateReader(GetRequiredEntry(archive, "xl/workbook.xml")))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "sheet")
                {
                    sheetRelationshipId = reader.GetAttribute("id", RelationshipNamespace);
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(sheetRelationshipId))
        {
            throw new InvalidOperationException("워크북에서 시트를 찾을 수 없습니다.");
        }

        string? target = null;
        using (var reader = CreateReader(GetRequiredEntry(archive, "xl/_rels/workbook.xml.rels")))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element &&
                    reader.LocalName == "Relationship" &&
                    reader.GetAttribute("Id") == sheetRelationshipId)
                {
                    target = reader.GetAttribute("Target");
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(target))
        {
            throw new InvalidOperationException("워크북에서 시트 경로를 확인할 수 없습니다.");
        }

        return target.StartsWith("/", StringComparison.Ordinal) ? target.TrimStart('/') : "xl/" + target;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var strings = new List<string>();
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return strings;
        }

        using var reader = CreateReader(entry);
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "si")
            {
                strings.Add(ReadTextContent(reader));
            }
        }

        return strings;
    }

    private static List<string> ReadColumnA(ZipArchiveEntry sheetEntry, IReadOnlyList<string> sharedStrings)
    {
        var values = new List<string>();
        using var reader = CreateReader(sheetEntry);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "c")
            {
                continue;
            }

            var cellReference = reader.GetAttribute("r");
            if (cellReference is null ||
                cellReference.Length < 2 ||
                cellReference[0] != 'A' ||
                !char.IsDigit(cellReference[1]))
            {
                continue;
            }

            values.Add(ReadCellValue(reader, sharedStrings).Trim());
        }

        return values;
    }

    private static string ReadCellValue(XmlReader reader, IReadOnlyList<string> sharedStrings)
    {
        var cellType = reader.GetAttribute("t");
        if (reader.IsEmptyElement)
        {
            return string.Empty;
        }

        var rawValue = string.Empty;
        var inlineText = new StringBuilder();
        using (var subtree = reader.ReadSubtree())
        {
            while (subtree.Read())
            {
                if (subtree.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (subtree.LocalName == "v")
                {
                    rawValue = subtree.ReadElementContentAsString();
                }
                else if (subtree.LocalName == "t")
                {
                    inlineText.Append(subtree.ReadElementContentAsString());
                }
            }
        }

        if (cellType == "s")
        {
            return int.TryParse(rawValue, out var index) && index >= 0 && index < sharedStrings.Count
                ? sharedStrings[index]
                : string.Empty;
        }

        return cellType == "inlineStr" ? inlineText.ToString() : rawValue;
    }

    private static string ReadTextContent(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        using var subtree = reader.ReadSubtree();
        while (subtree.Read())
        {
            while (subtree.NodeType == XmlNodeType.Element && subtree.LocalName == "t")
            {
                builder.Append(subtree.ReadElementContentAsString());
            }
        }

        return builder.ToString();
    }

    private static ZipArchiveEntry GetRequiredEntry(ZipArchive archive, string path)
    {
        return archive.GetEntry(path) ?? throw new InvalidOperationException($"xlsx에서 '{path}'를 찾을 수 없습니다.");
    }

    private static XmlReader CreateReader(ZipArchiveEntry entry)
    {
        return XmlReader.Create(entry.Open(), new XmlReaderSettings { CloseInput = true, IgnoreWhitespace = true });
    }
}
