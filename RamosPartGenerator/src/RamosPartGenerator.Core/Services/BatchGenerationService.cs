using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Core.Services;

public sealed class BatchGenerationService
{
    private readonly ModuleService _moduleService;
    private readonly IncomingCompService _incomingCompService;

    public BatchGenerationService(
        ModuleService moduleService,
        IncomingCompService incomingCompService)
    {
        _moduleService = moduleService;
        _incomingCompService = incomingCompService;
    }

    public BatchGenerationResult GenerateFromModuleParts(
        IEnumerable<string?> partCodes,
        MdlBatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(partCodes);
        ArgumentNullException.ThrowIfNull(options);

        var items = new List<BatchItemResult>();
        var allRows = new List<GeneratedPartRow>();
        var inputCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outputCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateCount = 0;

        foreach (var rawPartCode in partCodes)
        {
            var inputPartCode = NormalizeInput(rawPartCode);
            if (string.IsNullOrEmpty(inputPartCode))
            {
                continue;
            }

            if (!inputCodes.Add(inputPartCode))
            {
                duplicateCount++;
                continue;
            }

            var itemRows = new List<GeneratedPartRow>();
            var messages = new List<string>();
            var detectedInputKind = ModuleBatchInputKind.Normal;

            try
            {
                var parsed = ParseModuleInput(inputPartCode, out detectedInputKind);
                var baseRequest = CreateBaseRequest(parsed);

                GenerateBaseRows(baseRequest, options, itemRows, messages);
                GenerateModuleWorkRows(baseRequest, options, itemRows, messages);
                GenerateCompRows(baseRequest, options, itemRows, messages);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                messages.Add(exception.Message);
            }

            var uniqueItemRows = new List<GeneratedPartRow>();
            foreach (var row in itemRows)
            {
                if (outputCodes.Add(row.PartCode))
                {
                    uniqueItemRows.Add(row);
                    allRows.Add(row);
                }
                else
                {
                    duplicateCount++;
                }
            }

            if (uniqueItemRows.Count == 0 && messages.Count == 0)
            {
                messages.Add("생성할 항목을 선택해 주세요.");
            }

            var status = uniqueItemRows.Count switch
            {
                0 => BatchItemStatus.Failed,
                _ when messages.Count > 0 => BatchItemStatus.PartialSuccess,
                _ => BatchItemStatus.Success
            };

            items.Add(new BatchItemResult(
                inputPartCode,
                detectedInputKind,
                status,
                uniqueItemRows,
                messages));
        }

        return new BatchGenerationResult(items, allRows, duplicateCount);
    }

