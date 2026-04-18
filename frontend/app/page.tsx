"use client";

import { useState, useRef } from "react";
import { useRouter } from "next/navigation";
import { useAuth, UserButton, SignInButton } from "@clerk/nextjs";
import { createSession, requestUploadUrl } from "@/lib/api";

type InputMode = "text" | "photo" | "video" | "audio";

export default function Home() {
  const { isSignedIn } = useAuth();
  const router = useRouter();
  const [mode, setMode] = useState<InputMode>("text");
  const [question, setQuestion] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [recording, setRecording] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [childAge, setChildAge] = useState("");
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);

  async function startRecording() {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    const recorder = new MediaRecorder(stream);
    audioChunksRef.current = [];
    recorder.ondataavailable = (e) => audioChunksRef.current.push(e.data);
    recorder.onstop = () => {
      const blob = new Blob(audioChunksRef.current, { type: "audio/webm" });
      setFile(new File([blob], "recording.webm", { type: "audio/webm" }));
      stream.getTracks().forEach((t) => t.stop());
    };
    recorder.start();
    mediaRecorderRef.current = recorder;
    setRecording(true);
  }

  function stopRecording() {
    mediaRecorderRef.current?.stop();
    setRecording(false);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!isSignedIn) return;
    setSubmitting(true);

    try {
      let mediaId: string | undefined;

      if (file) {
        const { uploadUrl, id } = await requestUploadUrl(file.name, file.type);
        await fetch(uploadUrl, { method: "PUT", body: file, headers: { "Content-Type": file.type } });
        mediaId = id;
      }

      const { sessionId } = await createSession({ questionText: question || undefined, mediaId, childAge: childAge || undefined });
      router.push(`/session/${sessionId}`);
    } finally {
      setSubmitting(false);
    }
  }

  const modes: { key: InputMode; label: string; icon: string }[] = [
    { key: "text", label: "Ask", icon: "💬" },
    { key: "photo", label: "Photo", icon: "📷" },
    { key: "video", label: "Video", icon: "🎥" },
    { key: "audio", label: "Record", icon: "🎙️" },
  ];

  return (
    <main className="max-w-lg mx-auto px-4 py-8">
      <header className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-orange-600">Little Munchkins</h1>
          <p className="text-sm text-gray-500">Understand your baby&apos;s behavior</p>
        </div>
        {isSignedIn ? <UserButton /> : <SignInButton><button className="text-sm bg-orange-500 text-white px-3 py-1.5 rounded-lg">Sign in</button></SignInButton>}
      </header>

      <form onSubmit={handleSubmit} className="space-y-5">
        {/* Age */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Child&apos;s age</label>
          <input
            type="text"
            placeholder="e.g. 14 months, 3 years"
            value={childAge}
            onChange={(e) => setChildAge(e.target.value)}
            className="w-full border border-gray-300 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-400"
          />
        </div>

        {/* Mode selector */}
        <div className="grid grid-cols-4 gap-2">
          {modes.map((m) => (
            <button
              key={m.key}
              type="button"
              onClick={() => { setMode(m.key); setFile(null); }}
              className={`flex flex-col items-center py-3 rounded-xl text-xs font-medium transition-colors ${
                mode === m.key ? "bg-orange-500 text-white" : "bg-white border border-gray-200 text-gray-600"
              }`}
            >
              <span className="text-xl mb-1">{m.icon}</span>
              {m.label}
            </button>
          ))}
        </div>

        {/* Text */}
        {mode === "text" && (
          <textarea
            rows={4}
            placeholder="Describe what your child is doing or ask a question…"
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            className="w-full border border-gray-300 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-orange-400 resize-none"
          />
        )}

        {/* Photo */}
        {mode === "photo" && (
          <div className="flex flex-col items-center gap-3">
            <input
              type="file"
              accept="image/*"
              capture="environment"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className="w-full text-sm text-gray-600 file:mr-3 file:py-2 file:px-4 file:rounded-lg file:border-0 file:bg-orange-100 file:text-orange-700"
            />
            {file && <p className="text-xs text-gray-500">{file.name}</p>}
          </div>
        )}

        {/* Video */}
        {mode === "video" && (
          <div className="flex flex-col items-center gap-3">
            <input
              type="file"
              accept="video/*"
              capture="environment"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className="w-full text-sm text-gray-600 file:mr-3 file:py-2 file:px-4 file:rounded-lg file:border-0 file:bg-orange-100 file:text-orange-700"
            />
            {file && <p className="text-xs text-gray-500">{file.name}</p>}
          </div>
        )}

        {/* Audio */}
        {mode === "audio" && (
          <div className="flex flex-col items-center gap-3">
            {!file ? (
              <button
                type="button"
                onPointerDown={startRecording}
                onPointerUp={stopRecording}
                className={`w-24 h-24 rounded-full text-4xl flex items-center justify-center transition-colors ${
                  recording ? "bg-red-500 animate-pulse" : "bg-orange-500"
                }`}
              >
                🎙️
              </button>
            ) : (
              <div className="flex flex-col items-center gap-2">
                <span className="text-green-600 text-sm">Recording saved</span>
                <button type="button" onClick={() => setFile(null)} className="text-xs text-gray-400 underline">Re-record</button>
              </div>
            )}
            <p className="text-xs text-gray-400">{recording ? "Recording… release to stop" : "Hold to record"}</p>
          </div>
        )}

        {/* Optional follow-up question when uploading media */}
        {mode !== "text" && (
          <textarea
            rows={2}
            placeholder="Any specific question? (optional)"
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            className="w-full border border-gray-300 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-orange-400 resize-none"
          />
        )}

        <button
          type="submit"
          disabled={submitting || !isSignedIn || (mode !== "text" && !file) || (mode === "text" && !question.trim())}
          className="w-full bg-orange-500 disabled:bg-gray-300 text-white font-semibold py-3 rounded-xl transition-colors"
        >
          {submitting ? "Analysing…" : "Analyse"}
        </button>

        {!isSignedIn && (
          <p className="text-center text-xs text-gray-400">Sign in to use Little Munchkins</p>
        )}
      </form>
    </main>
  );
}
