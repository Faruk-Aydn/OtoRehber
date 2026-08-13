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
  event.respondWith(
    fetch(event.request)
      .then(response => {
        // Ağ isteği başarılı olursa, cache'i güncelle ve cevabı dön
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
        // Eğer ağa ulaşılamazsa (offline), o zaman cache'den getir
        return caches.match(event.request);
      })
  );
});
