const CACHE_NAME = 'otorehber-cache-v1';
const urlsToCache = [
  '/',
  '/css/site.css',
  '/js/site.js',
  '/favicon.ico'
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        return cache.addAll(urlsToCache);
      })
  );
});

self.addEventListener('fetch', event => {
  // Sadece kendi sitemizdeki istekleri (same-origin) ön belleğe al veya müdahale et
  if (!event.request.url.startsWith(self.location.origin)) {
    return;
  }

  // Sadece GET isteklerini yakala (POST isteklerini bozmamak için)
  if (event.request.method !== 'GET') {
    return;
  }

  event.respondWith(
    fetch(event.request)
      .then(response => {
        if (response && response.status === 200 && response.type === 'basic') {
          const responseToCache = response.clone();
          caches.open(CACHE_NAME)
            .then(cache => {
              cache.put(event.request, responseToCache);
            });
        }
        return response;
      })
      .catch(() => {
        return caches.match(event.request);
      })
  );
});
