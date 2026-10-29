using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;
using RamosPartGenerator.Excel;

// RamosPartGenerator 헤드리스 CLI. GUI와 같은 Core 로직을 호출한다(로직 하나, 입구 둘).
// 쓰기는 --xlsx 출력 파일뿐이다. ERP 캐시·PGM Export 이력(pgm-exported-cache.json)은 읽지도 쓰지도 않는다.
// 종료 코드: 0 성공 / 1 일부 실패(확인 필요 포함) / 2 입력·실행 오류
Console.OutputEncoding = new UTF8Encoding(false);
try
{
    return Cli.Run(args);
}
catch (CliInputException ex)
{
    Console.Error.WriteLine($"입력 오류: {ex.Message}");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"실행 오류: {ex.Message}");
    return 2;
}

internal sealed class CliInputException : Exception
{
    public CliInputException(string message) : base(message) { }
}

internal static class Cli
{
    private const string Revision = "30";

    private const string Usage = """
        RamosPartGenerator CLI

        사용법:
          generate <파트코드...|--in 목록파일> [--kind module|comp|lpddr] [--xlsx 출력] [--json]
          parse <파트코드...> [--kind module|comp|lpddr] [--json]
          validate <파트코드...|--in 목록파일> [--kind module|comp|lpddr] [--xlsx 출력] [--json]
          convert-legacy <구형코드...|--in 목록파일> --ic-brand S|G [--json]

        공통 옵션:
          --specs <dir>  규칙 폴더 (기본: exe 옆 specs\, 없는 파일은 내장 규칙)
          --kind         module(기본, Module Full Part) / comp(Comp Part) / lpddr
          --xlsx <파일>  등록 양식 Excel 저장 (generate/validate, 기존 파일은 덮어쓰지 않음)
          --json         stdout에 JSON(UTF-8) 출력
          --version      버전 출력

        종료 코드: 0 성공 / 1 일부 실패(확인 필요 포함) / 2 입력·실행 오류
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    // parse 의미 표시용: 요청 필드 -> shared.json code_options 키.
    // ponytail: Desktop DesktopLookupCatalog의 필드 목록과 같은 매핑을 복제했다. 화면 전용(internal)이라 Core로 옮기면 GUI 변경이 필요하다.
    private static readonly Dictionary<string, Dictionary<string, string[]>> MeaningKeys = new()
    {
        ["module"] = new()
        {
            ["ModuleSourceCode"] = new[] { "module_source", "manufacturing_module_source" },
            ["DramTypeCode"] = new[] { "dram_type" },
            ["DimmTypeCode"] = new[] { "dimm_type_common" },
            ["ModuleDensityCode"] = new[] { "module_density" },
            ["BankVddCode"] = new[] { "module_bank_vdd" },
            ["DieDensityCode"] = new[] { "module_die_density" },
            ["RankCode"] = new[] { "rank" },
            ["IcBrandCode"] = new[] { "module_ic_brand" },
            ["ModuleCompTypeCode"] = new[] { "comp_type", "manufacturing_comp_type" },
            ["CompTestCode"] = new[] { "tester" },
            ["ModuleSmtCode"] = new[] { "tester" },
            ["ModuleTestCode"] = new[] { "tester" },
            ["SpeedCode"] = new[] { "speed_ddr4", "speed_ddr5" },
            ["PcbCode"] = new[] { "pcb" },
            ["VendorCode"] = new[] { "vendor", "vendor_tm" },
            ["PurchaserCode"] = new[] { "purchaser", "purchaser_tm" },
            ["A100SpecialCode"] = new[] { "special_code1_table1", "special_code1_table2" },
            ["SpecialCode2Code"] = new[] { "module_special_code2" },
            ["SpecialCode3Code"] = new[] { "module_special_code3" },
            ["GradeCode"] = new[] { "grade_code" },
            ["ProductBinCode"] = new[] { "product_bin" }
        },
        ["comp"] = new()
        {
            ["DramTypeCode"] = new[] { "dram_type", "dram_type_comp_extra" },
            ["DensityCode"] = new[] { "density_ddr4", "density_ddr5" },
            ["BitOrganizationCode"] = new[] { "bit" },
            ["BankCode"] = new[] { "bank_ddr4", "bank_ddr5" },
            ["InterfaceCode"] = new[] { "interface_ddr4", "interface_ddr5" },
            ["CompTypeCode"] = new[] { "comp_type", "manufacturing_comp_type" },
            ["PackageTypeCode"] = new[] { "package_type" },
            ["DieBrandCode"] = new[] { "die_brand" },
            ["TesterCode"] = new[] { "tester" },
            ["VendorCode"] = new[] { "vendor", "vendor_tm" },
            ["PurchaserCode"] = new[] { "purchaser", "purchaser_tm" },
            ["CompType2Code"] = new[] { "comp_type2" }
        },
        ["lpddr"] = new()
        {
            ["SourceCode"] = new[] { "lpddr_incoming_source" },
            ["DramTypeCode"] = new[] { "lpddr_dram_type" },
            ["DensityCode"] = new[] { "lpddr_density" },
            ["BitCode"] = new[] { "lpddr_bit" },
            ["DieDensityCode"] = new[] { "lpddr_die_density" },
            ["DieBitCode"] = new[] { "lpddr_bit" },
            ["BankCode"] = new[] { "lpddr_bank" },
            ["InterfaceCode"] = new[] { "lpddr_interface" },
            ["StackCode"] = new[] { "lpddr_stack" },
            ["DieTypeCode"] = new[] { "lpddr_die_type" },
            ["PackageTypeCode"] = new[] { "lpddr_package_type" },
            ["DieBrandCode"] = new[] { "lpddr_die_brand" },
            ["AssemblyCode"] = new[] { "lpddr_assembly" },
            ["TesterCode"] = new[] { "lpddr_tester" },
            ["SpeedCode"] = new[] { "lpddr_speed" },
            ["VendorCode"] = new[] { "lpddr_vendor" },
            ["PurchaserCode"] = new[] { "lpddr_purchaser" }
        }
    };

    public static int Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--version")
        {
            Console.WriteLine(typeof(Cli).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            return 0;
        }

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        var command = args[0];
        var codes = new List<string>();
        string? xlsxPath = null;
        string? specDirectory = null;
        string? icBrand = null;
        var kind = "module";
        var json = false;

        for (var i = 1; i < args.Length; i++)
        {
            string NextValue() => i + 1 < args.Length ? args[++i] : throw new CliInputException($"{args[i]} 뒤에 값이 필요합니다.");

            switch (args[i])
            {
                case "--json": json = true; break;
                case "--xlsx": xlsxPath = NextValue(); break;
                case "--specs": specDirectory = NextValue(); break;
                case "--kind": kind = NextValue().ToLowerInvariant(); break;
                case "--ic-brand": icBrand = NextValue().ToUpperInvariant(); break;
                case "--in": codes.AddRange(ReadCodeList(NextValue())); break;
                default:
                    if (args[i].StartsWith("--"))
                    {
                        throw new CliInputException($"알 수 없는 옵션입니다: {args[i]}");
                    }

                    codes.Add(args[i]);
                    break;
            }
        }

        codes = codes.Select(code => code.Trim().ToUpperInvariant()).Where(code => code.Length > 0).ToList();
        if (codes.Count == 0)
        {
            throw new CliInputException("파트코드가 없습니다.");
        }

        if (!MeaningKeys.ContainsKey(kind))
        {
            throw new CliInputException($"--kind는 module/comp/lpddr만 가능합니다: {kind}");
        }

        if (xlsxPath is not null && command is not ("generate" or "validate"))
        {
            throw new CliInputException("--xlsx는 generate/validate에서만 사용할 수 있습니다.");
        }

        if (xlsxPath is not null && File.Exists(xlsxPath))
        {
            throw new CliInputException($"출력 파일이 이미 있습니다(덮어쓰지 않음): {xlsxPath}");
        }

        if (specDirectory is not null && !Directory.Exists(specDirectory))
        {
            throw new CliInputException($"--specs 폴더가 없습니다: {specDirectory}");
        }

        var specProvider = new SpecProvider(Path.GetFullPath(specDirectory ?? Path.Combine(AppContext.BaseDirectory, "specs")));
        specProvider.Load();
        var engine = new Engine(specProvider, kind);

        var items = command switch
        {
            "generate" => codes.Select(engine.Generate).ToList(),
            "parse" => codes.Select(engine.Parse).ToList(),
            "validate" => codes.Select(engine.Validate).ToList(),
            "convert-legacy" => codes.Select(code => engine.ConvertLegacy(code, ParseIcBrand(icBrand))).ToList(),
            _ => throw new CliInputException($"알 수 없는 명령입니다: {command}")
        };

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { command, items }, JsonOptions));
        }
        else
        {
            foreach (var item in items)
            {
                WriteText(command, item);
            }
        }

        if (xlsxPath is not null)
        {
            // 같은 파트가 여러 입력에서 나와도 등록 양식에는 한 번만 넣는다. 결과 출력 뒤에 쓰므로 저장 실패(exit 2)에도 결과는 남는다.
            var rows = items.Where(item => item.Status == "ok")
                .SelectMany(item => item.GeneratedRows)
                .DistinctBy(row => row.PartCode)
                .ToList();
            using (var stream = new FileStream(xlsxPath, FileMode.CreateNew))
            {
                stream.Write(new RegistrationExcelExporter().Export(rows));
            }

            Console.Error.WriteLine($"xlsx 저장: {Path.GetFullPath(xlsxPath)} ({rows.Count}행)");
        }

        return items.All(item => item.Status == "ok") ? 0 : 1;
    }

    private static char ParseIcBrand(string? icBrand)
    {
        return icBrand switch
        {
            "S" => 'S',
            "G" => 'G',
            null => throw new CliInputException("convert-legacy는 --ic-brand S|G가 필요합니다. (S: 삼성 직구매 PKG / G: GIGA)"),
            _ => throw new CliInputException($"--ic-brand는 S 또는 G만 가능합니다: {icBrand}")
        };
    }

    private static IEnumerable<string> ReadCodeList(string path)
    {
        if (!File.Exists(path))
        {
            throw new CliInputException($"목록 파일이 없습니다: {path}");
        }

        // 한 줄에 하나, 빈 줄과 # 주석은 건너뛴다.
        return File.ReadAllLines(path).Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#'));
    }

    private static void WriteText(string command, CliItem item)
    {
        if (item.Status == "error")
        {
            Console.WriteLine($"[ERROR] {item.Input}: {string.Join(" / ", item.Messages)}");
            if (item.Fields is null)
            {
                return;
            }
        }

        switch (command)
        {
            case "generate":
                foreach (var row in item.Rows!)
                {
                    Console.WriteLine($"{row.Kind}\t{row.Code}\t{row.GeneralInfo}\t{row.Spec}");
                }

                break;
            case "parse":
                Console.WriteLine(item.Input);
                foreach (var field in item.Fields!)
                {
                    Console.WriteLine($"  {field.Name} = {field.Code}{(field.Meaning is null ? string.Empty : $" ({field.Meaning})")}");
                }

                break;
            case "validate":
                Console.WriteLine($"[OK] {item.Input}");
                break;
            case "convert-legacy":
                Console.WriteLine($"{item.Input}\t{item.Converted}\t{item.Status}\t{string.Join(" / ", item.Notes!)}\t{string.Join(" / ", item.Warnings!)}");
                break;
        }
    }

    private sealed class Engine
    {
        private readonly SpecProvider _specProvider;
        private readonly string _kind;
        private readonly ModuleService _module;
        private readonly IncomingCompService _incoming;
        private readonly LpddrService _lpddr;

        public Engine(SpecProvider specProvider, string kind)
        {
            _specProvider = specProvider;
            _kind = kind;
            var textService = new ProductTextService(specProvider);
            _module = new ModuleService(specProvider, textService);
            _incoming = new IncomingCompService(specProvider, textService);
            _lpddr = new LpddrService(specProvider);
        }

        public CliItem Generate(string code)
        {
            return Guard(code, () =>
            {
                var rows = GenerateRows(ParseRequest(code));
                return new CliItem { Input = code, Status = "ok", GeneratedRows = rows, Rows = rows.Select(CliRow.From).ToList() };
            });
        }

        public CliItem Parse(string code)
        {
            List<CliField>? fields = null;
            return Guard(code, () =>
            {
                var request = ParseRequest(code);
                fields = BuildFields(request);
                // 파서가 통과해도 생성 규칙(조합 제한 등)을 어기면 생성기에서 사유가 나온다.
                GenerateRows(request);
                return new CliItem { Input = code, Status = "ok", Fields = fields };
            }, () => fields);
        }

        // RULES_CURRENT "왕복 검증": 파서 -> 생성기 결과에 입력 코드가 그대로 나와야 한다.
        public CliItem Validate(string code)
        {
            return Guard(code, () =>
            {
                var rows = GenerateRows(ParseRequest(code));
                if (rows.All(row => row.PartCode != code))
                {
                    throw new InvalidOperationException($"왕복 불일치: 생성 결과 [{string.Join(", ", rows.Select(row => row.PartCode))}]에 입력 코드가 없습니다.");
                }

                return new CliItem { Input = code, Status = "ok", GeneratedRows = rows };
            });
        }

        public CliItem ConvertLegacy(string code, char icBrand)
        {
            LegacyPartConversion? result = null;
            return Guard(code, () =>
            {
                result = LegacyPartConverter.Convert(code, icBrand);
                var converted = result.ConvertedCode;

                // GUI 구형→신형 변환 탭과 같은 왕복 검증
                var regenerated = LegacyPartConverter.IsCompPart(converted)
                    ? _incoming.GeneratePreview(_incoming.ParseCompPart(Revision, converted)).Where(row => row.Kind == "Comp")
                    : _module.GeneratePreview(_module.ParseModuleFullPart(Revision, converted));
                if (!regenerated.Any(row => row.PartCode == converted))
                {
                    throw new InvalidOperationException("신형 Part를 다시 생성한 결과가 일치하지 않습니다.");
                }

                return new CliItem
                {
                    Input = code,
                    Status = result.Warnings.Count > 0 ? "check" : "ok",
                    Converted = converted,
                    Notes = result.Notes.ToList(),
                    Warnings = result.Warnings.ToList()
                };
            }, convertResult: () => result);
        }

        private object ParseRequest(string code)
        {
            return _kind switch
            {
                "comp" => _incoming.ParseCompPart(Revision, code),
                "lpddr" => _lpddr.ParsePart(code),
                _ => _module.ParseModuleFullPart(Revision, code)
            };
        }

        private IReadOnlyList<GeneratedPartRow> GenerateRows(object request)
        {
            return request switch
            {
                IncomingCompRequest comp => _incoming.GeneratePreview(comp),
                LpddrRequest lpddr => _lpddr.GeneratePreview(lpddr),
                ModuleRequest module => _module.GeneratePreview(module),
                _ => throw new InvalidOperationException("지원하지 않는 요청입니다.")
            };
        }

        private List<CliField> BuildFields(object request)
        {
            var keys = MeaningKeys[_kind];
            var options = _specProvider.SharedSpec.CodeOptions;
            return request.GetType().GetProperties()
                .Where(property => property.PropertyType == typeof(string))
                .Select(property => (property.Name, Value: property.GetValue(request)?.ToString() ?? string.Empty))
                .Where(field => field.Value.Length > 0)
                .Select(field =>
                {
                    var meaning = keys.TryGetValue(field.Name, out var optionKeys)
                        ? optionKeys
                            .SelectMany(key => options.TryGetValue(key, out var list) ? list : new List<string>())
                            .FirstOrDefault(option => option.Split(" - ", 2)[0].Trim().Equals(field.Value, StringComparison.OrdinalIgnoreCase))
                        : null;
                    return new CliField(field.Name, field.Value, meaning?.Split(" - ", 2) is [_, var text] ? text : null);
                })
                .ToList();
        }

        private static CliItem Guard(
            string code,
            Func<CliItem> action,
            Func<List<CliField>?>? fields = null,
            Func<LegacyPartConversion?>? convertResult = null)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                var result = convertResult?.Invoke();
                return new CliItem
                {
                    Input = code,
                    Status = "error",
                    Messages = new List<string> { ex.Message },
                    Fields = fields?.Invoke(),
                    Converted = result?.ConvertedCode,
                    Notes = result?.Notes.ToList(),
                    Warnings = result?.Warnings.ToList()
                };
            }
        }
    }
}

internal sealed class CliItem
{
    public string Input { get; init; } = string.Empty;
    public string Status { get; init; } = "ok";
    public List<CliRow>? Rows { get; init; }
    public List<CliField>? Fields { get; init; }
    public string? Converted { get; init; }
    public List<string>? Notes { get; init; }
    public List<string>? Warnings { get; init; }
    public List<string> Messages { get; init; } = new();

    [JsonIgnore]
    public IReadOnlyList<GeneratedPartRow> GeneratedRows { get; init; } = Array.Empty<GeneratedPartRow>();
}

internal sealed record CliRow(string Kind, string Code, string Name, string GeneralInfo, string Spec)
{
    // kind는 등록 양식 구분과 같은 표기(MDL, MDL BIN, 입고 등)
    public static CliRow From(GeneratedPartRow row) => new(RegistrationExcelExporter.FormatKind(row.Kind), row.PartCode, row.Name, row.GeneralInfo, row.Specification);
}

internal sealed record CliField(string Name, string Code, string? Meaning);
