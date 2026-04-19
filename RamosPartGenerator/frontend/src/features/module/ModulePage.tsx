import { useEffect, useMemo, useState } from "react";
import { FieldInput } from "../../components/FieldInput";
import { ResultTable } from "../../components/ResultTable";
import { api } from "../../services/api";
import type { GeneratedPartRow, LookupField, LookupPage, ModuleRequest } from "../../types";

const EMPTY_REQUEST: ModuleRequest = {
  revision: "30",
  moduleSourceCode: "",
  compFullPartCode: "",
  moduleFullPartCode: "",
  dramTypeCode: "",
  dimmTypeCode: "",
  moduleDensityCode: "",
  dieDensityCode: "",
  compositionCode: "",
  rankCode: "",
  generationCode: "",
  icBrandCode: "",
  moduleCompTypeCode: "",
  compTestCode: "",
  moduleSmtCode: "",
  moduleTestCode: "",
  speedCode: "",
  pcbCode: "",
  vendorCode: "",
  purchaserCode: "",
  a100SpecialCode: "",
  specialCode2Code: "",
  specialCode3Code: "",
  gradeCode: "",
  productBinCode: "",
  basePartCode: "",
  binPartCode: ""
};

type Props = {
  revision: string;
};

type DisplayField = {
  field: LookupField;
  options?: string[];
  disabled?: boolean;
  label?: string;
};

