// OtoRehber Service Worker
// Strateji: SADECE statik asset cache'lenir. HTML/gezinme yanıtları ASLA
// cache'lenmez (giriş yapılmış sayfaların sızmasını önlemek için) — çevrimdışıysa
// dostça offline.html gösterilir.

const CACHE_VERSION = 'v3';
const STATIC_CACHE = `otorehber-static-${CACHE_VERSION}`;
const OFFLINE_URL = '/offline.html';

// Kurulumda hemen hazır bulundurulacak çekirdek dosyalar (sürüm parametresiz).
const PRECACHE_URLS = [
  OFFLINE_URL,
  '/css/app.min.css',
  '/lib/fontawesome/css/all.min.css',
  '/lib/fontawesome/webfonts/fa-solid-900.woff2',
  '/lib/aos/aos.css',
  '/lib/aos/aos.js',
  '/icons/icon-192.png',
  '/icons/icon-512.png',
  '/manifest.json'
];

// Bu yollar / uzantılar "statik asset" sayılır ve cache-first ile sunulur.
const STATIC_PREFIXES = ['/css/', '/js/', '/lib/', '/icons/', '/images/'];
const STATIC_EXT = /\.(css|js|woff2?|ttf|otf|eot|png|jpe?g|gif|svg|webp|ico|json|map)$/i;

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(STATIC_CACHE)
      .then((cache) => cache.addAll(PRECACHE_URLS))
      .then(() => self.skipWaiting())
      .catch(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(
        keys.filter((k) => k !== STATIC_CACHE).map((k) => caches.delete(k))
      ))
      .then(() => self.clients.claim())
  );
});

function isStaticAsset(url) {
  if (STATIC_EXT.test(url.pathname)) return true;
  return STATIC_PREFIXES.some((p) => url.pathname.startsWith(p));
}

self.addEventListener('fetch', (event) => {
  const req = event.request;

  if (req.method !== 'GET') return;

  const url = new URL(req.url);
  if (url.origin !== self.location.origin) return;

  // Gezinme (HTML) istekleri: network-first, cache YOK, hata → offline sayfası.
  if (req.mode === 'navigate') {
    event.respondWith(
      fetch(req).catch(() => caches.match(OFFLINE_URL, { cacheName: STATIC_CACHE }))
    );
    return;
  }

  // Statik asset: cache-first + arka planda tazele (stale-while-revalidate).
  if (isStaticAsset(url)) {
    event.respondWith(
      caches.open(STATIC_CACHE).then(async (cache) => {
        const cached = await cache.match(req);
        const network = fetch(req).then((res) => {
          if (res && res.ok && res.type === 'basic') {
            cache.put(req, res.clone());
          }
          return res;
        }).catch(() => cached);
        return cached || network;
      })
    );
    return;
  }

  // Diğer her şey (API vb.): dokunma, doğrudan ağa git.
});
