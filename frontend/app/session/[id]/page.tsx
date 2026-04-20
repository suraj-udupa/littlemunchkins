"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { getSession, SessionResult } from "@/lib/api";

export default function SessionPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [result, setResult] = useState<SessionResult | null>(null);
  const [status, setStatus] = useState<"pending" | "processing" | "complete" | "error">("pending");

  useEffect(() => {
    let cancelled = false;
    async function poll() {
      while (!cancelled) {
        const session = await getSession(id);
        if (cancelled) break;
        setStatus(session.status);
        if (session.status === "complete" || session.status === "error") {
          setResult(session.result ?? null);
          break;
        }
        await new Promise((r) => setTimeout(r, 2000));
      }
    }
    poll();
    return () => { cancelled = true; };
  }, [id]);

  if (status === "pending" || status === "processing") {
    return (
      <main className="max-w-lg mx-auto px-4 py-16 flex flex-col items-center gap-4">
        <div className="w-16 h-16 rounded-full border-4 border-orange-400 border-t-transparent animate-spin" />
        <p className="text-gray-500 text-sm">Analysing… this may take up to a minute for video.</p>
      </main>
    );
  }

  if (status === "error" || !result) {
    return (
      <main className="max-w-lg mx-auto px-4 py-16 flex flex-col items-center gap-4">
        <p className="text-red-500">Something went wrong. Please try again.</p>
        <button onClick={() => router.push("/")} className="text-sm text-orange-500 underline">Back</button>
      </main>
    );
  }

  return (
    <main className="max-w-lg mx-auto px-4 py-8 space-y-6">
      <button onClick={() => router.push("/")} className="text-sm text-orange-500 underline">← New question</button>

      <section className="bg-white rounded-2xl p-5 shadow-sm space-y-2">
        <h2 className="font-semibold text-gray-800">What&apos;s happening</h2>
        <p className="text-gray-700 text-sm">{result.behavior_summary}</p>
      </section>

      <section className="bg-white rounded-2xl p-5 shadow-sm space-y-2">
        <h2 className="font-semibold text-gray-800">Likely cause</h2>
        <p className="text-gray-700 text-sm">{result.likely_cause}</p>
      </section>

      {result.age_appropriate !== undefined && (
        <div className={`rounded-2xl px-5 py-3 text-sm font-medium ${result.age_appropriate ? "bg-green-100 text-green-700" : "bg-yellow-100 text-yellow-700"}`}>
          {result.age_appropriate ? "This behavior is typical for this age." : "This behavior may be worth discussing with a professional."}
        </div>
      )}

      <section className="bg-white rounded-2xl p-5 shadow-sm space-y-3">
        <h2 className="font-semibold text-gray-800">What you can do</h2>
        <ul className="space-y-2">
          {result.suggested_actions.map((action, i) => (
            <li key={i} className="flex gap-2 text-sm text-gray-700">
              <span className="text-orange-400 font-bold shrink-0">{i + 1}.</span>
              {action}
            </li>
          ))}
        </ul>
      </section>

      {result.when_to_seek_help && (
        <section className="bg-red-50 rounded-2xl p-5 shadow-sm space-y-2">
          <h2 className="font-semibold text-red-700">When to seek help</h2>
          <p className="text-red-600 text-sm">{result.when_to_seek_help}</p>
        </section>
      )}

      <p className="text-xs text-gray-400 text-center pb-4">
        This is not medical advice. Always consult your paediatrician for health concerns.
      </p>
    </main>
  );
}