    public BatchGenerationResult GenerateFromCompParts(
        IEnumerable<string?> partCodes,
        CompBatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(partCodes);
        ArgumentNullException.ThrowIfNull(options);

        var items = new List<BatchItemResult>();
        var allRows = new List<GeneratedPartRow>();
        var inputCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outputCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateCount = 0;

        foreach (var rawPartCode in partCodes)
        {
            var inputPartCode = NormalizeInput(rawPartCode);
            if (string.IsNullOrEmpty(inputPartCode))
            {
                continue;
            }

            if (!inputCodes.Add(inputPartCode))
            {
                duplicateCount++;
                continue;
            }

            var itemRows = new List<GeneratedPartRow>();
            var messages = new List<string>();
            var detectedInputKind = ModuleBatchInputKind.Normal;

            try
            {
                var parsed = _incomingCompService.ParseCompPart("30", inputPartCode);
                detectedInputKind = parsed.CompType2Code.Equals("B", StringComparison.OrdinalIgnoreCase)
                    ? ModuleBatchInputKind.Reball
                    : ModuleBatchInputKind.Normal;
                AddRows(_incomingCompService.GeneratePreview(parsed), itemRows);

                if (options.IncludeCompMdl)
                {
                    var speedCodes = options.SpeedCodes
                        .Select(speedCode => (speedCode ?? string.Empty).Trim().ToUpperInvariant())
                        .Where(speedCode => !string.IsNullOrEmpty(speedCode))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    if (speedCodes.Length == 0)
                    {
                        TryGenerate(
                            "Comp_MDL",
                            () => _moduleService.GeneratePreview(CreateCompMdlRequest(parsed, string.Empty)),
                            generatedRows => AddRows(generatedRows, itemRows),
                            messages);
                    }
                    else
                    {
                        foreach (var speedCode in speedCodes)
                        {
                            TryGenerate(
                                $"Comp_MDL ({speedCode})",
                                () => _moduleService.GeneratePreview(CreateCompMdlRequest(parsed, speedCode)),
                                generatedRows => AddRows(generatedRows, itemRows),
                                messages);
                        }
                    }
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                messages.Add(exception.Message);
            }

            var uniqueItemRows = new List<GeneratedPartRow>();
            foreach (var row in itemRows)
            {
                if (outputCodes.Add(row.PartCode))
                {
                    uniqueItemRows.Add(row);
                    allRows.Add(row);
                }
                else
                {
                    duplicateCount++;
                }
            }

            var status = uniqueItemRows.Count switch
            {
                0 => BatchItemStatus.Failed,
                _ when messages.Count > 0 => BatchItemStatus.PartialSuccess,
                _ => BatchItemStatus.Success
            };

            items.Add(new BatchItemResult(
                inputPartCode,
                detectedInputKind,
                status,
                uniqueItemRows,
                messages));
        }

        return new BatchGenerationResult(items, allRows, duplicateCount);
    }

    private ModuleRequest ParseModuleInput(
        string inputPartCode,
        out ModuleBatchInputKind detectedInputKind)
    {
        var parsePartCode = inputPartCode;
        detectedInputKind = DetectInputKind(inputPartCode);

        if (detectedInputKind is ModuleBatchInputKind.SecondRepairDummy or ModuleBatchInputKind.ReballRepairDummy)
        {
            parsePartCode = RemoveDummySuffix(inputPartCode);
        }

        var parsed = _moduleService.ParseModuleFullPart("30", parsePartCode);
        if (detectedInputKind == ModuleBatchInputKind.Normal)
        {
            detectedInputKind = parsed.IsFinishedProductRetest
                ? ModuleBatchInputKind.FinishedProductRetest
                : parsed.SpecialCode2Code switch
                {
                    "R" => ModuleBatchInputKind.FirstRepair,
                    "S" => ModuleBatchInputKind.SecondRepair,
                    "B" => ModuleBatchInputKind.Reball,
                    "C" => ModuleBatchInputKind.ReballRepair,
                    _ => ModuleBatchInputKind.Normal
                };
        }

        return parsed;
    }

    private void GenerateBaseRows(
        ModuleRequest baseRequest,
        MdlBatchOptions options,
        ICollection<GeneratedPartRow> rows,
        ICollection<string> messages)
    {
        if (!options.IncludeBasePid && !options.IncludeBaseMfgId)
        {
            return;
        }

        TryGenerate("기본 Part", () => _moduleService.GeneratePreview(CloneRequest(baseRequest)), generatedRows =>
        {
            if (options.IncludeBasePid)
            {
                AddKind(generatedRows, rows, "Module");
            }

            if (options.IncludeBaseMfgId)
            {
                AddKind(generatedRows, rows, "Module BIN");
            }
        }, messages);
    }

    private void GenerateModuleWorkRows(
        ModuleRequest baseRequest,
        MdlBatchOptions options,
        ICollection<GeneratedPartRow> rows,
        ICollection<string> messages)
    {
        if (options.IncludeReball)
        {
            GenerateSpecialCode2Rows("Reball", baseRequest, "B", false, rows, messages);
        }

        if (options.IncludeFirstRepair)
        {
            GenerateSpecialCode2Rows("1차 Repair", baseRequest, "R", true, rows, messages);
        }

        if (options.IncludeSecondRepair)
        {
            GenerateSpecialCode2Rows("2차 Repair", baseRequest, "S", true, rows, messages);
        }

        if (options.IncludeReballRepair)
        {
            GenerateSpecialCode2Rows("Reball Repair", baseRequest, "C", true, rows, messages);
        }

        if (options.IncludeFinishedProductRetest)
        {
            var request = CloneRequest(baseRequest);
            request.IsFinishedProductRetest = true;
            TryGenerate(
                "완제품 Retest",
                () => _moduleService.GeneratePreview(request),
                generatedRows => AddRows(generatedRows, rows),
                messages);
        }
    }

    private void GenerateCompRows(
        ModuleRequest baseRequest,
        MdlBatchOptions options,
        ICollection<GeneratedPartRow> rows,
        ICollection<string> messages)
    {
        if (options.IncludeOriginalCompRelated)
        {
            TryGenerate(
                "원본 Comp 관련",
                () => _incomingCompService.GeneratePreview(CreateIncomingCompRequest(baseRequest, false)),
                generatedRows => AddRows(generatedRows, rows),
                messages);
        }

        if (options.IncludeReballCompRelated)
        {
            TryGenerate(
                "Reball Comp 관련",
                () => _incomingCompService.GeneratePreview(CreateIncomingCompRequest(baseRequest, true)),
                generatedRows => AddRows(generatedRows, rows),
                messages);
        }
    }

    private void GenerateSpecialCode2Rows(
        string label,
        ModuleRequest baseRequest,
        string specialCode2Code,
        bool dummyFirst,
        ICollection<GeneratedPartRow> rows,
        ICollection<string> messages)
    {
        var request = CloneRequest(baseRequest);
        request.SpecialCode2Code = specialCode2Code;

        TryGenerate(label, () => _moduleService.GeneratePreview(request), generatedRows =>
        {
            if (dummyFirst)
            {
                AddKind(generatedRows, rows, "Module Dummy");
            }

            AddKind(generatedRows, rows, "Module");
            AddKind(generatedRows, rows, "Module BIN");
        }, messages);
    }

    private static void TryGenerate(
        string label,
        Func<IReadOnlyList<GeneratedPartRow>> generate,
        Action<IReadOnlyList<GeneratedPartRow>> addRows,
        ICollection<string> messages)
    {
        try
        {
            addRows(generate());
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            messages.Add($"{label}: {exception.Message}");
        }
    }

    private static IncomingCompRequest CreateIncomingCompRequest(ModuleRequest request, bool isReball)
    {
        var dramTypeCode = request.DramTypeCode switch
        {
            "4" => "A",
            "R" => "R",
            _ => throw new InvalidOperationException($"Comp로 변환할 수 없는 Module DRAM Type입니다: {request.DramTypeCode}")
        };

        return new IncomingCompRequest
        {
            Revision = request.Revision,
            SourceCode = MapModuleToIncomingSource(request.ModuleSourceCode),
            DramTypeCode = dramTypeCode,
            DensityCode = MapModuleDieDensityToCompDensity(dramTypeCode, request.DieDensityCode),
            BitOrganizationCode = MapModuleCompositionToCompBit(request.CompositionCode),
            BankCode = dramTypeCode == "A" ? "5" : "6",
            InterfaceCode = dramTypeCode == "A" ? "W" : "V",
            RevisionCode = request.GenerationCode,
            CompTypeCode = request.ModuleCompTypeCode,
            DieBrandCode = request.IcBrandCode,
            VendorCode = request.VendorCode,
            PurchaserCode = request.PurchaserCode,
            CompType2Code = isReball ? "B" : string.Empty,
            PackageTypeCode = "B",
            TesterCode = request.CompTestCode
        };
    }

    private static ModuleRequest CreateCompMdlRequest(IncomingCompRequest request, string speedCode)
    {
        if (string.IsNullOrWhiteSpace(speedCode))
        {
            throw new InvalidOperationException("Comp_MDL Speed를 선택해 주세요.");
        }

        if (request.CompType2Code is not ("0" or "B"))
        {
            throw new InvalidOperationException(
                $"Comp_MDL로 변환할 수 없는 Comp Type 2입니다: {request.CompType2Code}");
        }

        var dieDensityCode = MapCompDensityToModuleDieDensity(request.DensityCode);
        var moduleDensityCode = ModuleService.GetCompSaleModuleDensityCode(dieDensityCode);
        if (string.IsNullOrEmpty(moduleDensityCode))
        {
            throw new InvalidOperationException(
                $"Comp_MDL Module Density를 Base Die Density {dieDensityCode}에서 결정할 수 없습니다.");
        }

        return new ModuleRequest
        {
            Revision = request.Revision,
            ModuleSourceCode = MapIncomingToModuleSource(request.SourceCode),
            DramTypeCode = request.DramTypeCode switch
            {
                "A" => "4",
                "R" => "R",
                _ => throw new InvalidOperationException(
                    $"Comp_MDL로 변환할 수 없는 DRAM Type입니다: {request.DramTypeCode}")
            },
            DimmTypeCode = "C",
            ModuleDensityCode = moduleDensityCode,
            DieDensityCode = dieDensityCode,
            CompositionCode = MapCompBitToModuleComposition(request.BitOrganizationCode),
            GenerationCode = request.RevisionCode,
            IcBrandCode = request.DieBrandCode,
            ModuleCompTypeCode = request.CompTypeCode,
            CompTestCode = request.TesterCode,
            SpeedCode = speedCode,
            VendorCode = request.VendorCode,
            PurchaserCode = request.PurchaserCode,
            SpecialCode2Code = request.CompType2Code == "B" ? "B" : string.Empty
        };
    }

    private static string MapIncomingToModuleSource(string incomingSourceCode)
    {
        return incomingSourceCode switch
        {
            "K" => "RM",
            "T" => "TM",
            "C" => "CM",
            "B" => "BM",
            "X" => "XM",
            "Z" => "ZM",
            _ => throw new InvalidOperationException(
                $"Comp_MDL Source로 변환할 수 없는 Incoming Source입니다: {incomingSourceCode}")
        };
    }

    private static string MapCompDensityToModuleDieDensity(string densityCode)
    {
        return densityCode switch
        {
            "4G" => "4",
            "8G" => "8",
            "AG" or "AH" => "A",
            "HE" => "H",
            "BH" => "B",
            _ => throw new InvalidOperationException(
                $"Comp_MDL Base Die Density로 변환할 수 없는 Comp Density입니다: {densityCode}")
        };
    }

    private static string MapCompBitToModuleComposition(string bitOrganizationCode)
    {
        return bitOrganizationCode switch
        {
            "04" => "4",
            "08" => "8",
            "16" => "6",
            "48" => "9",
            _ => throw new InvalidOperationException(
                $"Comp_MDL Composition으로 변환할 수 없는 Comp Bit입니다: {bitOrganizationCode}")
        };
    }

    private static string MapModuleToIncomingSource(string moduleSourceCode)
    {
        return moduleSourceCode switch
        {
            "RM" => "K",
            "TM" => "T",
            "CM" => "C",
            "BM" => "B",
            "XM" => "X",
            "ZM" => "Z",
            _ => throw new InvalidOperationException($"Comp Source로 변환할 수 없는 Module Source입니다: {moduleSourceCode}")
        };
    }

    private static string MapModuleDieDensityToCompDensity(string dramTypeCode, string dieDensityCode)
    {
        return (dramTypeCode, dieDensityCode) switch
        {
            ("A", "4") => "4G",
            ("A", "8") => "8G",
            ("A", "A") => "AG",
            ("R", "A") => "AH",
            ("R", "H") => "HE",
            ("R", "B") => "BH",
            _ => throw new InvalidOperationException(
                $"Comp Density로 변환할 수 없는 Base Die Density입니다: {dieDensityCode}")
        };
    }

    private static string MapModuleCompositionToCompBit(string compositionCode)
    {
        return compositionCode switch
        {
            "4" => "04",
            "8" => "08",
            "6" => "16",
            "9" => "48",
            _ => throw new InvalidOperationException(
                $"Comp Bit로 변환할 수 없는 Module Composition입니다: {compositionCode}")
        };
    }

    private static ModuleRequest CreateBaseRequest(ModuleRequest parsed)
    {
        var request = CloneRequest(parsed);
        request.ModuleFullPartCode = string.Empty;
        request.BasePartCode = string.Empty;
        request.BinPartCode = string.Empty;
        request.SpecialCode2Code = string.Empty;
        if (request.SpecialCode3Code == "Y")
        {
            request.SpecialCode3Code = string.Empty;
        }
        request.IsFinishedProductRetest = false;
        return request;
    }

    private static ModuleRequest CloneRequest(ModuleRequest request)
    {
        return new ModuleRequest
        {
            Revision = request.Revision,
            ModuleSourceCode = request.ModuleSourceCode,
            CompFullPartCode = request.CompFullPartCode,
            ModuleFullPartCode = request.ModuleFullPartCode,
            DramTypeCode = request.DramTypeCode,
            DimmTypeCode = request.DimmTypeCode,
            ModuleDensityCode = request.ModuleDensityCode,
            BankVddCode = request.BankVddCode,
            DieDensityCode = request.DieDensityCode,
            CompositionCode = request.CompositionCode,
            RankCode = request.RankCode,
            GenerationCode = request.GenerationCode,
            IcBrandCode = request.IcBrandCode,
            ModuleCompTypeCode = request.ModuleCompTypeCode,
            CompTestCode = request.CompTestCode,
            ModuleSmtCode = request.ModuleSmtCode,
            ModuleTestCode = request.ModuleTestCode,
            SpeedCode = request.SpeedCode,
            PcbCode = request.PcbCode,
            VendorCode = request.VendorCode,
            PurchaserCode = request.PurchaserCode,
            A100SpecialCode = request.A100SpecialCode,
            SpecialCode2Code = request.SpecialCode2Code,
            SpecialCode3Code = request.SpecialCode3Code,
            GradeCode = request.GradeCode,
            ProductBinCode = request.ProductBinCode,
            IsFinishedProductRetest = request.IsFinishedProductRetest,
            BasePartCode = request.BasePartCode,
            BinPartCode = request.BinPartCode
        };
    }

    private static ModuleBatchInputKind DetectInputKind(string inputPartCode)
    {
        var parts = inputPartCode.Split('-');
        if (parts.Length is < 2 or > 3)
        {
            return ModuleBatchInputKind.Normal;
        }

        var tailPart = parts[1];
        if (tailPart.EndsWith("R0", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleBatchInputKind.SecondRepairDummy;
        }

        if (tailPart.EndsWith("B0", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleBatchInputKind.ReballRepairDummy;
        }

        if (tailPart.EndsWith("00", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleBatchInputKind.SharedDummy;
        }

        if (tailPart.EndsWith("0Y", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleBatchInputKind.FinishedProductRetest;
        }

        return ModuleBatchInputKind.Normal;
    }

    private static string RemoveDummySuffix(string partCode)
    {
        var parts = partCode.Split('-');
        parts[1] = parts[1][..^2];
        return string.Join('-', parts);
    }

    private static string NormalizeInput(string? partCode)
    {
        return (partCode ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", string.Empty);
    }

    private static void AddKind(
        IEnumerable<GeneratedPartRow> source,
        ICollection<GeneratedPartRow> target,
        string kind)
    {
        foreach (var row in source.Where(row => row.Kind == kind))
        {
            target.Add(row);
        }
    }

    private static void AddRows(
        IEnumerable<GeneratedPartRow> source,
        ICollection<GeneratedPartRow> target)
    {
        foreach (var row in source)
        {
            target.Add(row);
        }
    }
}
