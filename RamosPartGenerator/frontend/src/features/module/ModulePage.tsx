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

export function ModulePage({ revision }: Props) {
  const [lookups, setLookups] = useState<LookupPage | null>(null);
  const [request, setRequest] = useState<ModuleRequest>({ ...EMPTY_REQUEST, revision });
  const [compFullPart, setCompFullPart] = useState("");
  const [moduleFullPart, setModuleFullPart] = useState("");
  const [rows, setRows] = useState<GeneratedPartRow[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    api.getModuleLookups(revision)
      .then(setLookups)
      .catch((err: Error) => setError(err.message));
    setRequest((prev) => ({ ...prev, revision }));
    setError("");
  }, [revision]);

  const grouped = useMemo(() => {
    const fields = lookups?.fields ?? [];
    return {
      quick: fields.filter((field) => field.section === "quick"),
      base: fields.filter((field) => field.section === "base"),
      structure: fields.filter((field) => field.section === "structure"),
      output: fields.filter((field) => field.section === "output")
    };
  }, [lookups]);

  function updateField(key: string, value: string) {
    setRequest((prev) => ({ ...prev, [key]: value }));
  }

  async function handleParseComp() {
    try {
      const parsed = await api.parseModuleComp(revision, compFullPart);
      setRequest((prev) => ({ ...prev, ...parsed, revision }));
      setError("");
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function handleParseModule() {
    try {
      const parsed = await api.parseModuleFull(revision, moduleFullPart);
      setRequest((prev) => ({ ...prev, ...parsed, revision }));
      setError("");
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function handlePreview() {
    try {
      const previewRows = await api.previewModule({ ...request, revision });
      setRows(previewRows);
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
          <input className="field-input" value={compFullPart} onChange={(e) => setCompFullPart(e.target.value)} />
          <button type="button" className="secondary-button" onClick={handleParseComp}>Parse</button>
        </label>
        <label className="field-row compact">
          <span className="field-label">Module Full Part</span>
          <input className="field-input" value={moduleFullPart} onChange={(e) => setModuleFullPart(e.target.value)} />
          <button type="button" className="secondary-button" onClick={handleParseModule}>Parse</button>
        </label>
        <div className="action-buttons">
          <button type="button" className="primary-button" onClick={handlePreview}>Generate</button>
          <button type="button" className="ghost-button" onClick={handleReset}>Reset</button>
        </div>
      </section>

      {error && <div className="error-box">{error}</div>}

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
  fields: LookupField[];
  values: Record<string, string>;
  onChange: (key: string, value: string) => void;
}) {
  return (
    <section className="section-card">
      <div className="section-title">{title}</div>
      <div className="field-list">
        {fields.map((field) => (
          <FieldInput
            key={field.key}
            field={field}
            value={values[field.key] ?? ""}
            onChange={onChange}
          />
        ))}
      </div>
    </section>
  );
}
