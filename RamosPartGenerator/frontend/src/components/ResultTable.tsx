import type { GeneratedPartRow } from "../types";

type Props = {
  rows: GeneratedPartRow[];
};

export function ResultTable({ rows }: Props) {
  return (
    <div className="result-card">
      <div className="section-title">Preview</div>
      <div className="table-wrap">
        <table className="result-table">
          <thead>
            <tr>
              <th>Type</th>
              <th>Part Code</th>
              <th>Name</th>
              <th>General Info</th>
              <th>Specification</th>
              <th>Note</th>
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 ? (
              <tr>
                <td colSpan={6} className="empty-cell">No rows</td>
              </tr>
            ) : (
              rows.map((row, index) => (
                <tr key={`${row.partCode}-${index}`}>
                  <td>{row.kind}</td>
                  <td>{row.partCode}</td>
                  <td>{row.name}</td>
                  <td>{row.generalInfo}</td>
                  <td>{row.specification}</td>
                  <td>{row.note ?? ""}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
