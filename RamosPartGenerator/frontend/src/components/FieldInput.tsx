import type { LookupField } from "../types";

type Props = {
  field: LookupField;
  value: string;
  onChange: (key: string, value: string) => void;
  options?: string[];
  disabled?: boolean;
  label?: string;
};

export function FieldInput({ field, value, onChange, options, disabled = false, label }: Props) {
  const listId = `${field.key}-options`;
  const items = options ?? field.options;

  if (!field.visible) {
    return null;
  }

  return (
    <label className="field-row">
      <span className="field-label">{label ?? field.label}</span>
      <input
        className="field-input"
        list={items.length > 0 ? listId : undefined}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(field.key, event.target.value)}
      />
      {items.length > 0 && (
        <datalist id={listId}>
          {items.map((option) => (
            <option key={option} value={option} />
          ))}
        </datalist>
      )}
    </label>
  );
}
