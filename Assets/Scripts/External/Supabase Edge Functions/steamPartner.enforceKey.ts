// ============================================================================
// steam-partner Edge Function (Supabase Cloud)
// Generates a short-lived Supabase JWT for a verified Steam user.
// Works with --no-verify-jwt and only needs apikey header from Unity.
// ============================================================================
import { create } from "https://deno.land/x/djwt@v3.0.1/mod.ts";

const SUPABASE_ANON_KEY = Deno.env.get("SUPABASE_ANON_KEY") ?? "";
export const config = {
  cors: {
    origin: "*",
    methods: ["POST", "OPTIONS"],
    allowedHeaders: ["content-type", "apikey"],
  },
};
export default {
  async fetch(req) {
    // --- Preflight ---
    if (req.method === "OPTIONS")
      return new Response(null, {
        status: 204,
      });
    if (req.method !== "POST")
      return respond(
        {
          ok: false,
          error: "Use POST",
        },
        405
      );
    try {
      const apikey = req.headers.get("apikey") ?? "";
      const incomingSig = shortSig(apikey);
      const expectedSig = shortSig(SUPABASE_ANON_KEY);
      console.log(
        `[steam-partner] apikey present=${apikey.length > 0} len=${apikey.length} incoming=${incomingSig} expected=${expectedSig}`
      );
      if (!apikey || apikey !== SUPABASE_ANON_KEY) {
        return respond(
          {
            ok: false,
            error: "Invalid apikey",
          },
          401
        );
      }

      const { steamid, ticket } = await req.json().catch(() => ({}));
      if (!steamid)
        return respond(
          {
            ok: false,
            error: "Provide 'steamid'",
          },
          400
        );
      const jwtSecret = Deno.env.get("JWT_SECRET");
      if (!jwtSecret)
        return respond(
          {
            ok: false,
            error: "Missing JWT_SECRET",
          },
          500
        );
      // --- Optional Steam verification ---
      const steamApiKey = Deno.env.get("STEAM_API_KEY");
      let profile = null;
      if (steamApiKey) {
        const url = `https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=${steamApiKey}&steamids=${steamid}`;
        const res = await fetch(url);
        profile = await res.json().catch(() => null);
      }
      // --- Sign JWT ---
      const now = Math.floor(Date.now() / 1000);
      const key = await crypto.subtle.importKey(
        "raw",
        new TextEncoder().encode(jwtSecret),
        {
          name: "HMAC",
          hash: "SHA-256",
        },
        false,
        ["sign"]
      );
      const payload = {
        sub: `steam:${steamid}`,
        role: "authenticated",
        steamid,
        iat: now,
        exp: now + 3600,
      };
      const token = await create(
        {
          alg: "HS256",
          typ: "JWT",
        },
        payload,
        key
      );
      return respond({
        ok: true,
        steamid,
        token,
        data: profile,
      });
    } catch (err) {
      console.error("Internal error:", err);
      return respond(
        {
          ok: false,
          error: "Internal error",
          details: String(err),
        },
        500
      );
    }
  },
};
function respond(obj, status = 200) {
  return new Response(JSON.stringify(obj), {
    status,
    headers: {
      "content-type": "application/json",
      "access-control-allow-origin": "*",
      "access-control-allow-headers": "content-type,apikey",
    },
  });
}

function shortSig(value) {
  if (!value || value.length < 12) return "(missing)";
  return `${value.slice(0, 6)}...${value.slice(-4)}`;
}
