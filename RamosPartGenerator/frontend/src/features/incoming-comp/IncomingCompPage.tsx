import { useEffect, useMemo, useState } from "react";
import { FieldInput } from "../../components/FieldInput";
import { ResultTable } from "../../components/ResultTable";
import { api } from "../../services/api";
import type { GeneratedPartRow, IncomingCompRequest, LookupField, LookupPage } from "../../types";

const EMPTY_REQUEST: IncomingCompRequest = {
  revision: "30",
  sourceCode: "",
  dramTypeCode: "",
  densityCode: "",
  bitOrganizationCode: "",
  bankCode: "",
  interfaceCode: "",
  revisionCode: "",
  compTypeCode: "",
  dieBrandCode: "",
  vendorCode: "",
  purchaserCode: "",
  compType2Code: "",
  packageTypeCode: "",
  testerCode: ""
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

export function IncomingCompPage({ revision }: Props) {
  const [lookups, setLookups] = useState<LookupPage | null>(null);
  const [request, setRequest] = useState<IncomingCompRequest>({ ...EMPTY_REQUEST, revision });
  const [compFullPart, setCompFullPart] = useState("");
  const [rows, setRows] = useState<GeneratedPartRow[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    api.getIncomingLookups(revision)
      .then((page) => {
        setLookups(page);
        setRequest({ ...EMPTY_REQUEST, revision });
      })
      .catch((err: Error) => setError(err.message));
    setCompFullPart("");
    setRows([]);
    setError("");
  }, [revision]);

  const isRev27 = revision === "27";
  const dramTypeCode = extractCode(request.dramTypeCode);
  const sourceCode = extractCode(request.sourceCode);
  const isThirdParty = sourceCode === "T" || sourceCode === "B";

  const grouped = useMemo(() => {
    const fields = lookups?.fields ?? [];
    return {
      common: fields.filter((field) => field.section === "common").map((field) => buildFieldView(field, request, revision)),
      comp: fields.filter((field) => field.section === "comp").map((field) => buildFieldView(field, request, revision)),
      extra: fields.filter((field) => field.section === "extra").map((field) => buildFieldView(field, request, revision))
    };
  }, [lookups, request, revision]);

  function updateField(key: string, value: string) {
    setRequest((prev) => {
      const next = { ...prev, [key]: value };
      if (key === "dramTypeCode") {
        const code = extractCode(value);
        if (code === "A") {
          next.bankCode = resolveDisplayValue("5", getFieldOptions("bankCode"));
          next.interfaceCode = resolveDisplayValue("W", getFieldOptions("interfaceCode"));
          if (!["4G", "8G", "AG"].includes(extractCode(next.densityCode))) {
            next.densityCode = "";
          }
        } else if (code === "R") {
          next.bankCode = resolveDisplayValue("6", getFieldOptions("bankCode"));
          next.interfaceCode = resolveDisplayValue("V", getFieldOptions("interfaceCode"));
          if (!["AH", "HE", "BH"].includes(extractCode(next.densityCode))) {
            next.densityCode = "";
          }
        } else {
          next.bankCode = "";
          next.interfaceCode = "";
        }
      }

      if (key === "sourceCode") {
        const code = extractCode(value);
        const tp = code === "T" || code === "B";
        if (isRev27) {
          if (!tp) {
            next.vendorCode = "";
          }
          next.purchaserCode = "";
        } else if (!tp) {
          next.purchaserCode = "";
        }
      }

      return next;
    });
  }

  function getFieldOptions(key: string): string[] {
    const field = lookups?.fields.find((item) => item.key === key);
    return field?.options ?? [];
  }

  async function handleParse() {
    try {
      const parsed = await api.parseIncomingComp(revision, compFullPart.trim());
      setRequest(toDisplayRequest(parsed, lookups));
      setError("");
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function handlePreview() {
    try {
      const previewRows = await api.previewIncoming(toApiRequest(request));
      setRows(previewRows);
      setError("");
    } catch (err) {
      setError((err as Error).message);
    }
  }

  function handleReset() {
    setRequest({ ...EMPTY_REQUEST, revision });
    setCompFullPart("");
    setRows([]);
    setError("");
  }

  return (
    <div className="page">
      <section className="action-card">
        <div className="action-input">
          <label className="field-row compact">
            <span className="field-label">Comp Full Part</span>
            <input
              className="field-input"
              placeholder="RCRAH086VA-PBGWG"
              value={compFullPart}
              onChange={(e) => setCompFullPart(e.target.value.toUpperCase())}
            />
          </label>
          <div className="hint-row">
            <span>Typing and dropdown selection can be mixed. Preview sends normalized code values to the API.</span>
          </div>
        </div>
        <div className="action-buttons">
          <button type="button" className="secondary-button" onClick={handleParse}>Parse</button>
          <button type="button" className="primary-button" onClick={handlePreview}>Generate</button>
          <button type="button" className="ghost-button" onClick={handleReset}>Reset</button>
        </div>
      </section>

      {error && <div className="error-box">{error}</div>}

      <div className="rule-strip">
        <span className="rule-pill">{dramTypeCode === "A" ? "DDR4 fixed: 16Bank / POD 1.2V" : dramTypeCode === "R" ? "DDR5 fixed: 32Bank / POD 1.1V" : "Select DRAM Type to apply Bank / Interface defaults"}</span>
        <span className="rule-pill">{isRev27 ? "Rev 27: Vendor(For TP) only" : "Rev 30: Vendor + Purchaser"}</span>
        <span className="rule-pill">{isThirdParty ? "Third-party source selected" : "Internal source selected"}</span>
      </div>

      <div className="grid-3">
        <FieldSection title="Common" fields={grouped.common} values={request} onChange={updateField} />
        <FieldSection title="Comp Fields" fields={grouped.comp} values={request} onChange={updateField} />
        <FieldSection title="Extra" fields={grouped.extra} values={request} onChange={updateField} />
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

function buildFieldView(field: LookupField, request: IncomingCompRequest, revision: string): DisplayField {
  const dramTypeCode = extractCode(request.dramTypeCode);
  const sourceCode = extractCode(request.sourceCode);
  const isThirdParty = sourceCode === "T" || sourceCode === "B";
  const isRev27 = revision === "27";

  if (field.key === "densityCode") {
    return {
      field,
      options: dramTypeCode === "A"
        ? field.options.filter((option) => ["4G", "8G", "AG"].includes(extractCode(option)))
        : dramTypeCode === "R"
          ? field.options.filter((option) => ["AH", "HE", "BH"].includes(extractCode(option)))
          : field.options
    };
  }

  if (field.key === "bankCode") {
    return {
      field,
      options: dramTypeCode === "A"
        ? field.options.filter((option) => extractCode(option) === "5")
        : dramTypeCode === "R"
          ? field.options.filter((option) => extractCode(option) === "6")
          : field.options,
      disabled: dramTypeCode === "A" || dramTypeCode === "R"
    };
  }

  if (field.key === "interfaceCode") {
    return {
      field,
      options: dramTypeCode === "A"
        ? field.options.filter((option) => extractCode(option) === "W")
        : dramTypeCode === "R"
          ? field.options.filter((option) => extractCode(option) === "V")
          : field.options,
      disabled: dramTypeCode === "A" || dramTypeCode === "R"
    };
  }

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

  return { field };
}

function extractCode(rawValue: string): string {
  const trimmed = rawValue.trim();
  if (!trimmed || trimmed === "(None)" || trimmed === "(¾øÀ½)") {
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

function toDisplayRequest(parsed: IncomingCompRequest, lookups: LookupPage | null): IncomingCompRequest {
  const resolve = (key: keyof IncomingCompRequest, value: string) => {
    const field = lookups?.fields.find((item) => item.key === key);
    return resolveDisplayValue(value, field?.options ?? []);
  };

  return {
    revision: parsed.revision,
    sourceCode: resolve("sourceCode", parsed.sourceCode),
    dramTypeCode: resolve("dramTypeCode", parsed.dramTypeCode),
    densityCode: resolve("densityCode", parsed.densityCode),
    bitOrganizationCode: resolve("bitOrganizationCode", parsed.bitOrganizationCode),
    bankCode: resolve("bankCode", parsed.bankCode),
    interfaceCode: resolve("interfaceCode", parsed.interfaceCode),
    revisionCode: parsed.revisionCode,
    compTypeCode: resolve("compTypeCode", parsed.compTypeCode),
    dieBrandCode: resolve("dieBrandCode", parsed.dieBrandCode),
    vendorCode: resolve("vendorCode", parsed.vendorCode),
    purchaserCode: resolve("purchaserCode", parsed.purchaserCode),
    compType2Code: resolve("compType2Code", parsed.compType2Code),
    packageTypeCode: resolve("packageTypeCode", parsed.packageTypeCode),
    testerCode: resolve("testerCode", parsed.testerCode)
  };
}

function toApiRequest(request: IncomingCompRequest): IncomingCompRequest {
  return {
    revision: request.revision,
    sourceCode: extractCode(request.sourceCode),
    dramTypeCode: extractCode(request.dramTypeCode),
    densityCode: extractCode(request.densityCode),
    bitOrganizationCode: extractCode(request.bitOrganizationCode),
    bankCode: extractCode(request.bankCode),
    interfaceCode: extractCode(request.interfaceCode),
    revisionCode: extractCode(request.revisionCode),
    compTypeCode: extractCode(request.compTypeCode),
    dieBrandCode: extractCode(request.dieBrandCode),
    vendorCode: extractCode(request.vendorCode),
    purchaserCode: extractCode(request.purchaserCode),
    compType2Code: extractCode(request.compType2Code),
    packageTypeCode: extractCode(request.packageTypeCode),
    testerCode: extractCode(request.testerCode)
  };
}
