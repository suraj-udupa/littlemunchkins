// import { clerkMiddleware, createRouteMatcher } from "@clerk/nextjs/server";
// const isPublicRoute = createRouteMatcher(["/sign-in(.*)", "/sign-up(.*)"]);
// export default clerkMiddleware(async (auth, req) => {
//   if (!isPublicRoute(req)) await auth.protect();
// });

import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

// Clerk disabled — passthrough middleware
export default function middleware(_req: NextRequest) {
  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next|[^?]*\\.(?:html?|css|js(?!on)|jpe?g|webp|png|gif|svg|ttf|woff2?|ico|csv|docx?|xlsx?|zip|webmanifest)).*)"],
};
