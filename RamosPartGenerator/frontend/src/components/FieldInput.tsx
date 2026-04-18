import type { LookupField } from "../types";

type Props = {
  field: LookupField;
  value: string;
  onChange: (key: string, value: string) => void;
};

export function FieldInput({ field, value, onChange }: Props) {
  const listId = `${field.key}-options`;

  if (!field.visible) {
    return null;
  }

  return (
    <label className="field-row">
      <span className="field-label">{field.label}</span>
      <input
        className="field-input"
        list={field.options.length > 0 ? listId : undefined}
        value={value}
        onChange={(event) => onChange(field.key, event.target.value)}
      />
      {field.options.length > 0 && (
        <datalist id={listId}>
          {field.options.map((option) => (
            <option key={option} value={option} />
          ))}
        </datalist>
      )}
    </label>
  );
}
