const LOCAL_API = process.env.NEXT_PUBLIC_LOCAL_API_URL;
const CLOUDFLARE_API = process.env.NEXT_PUBLIC_API_URL as string;

let activeApiUrl: string | null = null;

export async function getActiveApiUrl(): Promise<string> {
  const fallbackUrl = CLOUDFLARE_API;

  // 1. In-memory cache (Fastest)
  if (activeApiUrl) return activeApiUrl;

  // 2. Server-Side Execution (e.g., Vercel Serverless Functions)
  // Vercel servers cannot reach your local network. Always use Cloudflare here.
  if (typeof window === 'undefined') {
    activeApiUrl = fallbackUrl;
    return activeApiUrl;
  }

  // 3. Client-Side Persistent Cache (Browser)
  // Prevent the delay on every page load across the session
  const cachedUrl = localStorage.getItem('activeApiUrl');
  if (cachedUrl) {
    activeApiUrl = cachedUrl;
    return activeApiUrl;
  }

  // 4. Client-Side Ping (Only if LOCAL_API is provided)
  if (LOCAL_API) {
    try {
      const controller = new AbortController();
      // Timeout restored to 1.5s as requested
      const timeoutId = setTimeout(() => controller.abort(), 1500);

      const response = await fetch(`${LOCAL_API}/api/health`, {
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (response.ok) {
        console.log("Using Local API");
        activeApiUrl = LOCAL_API;
        localStorage.setItem('activeApiUrl', activeApiUrl);
        return activeApiUrl;
      }
    } catch (error) {
      console.log("Local API failed, switching to Cloudflare Fallback.");
    }
  }

  // 5. Fallback to Cloudflare
  activeApiUrl = fallbackUrl;
  localStorage.setItem('activeApiUrl', activeApiUrl);
  return activeApiUrl;
}
