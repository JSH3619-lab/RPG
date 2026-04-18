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

export function IncomingCompPage({ revision }: Props) {
  const [lookups, setLookups] = useState<LookupPage | null>(null);
  const [request, setRequest] = useState<IncomingCompRequest>({ ...EMPTY_REQUEST, revision });
  const [compFullPart, setCompFullPart] = useState("");
  const [rows, setRows] = useState<GeneratedPartRow[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    api.getIncomingLookups(revision)
      .then(setLookups)
      .catch((err: Error) => setError(err.message));
    setRequest((prev) => ({ ...prev, revision }));
    setError("");
  }, [revision]);

  const grouped = useMemo(() => {
    const fields = lookups?.fields ?? [];
    return {
      common: fields.filter((field) => field.section === "common"),
      comp: fields.filter((field) => field.section === "comp"),
      extra: fields.filter((field) => field.section === "extra")
    };
  }, [lookups]);

  function updateField(key: string, value: string) {
    setRequest((prev) => ({ ...prev, [key]: value }));
  }

  async function handleParse() {
    try {
      const parsed = await api.parseIncomingComp(revision, compFullPart);
      setRequest(parsed);
      setError("");
    } catch (err) {
      setError((err as Error).message);
    }
  }

  async function handlePreview() {
    try {
      const previewRows = await api.previewIncoming({ ...request, revision });
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
            <input className="field-input" value={compFullPart} onChange={(e) => setCompFullPart(e.target.value)} />
          </label>
        </div>
        <div className="action-buttons">
          <button type="button" className="secondary-button" onClick={handleParse}>Parse</button>
          <button type="button" className="primary-button" onClick={handlePreview}>Generate</button>
          <button type="button" className="ghost-button" onClick={handleReset}>Reset</button>
        </div>
      </section>

      {error && <div className="error-box">{error}</div>}

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
