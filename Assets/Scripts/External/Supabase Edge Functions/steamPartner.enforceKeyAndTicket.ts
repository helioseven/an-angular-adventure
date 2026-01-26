// ============================================================================
// steam-partner Edge Function (Supabase Cloud)
// Generates a short-lived Supabase JWT for a verified Steam user.
// Works with --no-verify-jwt and only needs apikey header from Unity.
// ============================================================================
import { create } from "https://deno.land/x/djwt@v3.0.1/mod.ts";

const SUPABASE_ANON_KEY = Deno.env.get("SUPABASE_ANON_KEY") ?? "";
const STEAM_WEB_API_KEY = Deno.env.get("STEAM_API_KEY") ?? "";
const STEAM_APP_IDS = (Deno.env.get("STEAM_APP_IDS") ?? "")
  .split(",")
  .map((id) => id.trim())
  .filter((id) => id.length > 0);
const ALLOW_DEV_TICKET = Deno.env.get("ALLOW_DEV_TICKET") === "true";
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
      const steamKeySig = shortSig(STEAM_WEB_API_KEY);
      console.log(
        `[steam-partner] env STEAM_API_KEY=${STEAM_WEB_API_KEY ? "set" : "missing"} steamKeySig=${steamKeySig} STEAM_APP_IDS=${STEAM_APP_IDS.join(",") || "(missing)"} ALLOW_DEV_TICKET=${ALLOW_DEV_TICKET}`
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
      console.log(
        `[steam-partner] body steamid=${steamid ? steamid : "(missing)"} ticketLen=${ticket ? ticket.length : 0}`
      );
      if (!steamid)
        return respond(
          {
            ok: false,
            error: "Provide 'steamid'",
          },
          400
        );
      if (!ticket) {
        return respond(
          {
            ok: false,
            error: "Provide 'ticket'",
          },
          400
        );
      }
      const jwtSecret = Deno.env.get("JWT_SECRET");
      if (!jwtSecret)
        return respond(
          {
            ok: false,
            error: "Missing JWT_SECRET",
          },
          500
        );
      if (ALLOW_DEV_TICKET && ticket === "DEV_TICKET") {
        console.log("[steam-partner] DEV_TICKET accepted");
      } else {
        const verify = await verifySteamTicket(steamid, ticket);
        if (!verify.ok) {
          console.log(
            `[steam-partner] ticket verify failed: ${verify.details ?? verify.error}`
          );
          return respond(
            {
              ok: false,
              error: "Steam ticket verification failed",
              details: verify.details ?? verify.error,
            },
            401
          );
        }
      }

      // --- Optional Steam profile ---
      let profile = null;
      if (STEAM_WEB_API_KEY) {
        const url = `https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=${STEAM_WEB_API_KEY}&steamids=${steamid}`;
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

async function verifySteamTicket(steamid, ticket) {
  if (!STEAM_WEB_API_KEY || STEAM_APP_IDS.length === 0) {
    return { ok: false, error: "Missing STEAM_API_KEY or STEAM_APP_IDS" };
  }

  let lastError = "Ticket verification failed";

  for (const appId of STEAM_APP_IDS) {
    const url =
      "https://partner.steam-api.com/ISteamUserAuth/AuthenticateUserTicket/v1/?" +
      `key=${encodeURIComponent(STEAM_WEB_API_KEY)}` +
      `&appid=${encodeURIComponent(appId)}` +
      `&ticket=${encodeURIComponent(ticket)}`;

    const res = await fetch(url);
    const body = await res.json().catch(() => null);

    if (!res.ok) {
      const errMsg = body?.response?.error ?? "Steam auth request failed";
      console.log(
        `[steam-partner] Steam auth HTTP ${res.status} appid=${appId} error=${errMsg}`
      );
      lastError = errMsg;
      continue;
    }

    const params = body?.response?.params ?? {};
    if (params.result && params.result !== "OK") {
      lastError = params.result;
      continue;
    }
    if (!params.steamid || params.steamid !== steamid) {
      lastError = "SteamID mismatch";
      continue;
    }

    console.log(`[steam-partner] Steam ticket verified for appid=${appId}`);
    return { ok: true };
  }

  return { ok: false, error: lastError };
}