export function ModulePage({ revision }: Props) {
  const [lookups, setLookups] = useState<LookupPage | null>(null);
  const [request, setRequest] = useState<ModuleRequest>({ ...EMPTY_REQUEST, revision });
  const [compFullPart, setCompFullPart] = useState("");
  const [moduleFullPart, setModuleFullPart] = useState("");
  const [rows, setRows] = useState<GeneratedPartRow[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    api.getModuleLookups(revision)
      .then((page) => {
        setLookups(page);
        setRequest({ ...EMPTY_REQUEST, revision });
      })
      .catch((err: Error) => setError(err.message));
    setCompFullPart("");
    setModuleFullPart("");
    setRows([]);
    setError("");
  }, [revision]);

  const isRev27 = revision === "27";
  const sourceCode = extractCode(request.moduleSourceCode);
  const isThirdParty = sourceCode === "TM" || sourceCode === "BM";

  const grouped = useMemo(() => {
    const fields = lookups?.fields ?? [];
    return {
      base: fields.filter((field) => field.section === "base").map((field) => buildFieldView(field, request, revision)),
      structure: fields.filter((field) => field.section === "structure").map((field) => buildFieldView(field, request, revision)),
      output: fields.filter((field) => field.section === "output").map((field) => buildFieldView(field, request, revision))
    };
  }, [lookups, request, revision]);

  function updateField(key: string, value: string) {
    setRequest((prev) => ({ ...prev, [key]: value }));
  }

  async function handleParseComp() {
    try {
      const parsed = await api.parseModuleComp(revision, compFullPart.trim());
      setRequest((prev) => ({ ...prev, ...toDisplayRequest(parsed, lookups), revision }));
      setError("");
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function handleParseModule() {
    try {
      const parsed = await api.parseModuleFull(revision, moduleFullPart.trim());
      setRequest((prev) => ({ ...prev, ...toDisplayRequest(parsed, lookups), revision }));
      setError("");
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function handlePreview() {
    try {
      const previewRows = await api.previewModule(toApiRequest(request, compFullPart, moduleFullPart));
      setRows(previewRows);
      setError("");
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function handleExport() {
    try {
      if (rows.length === 0) {
        setError("Generate results first.");
        return;
      }

      const blob = await api.exportRegistration(rows);
      const url = window.URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `module_${revision}.xlsx`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      window.URL.revokeObjectURL(url);
      setError("");
    } catch (err) {
      setError((err as Error).message);
    }
  }

  function handleReset() {
    setRequest({ ...EMPTY_REQUEST, revision });
    setCompFullPart("");
    setModuleFullPart("");
    setRows([]);
    setError("");
  }

  return (
    <div className="page">
      <section className="action-card stacked">
        <label className="field-row compact">
          <span className="field-label">Comp Full Part</span>
          <input
            className="field-input"
            placeholder="TCRAH086VB-NBGWGH"
            value={compFullPart}
            onChange={(e) => setCompFullPart(e.target.value.toUpperCase())}
          />
          <button type="button" className="secondary-button" onClick={handleParseComp}>Parse</button>
        </label>
        <label className="field-row compact">
          <span className="field-label">Module Full Part</span>
          <input
            className="field-input"
            placeholder="TMRDAG58A1B-GNWRRWM7GH"
            value={moduleFullPart}
            onChange={(e) => setModuleFullPart(e.target.value.toUpperCase())}
          />
          <button type="button" className="secondary-button" onClick={handleParseModule}>Parse</button>
        </label>
        <div className="hint-row">
          <span>Use Comp Full Part to prefill module defaults. Module Full Part parsing overrides visible fields for review.</span>
        </div>
        <div className="action-buttons">
          <button type="button" className="primary-button" onClick={handlePreview}>Generate</button>
          <button type="button" className="secondary-button" onClick={handleExport}>Export Excel</button>
          <button type="button" className="ghost-button" onClick={handleReset}>Reset</button>
        </div>
      </section>

      {error && <div className="error-box">{error}</div>}

      <div className="rule-strip">
        <span className="rule-pill">{sourceCode ? `Source: ${sourceCode}` : "Parse Comp Part or Module Part to derive source"}</span>
        <span className="rule-pill">{isRev27 ? "Rev 27: combined I.C Brand + Comp Type, Vendor (For TP) only" : "Rev 30: split I.C Brand / Comp Type, Vendor + Purchaser"}</span>
        <span className="rule-pill">{isThirdParty ? "Third-party module" : sourceCode ? "Internal module" : "Source not determined yet"}</span>
      </div>

      <div className="grid-3">
        <FieldSection title="Module Base" fields={grouped.base} values={request} onChange={updateField} />
        <FieldSection title="Structure" fields={grouped.structure} values={request} onChange={updateField} />
        <FieldSection title="Output" fields={grouped.output} values={request} onChange={updateField} />
      </div>

      <ResultTable rows={rows} />
    </div>
  );
}

function FieldSection({
  title,
  fields,
  values,
  onChange
}: {
  title: string;
  fields: DisplayField[];
  values: Record<string, string>;
  onChange: (key: string, value: string) => void;
}) {
  return (
    <section className="section-card">
      <div className="section-title">{title}</div>
      <div className="field-list">
        {fields.map(({ field, options, disabled, label }) => (
          <FieldInput
            key={field.key}
            field={field}
            label={label}
            options={options}
            disabled={disabled}
            value={values[field.key] ?? ""}
            onChange={onChange}
          />
        ))}
      </div>
    </section>
  );
}

function buildFieldView(field: LookupField, request: ModuleRequest, revision: string): DisplayField {
  const sourceCode = extractCode(request.moduleSourceCode);
  const isThirdParty = sourceCode === "TM" || sourceCode === "BM";
  const isRev27 = revision === "27";

  if (field.key === "vendorCode") {
    return {
      field,
      disabled: isRev27 && !isThirdParty,
      label: isRev27 ? "Vendor (For TP)" : field.label
    };
  }

  if (field.key === "purchaserCode") {
    return {
      field,
      disabled: !isThirdParty
    };
  }

  if (field.key === "a100SpecialCode") {
    return {
      field,
      disabled: !isThirdParty || extractCode(request.vendorCode) !== "A" || extractCode(request.purchaserCode) !== "A"
    };
  }

  if (field.key === "moduleCompTypeCode") {
    return {
      field,
      label: isRev27 ? "I.C Brand + Comp Type" : field.label
    };
  }

  return { field };
}

function extractCode(rawValue: string): string {
  const trimmed = rawValue.trim();
  if (!trimmed || trimmed === "(None)") {
    return "";
  }

  const separatorIndex = trimmed.indexOf(" - ");
  if (separatorIndex > -1) {
    return trimmed.slice(0, separatorIndex).trim();
  }

  return trimmed;
}

function resolveDisplayValue(code: string, options: string[]): string {
  if (!code) {
    return "";
  }

  const matched = options.find((option) => extractCode(option) === code);
  return matched ?? code;
}

function toDisplayRequest(parsed: ModuleRequest, lookups: LookupPage | null): ModuleRequest {
  const resolve = (key: keyof ModuleRequest, value: string) => {
    const field = lookups?.fields.find((item) => item.key === key);
    return resolveDisplayValue(value, field?.options ?? []);
  };

  return {
    revision: parsed.revision,
    moduleSourceCode: parsed.moduleSourceCode,
    compFullPartCode: parsed.compFullPartCode,
    moduleFullPartCode: parsed.moduleFullPartCode,
    dramTypeCode: resolve("dramTypeCode", parsed.dramTypeCode),
    dimmTypeCode: resolve("dimmTypeCode", parsed.dimmTypeCode),
    moduleDensityCode: resolve("moduleDensityCode", parsed.moduleDensityCode),
    dieDensityCode: resolve("dieDensityCode", parsed.dieDensityCode),
    compositionCode: resolve("compositionCode", parsed.compositionCode),
    rankCode: resolve("rankCode", parsed.rankCode),
    generationCode: parsed.generationCode,
    icBrandCode: resolve("icBrandCode", parsed.icBrandCode),
    moduleCompTypeCode: resolve("moduleCompTypeCode", parsed.moduleCompTypeCode),
    compTestCode: parsed.compTestCode,
    moduleSmtCode: parsed.moduleSmtCode,
    moduleTestCode: parsed.moduleTestCode,
    speedCode: parsed.speedCode,
    pcbCode: parsed.pcbCode,
    vendorCode: resolve("vendorCode", parsed.vendorCode),
    purchaserCode: resolve("purchaserCode", parsed.purchaserCode),
    a100SpecialCode: resolve("a100SpecialCode", parsed.a100SpecialCode),
    specialCode2Code: resolve("specialCode2Code", parsed.specialCode2Code),
    specialCode3Code: resolve("specialCode3Code", parsed.specialCode3Code),
    gradeCode: resolve("gradeCode", parsed.gradeCode),
    productBinCode: resolve("productBinCode", parsed.productBinCode),
    basePartCode: parsed.basePartCode,
    binPartCode: parsed.binPartCode
  };
}

function toApiRequest(request: ModuleRequest, compFullPart: string, moduleFullPart: string): ModuleRequest {
  return {
    revision: request.revision,
    moduleSourceCode: extractCode(request.moduleSourceCode),
    compFullPartCode: compFullPart.trim().toUpperCase(),
    moduleFullPartCode: moduleFullPart.trim().toUpperCase(),
    dramTypeCode: extractCode(request.dramTypeCode),
    dimmTypeCode: extractCode(request.dimmTypeCode),
    moduleDensityCode: extractCode(request.moduleDensityCode),
    dieDensityCode: extractCode(request.dieDensityCode),
    compositionCode: extractCode(request.compositionCode),
    rankCode: extractCode(request.rankCode),
    generationCode: extractCode(request.generationCode),
    icBrandCode: extractCode(request.icBrandCode),
    moduleCompTypeCode: extractCode(request.moduleCompTypeCode),
    compTestCode: extractCode(request.compTestCode),
    moduleSmtCode: extractCode(request.moduleSmtCode),
    moduleTestCode: extractCode(request.moduleTestCode),
    speedCode: extractCode(request.speedCode),
    pcbCode: extractCode(request.pcbCode),
    vendorCode: extractCode(request.vendorCode),
    purchaserCode: extractCode(request.purchaserCode),
    a100SpecialCode: extractCode(request.a100SpecialCode),
    specialCode2Code: extractCode(request.specialCode2Code),
    specialCode3Code: extractCode(request.specialCode3Code),
    gradeCode: extractCode(request.gradeCode),
    productBinCode: extractCode(request.productBinCode),
    basePartCode: request.basePartCode.trim().toUpperCase(),
    binPartCode: request.binPartCode.trim().toUpperCase()
  };
}

