import { useState } from "react";
import { IncomingCompPage } from "./features/incoming-comp/IncomingCompPage";
import { ModulePage } from "./features/module/ModulePage";

type TabKey = "incoming" | "module";
const REVISION = "30";

export function App() {
  const [tab, setTab] = useState<TabKey>("incoming");

  return (
    <div className="app-shell">
      <header className="app-header">
        <div>
          <h1>Ramos Part Generator</h1>
          <p>Rev 30 rules, preview, parse, and Excel export flow</p>
        </div>
        <div className="revision-switch">
          <span>Spec Rev</span>
          <div className="radio-group">
            <span className="radio-pill">Rev 30</span>
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

      <div className={tab === "incoming" ? "tab-panel active" : "tab-panel hidden"}>
        <IncomingCompPage revision={REVISION} />
      </div>
      <div className={tab === "module" ? "tab-panel active" : "tab-panel hidden"}>
        <ModulePage revision={REVISION} />
      </div>
    </div>
  );
}
