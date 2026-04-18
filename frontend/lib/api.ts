const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export interface SessionResult {
  behavior_summary: string;
  likely_cause: string;
  age_appropriate: boolean;
  suggested_actions: string[];
  when_to_seek_help?: string;
}

export interface SessionResponse {
  status: "pending" | "processing" | "complete" | "error";
  result?: SessionResult;
}

async function apiFetch(path: string, init?: RequestInit) {
  const res = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
  });
  if (!res.ok) throw new Error(`API error ${res.status}`);
  return res.json();
}

export async function requestUploadUrl(filename: string, contentType: string): Promise<{ uploadUrl: string; id: string }> {
  return apiFetch("/api/uploads/sign", {
    method: "POST",
    body: JSON.stringify({ filename, contentType }),
  });
}

export async function createSession(payload: {
  questionText?: string;
  mediaId?: string;
  childAge?: string;
}): Promise<{ sessionId: string }> {
  return apiFetch("/api/sessions", { method: "POST", body: JSON.stringify(payload) });
}

export async function getSession(id: string): Promise<SessionResponse> {
  return apiFetch(`/api/sessions/${id}`);
}
