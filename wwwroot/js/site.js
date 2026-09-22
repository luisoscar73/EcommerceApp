(() => {
    if ("serviceWorker" in navigator) {
        window.addEventListener("load", () => {
            navigator.serviceWorker
                .register("/service-worker.js")
                .catch(error =>
                    console.error("No se pudo registrar el PWA:", error));
        });
    }

    let installPrompt = null;
    const installButton = document.getElementById("installAppButton");

    window.addEventListener("beforeinstallprompt", event => {
        event.preventDefault();
        installPrompt = event;

        if (installButton)
            installButton.hidden = false;
    });

    installButton?.addEventListener("click", async () => {
        if (!installPrompt)
            return;

        installPrompt.prompt();
        await installPrompt.userChoice;
        installPrompt = null;
        installButton.hidden = true;
    });

    window.addEventListener("appinstalled", () => {
        installPrompt = null;

        if (installButton)
            installButton.hidden = true;
    });
})();
