import type {
  GeneratedPartRow,
  IncomingCompRequest,
  LookupPage,
  ModuleRequest,
  RevisionMeta
} from "../types";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL?.toString() ?? "";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {})
    },
    ...init
  });

  if (!response.ok) {
    let message = "요청 처리에 실패했습니다.";
    try {
      const body = await response.json();
      message = body.message ?? message;
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  return response.json() as Promise<T>;
}

export const api = {
  getRevisions(): Promise<RevisionMeta[]> {
    return request<RevisionMeta[]>("/api/meta/revisions");
  },
  getIncomingLookups(revision: string): Promise<LookupPage> {
    return request<LookupPage>(`/api/lookups/incoming/${revision}`);
  },
  getModuleLookups(revision: string): Promise<LookupPage> {
    return request<LookupPage>(`/api/lookups/module/${revision}`);
  },
  previewIncoming(requestBody: IncomingCompRequest): Promise<GeneratedPartRow[]> {
    return request<GeneratedPartRow[]>("/api/incoming-comp/preview", {
      method: "POST",
      body: JSON.stringify(requestBody)
    });
  },
  parseIncomingComp(revision: string, partCode: string): Promise<IncomingCompRequest> {
    return request<IncomingCompRequest>("/api/incoming-comp/parse", {
      method: "POST",
      body: JSON.stringify({ revision, partCode })
    });
  },
  previewModule(requestBody: ModuleRequest): Promise<GeneratedPartRow[]> {
    return request<GeneratedPartRow[]>("/api/module/preview", {
      method: "POST",
      body: JSON.stringify(requestBody)
    });
  },
  parseModuleComp(revision: string, partCode: string): Promise<ModuleRequest> {
    return request<ModuleRequest>("/api/module/parse-comp", {
      method: "POST",
      body: JSON.stringify({ revision, partCode })
    });
  },
  parseModuleFull(revision: string, partCode: string): Promise<ModuleRequest> {
    return request<ModuleRequest>("/api/module/parse-full", {
      method: "POST",
      body: JSON.stringify({ revision, partCode })
    });
  },
  async exportRegistration(rows: GeneratedPartRow[]): Promise<Blob> {
    const response = await fetch(`${API_BASE_URL}/api/export/registration`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ rows })
    });

    if (!response.ok) {
      let message = "Export failed.";
      try {
        const body = await response.json();
        message = body.message ?? message;
      } catch {
        // ignore
      }
      throw new Error(message);
    }

    return response.blob();
  }
};
