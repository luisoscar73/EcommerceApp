const CACHE_NAME = "cafeteria-upds-v3";

const APP_SHELL = [
    "/offline.html",
    "/manifest.webmanifest",
    "/css/site.css",
    "/js/site.js",
    "/js/voice-assistant.js",
    "/icons/icon-192.png",
    "/icons/icon-512.png"
];

self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(APP_SHELL))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(
                keys
                    .filter(key => key !== CACHE_NAME)
                    .map(key => caches.delete(key))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener("fetch", event => {
    const request = event.request;

    if (request.method !== "GET")
        return;

    const url = new URL(request.url);

    if (url.origin !== self.location.origin)
        return;

    if (request.mode === "navigate") {
        event.respondWith(
            fetch(request).catch(() => caches.match("/offline.html"))
        );
        return;
    }

    if (["style", "script", "image", "font"].includes(request.destination)) {
        event.respondWith(
            caches.match(request).then(cached => {
                if (cached)
                    return cached;

                return fetch(request).then(response => {
                    if (response.ok) {
                        const copy = response.clone();
                        caches.open(CACHE_NAME)
                            .then(cache => cache.put(request, copy));
                    }

                    return response;
                });
            })
        );
    }
});
