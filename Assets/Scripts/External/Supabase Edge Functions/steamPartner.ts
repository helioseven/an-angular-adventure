// supabase/functions/steam-partner/index.ts
import { create } from "https://deno.land/x/djwt@v3.0.1/mod.ts";

export const config = {};

export default {
  async fetch(req: Request): Promise<Response> {
    if (req.method !== "POST") {
      return new Response(JSON.stringify({ ok: false, error: "Use POST" }), {
        status: 405,
        headers: { "content-type": "application/json" },
      });
    }

    try {
      const { steamid } = await req.json().catch(() => ({}));
      if (!steamid) {
        return new Response(
          JSON.stringify({ ok: false, error: "Provide 'steamid'" }),
          {
            status: 400,
            headers: { "content-type": "application/json" },
          }
        );
      }

      const jwtSecret = Deno.env.get("JWT_SECRET");
      if (!jwtSecret) {
        return new Response(
          JSON.stringify({ ok: false, error: "Missing JWT_SECRET" }),
          {
            status: 500,
            headers: { "content-type": "application/json" },
          }
        );
      }

      // ===== 1. Fetch Steam profile info =====
      const steamApiKey = Deno.env.get("STEAM_API_KEY");
      if (!steamApiKey) {
        return new Response(
          JSON.stringify({ ok: false, error: "Missing STEAM_API_KEY" }),
          {
            status: 500,
            headers: { "content-type": "application/json" },
          }
        );
      }

      const steamUrl = `https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=${steamApiKey}&steamids=${steamid}`;
      const steamRes = await fetch(steamUrl);
      const steamJson = await steamRes.json();

      // ===== 2. Create JWT =====
      const encoder = new TextEncoder();
      const key = await crypto.subtle.importKey(
        "raw",
        encoder.encode(jwtSecret),
        { name: "HMAC", hash: "SHA-256" },
        false,
        ["sign", "verify"]
      );

      const now = Math.floor(Date.now() / 1000);
      const payload = {
        sub: `steam:${steamid}`,
        role: "authenticated",
        steamid,
        iat: now,
        exp: now + 60 * 60,
      };

      const token = await create({ alg: "HS256", typ: "JWT" }, payload, key);

      // ===== 3. Return Steam profile + token =====
      return new Response(
        JSON.stringify({
          ok: true,
          steamid,
          token,
          data: steamJson, // ← this matches what your Unity code expects
        }),
        {
          headers: { "content-type": "application/json" },
        }
      );
    } catch (err) {
      console.error("Internal error:", err);
      return new Response(
        JSON.stringify({
          ok: false,
          error: "Internal error",
          details: String(err),
        }),
        {
          status: 500,
          headers: { "content-type": "application/json" },
        }
      );
    }
  },
};
