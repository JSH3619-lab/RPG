import { useEffect, useState } from "react";
import { IncomingCompPage } from "./features/incoming-comp/IncomingCompPage";
import { ModulePage } from "./features/module/ModulePage";
import { api } from "./services/api";
import type { RevisionMeta } from "./types";

type TabKey = "incoming" | "module";

export function App() {
  const [tab, setTab] = useState<TabKey>("incoming");
  const [revisions, setRevisions] = useState<RevisionMeta[]>([]);
  const [revision, setRevision] = useState("30");
  const [error, setError] = useState("");

  useEffect(() => {
    api.getRevisions()
      .then((items) => {
        setRevisions(items);
        if (items.length > 0) {
          setRevision(items[items.length - 1].revision);
        }
      })
      .catch((err: Error) => setError(err.message));
  }, []);

  return (
    <div className="app-shell">
      <header className="app-header">
        <div>
          <h1>Ramos Part Generator</h1>
          <p>Rev 27 / Rev 30 rules, preview, parse, and Excel export flow</p>
        </div>
        <div className="revision-switch">
          <span>Spec Rev</span>
          <div className="radio-group">
            {revisions.map((item) => (
              <label key={item.revision} className="radio-pill">
                <input
                  type="radio"
                  name="revision"
                  value={item.revision}
                  checked={revision === item.revision}
                  onChange={() => setRevision(item.revision)}
                />
                <span>{item.displayRevision}</span>
              </label>
            ))}
          </div>
        </div>
      </header>

      <nav className="tab-bar">
        <button className={tab === "incoming" ? "tab active" : "tab"} onClick={() => setTab("incoming")}>
          Incoming &amp; Comp
        </button>
        <button className={tab === "module" ? "tab active" : "tab"} onClick={() => setTab("module")}>
          Module
        </button>
      </nav>

      {error && <div className="error-box">{error}</div>}

      {tab === "incoming" ? <IncomingCompPage revision={revision} /> : <ModulePage revision={revision} />}
    </div>
  );
}
