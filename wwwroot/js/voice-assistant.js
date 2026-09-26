(() => {
    "use strict";

    const assistant = document.getElementById("voiceAssistant");
    const button = document.getElementById("voiceButton");
    const panel = document.getElementById("voicePanel");
    const status = document.getElementById("voiceStatus");
    const closeButton = document.getElementById("voiceCloseButton");
    const helpButton = document.getElementById("voiceHelpButton");
    const retryButton = document.getElementById("voiceRetryButton");
    const examples = document.getElementById("voiceExamples");

    if (!assistant || !button || !panel || !status)
        return;

    const Recognition =
        window.SpeechRecognition || window.webkitSpeechRecognition;

    const isAuthenticated =
        assistant.dataset.isAuthenticated === "true";
    const isAdmin = assistant.dataset.isAdmin === "true";

    let recognition = null;
    let isListening = false;

    const normalize = text => text
        .toLocaleLowerCase("es")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/[^a-z0-9ñ\s]/g, " ")
        .replace(/\s+/g, " ")
        .trim();

    const includesAny = (text, options) =>
        options.some(option => text.includes(option));

    const showPanel = () => {
        panel.hidden = false;
        button.setAttribute("aria-expanded", "true");
    };

    const setStatus = (message, state = "") => {
        showPanel();
        status.textContent = message;
        panel.dataset.state = state;
    };

    const speak = message => {
        if (!("speechSynthesis" in window))
            return;

        window.speechSynthesis.cancel();

        const utterance = new SpeechSynthesisUtterance(message);
        utterance.lang = "es-BO";
        utterance.rate = 1;
        window.speechSynthesis.speak(utterance);
    };

    const goTo = (path, message) => {
        setStatus(message, "success");
        speak(message);

        window.setTimeout(() => {
            window.location.assign(new URL(path, document.baseURI));
        }, 700);
    };

    const requireLogin = (path, optionName) => {
        const loginPath =
            `/Account/Login?returnUrl=${encodeURIComponent(path)}`;

        goTo(loginPath, `Inicia sesión para abrir ${optionName}`);
    };

    const showHelp = () => {
        const roleCommands = isAdmin
            ? "ventas, reportes, descargar reporte, ventas por periodo, productos más vendidos, stock bajo, carritos abandonados, pagos o nuevo producto"
            : isAuthenticated
                ? "carrito o mis compras"
                : "iniciar sesión o crear cuenta";

        examples?.removeAttribute("hidden");
        setStatus(
            `Puedes decir: inicio, menú, buscar seguido de un producto, ${roleCommands}.`,
            "help");
        speak(`Comandos disponibles: inicio, menú, buscar un producto, ${roleCommands}.`);
    };

    const executeCommand = transcript => {
        const command = normalize(transcript);

        if (!command) {
            setStatus(
                "No escuché palabras. Pulsa Escuchar e inténtalo otra vez.",
                "error");
            return;
        }

        if (includesAny(command, ["ayuda", "comandos", "que puedo decir"])) {
            showHelp();
            return;
        }

        if (includesAny(command, ["cerrar asistente", "cancelar", "detener"])) {
            panel.hidden = true;
            button.setAttribute("aria-expanded", "false");
            return;
        }

        if (includesAny(command, ["inicio", "pagina principal", "ir a casa"])) {
            goTo("/", "Abriendo el inicio");
            return;
        }

        const searchMatch = command.match(
            /^(?:buscar|busca|encuentra|mostrar)\s+(?:producto\s+)?(.+)$/);

        if (searchMatch?.[1]) {
            const term = searchMatch[1].trim();
            goTo(
                `/Products?search=${encodeURIComponent(term)}`,
                `Buscando ${term}`);
            return;
        }

        if (includesAny(command, [
            "menu", "productos", "ver productos", "catalogo"
        ])) {
            goTo("/Products", "Abriendo el menú");
            return;
        }

        if (includesAny(command, [
            "iniciar sesion", "inicio de sesion", "ingresar", "entrar"
        ])) {
            goTo("/Account/Login", "Abriendo el inicio de sesión");
            return;
        }

        if (includesAny(command, [
            "registrarme", "registro", "crear cuenta", "nueva cuenta"
        ])) {
            goTo("/Account/Register", "Abriendo el registro");
            return;
        }

        if (command.includes("carrito")) {
            if (isAuthenticated)
                goTo("/Cart", "Abriendo el carrito");
            else
                requireLogin("/Cart", "el carrito");
            return;
        }

        if (includesAny(command, [
            "mis compras", "mis pedidos", "historial"
        ])) {
            if (isAuthenticated)
                goTo("/Sales/MyPurchases", "Abriendo tus compras");
            else
                requireLogin("/Sales/MyPurchases", "tus compras");
            return;
        }

        if (includesAny(command, ["factura", "recibo", "comprobante"])) {
            if (!isAuthenticated) {
                requireLogin("/Sales/MyPurchases", "tus comprobantes");
                return;
            }

            if (isAdmin)
                goTo("/Sales", "Abriendo las ventas para elegir el comprobante");
            else
                goTo("/Sales/MyPurchases", "Abriendo tus comprobantes");
            return;
        }

        const reportCommands = [
            "reporte", "reportes", "ventas por periodo",
            "productos mas vendidos", "carritos abandonados",
            "conciliacion", "comisiones", "pagos"
        ];

        if (includesAny(command, reportCommands)) {
            if (!isAdmin) {
                setStatus(
                    "Los reportes solo están disponibles para el administrador.",
                    "error");
                speak("Los reportes requieren una cuenta de administrador.");
                return;
            }

            if (includesAny(command, [
                "descargar reporte", "reporte pdf", "informe pdf"
            ])) {
                goTo(
                    "/Reports/DownloadPdf",
                    "Preparando el reporte en PDF");
                return;
            }

            if (command.includes("carritos abandonados")) {
                goTo(
                    "/Reports#carritos",
                    "Abriendo los carritos abandonados");
                return;
            }

            if (includesAny(command, [
                "conciliacion", "comisiones", "pagos"
            ])) {
                goTo(
                    "/Reports#pagos",
                    "Abriendo la conciliación de pagos");
                return;
            }

            goTo("/Reports", "Abriendo los reportes");
            return;
        }

        if (includesAny(command, ["stock bajo", "inventario bajo"])) {
            if (isAdmin) {
                goTo(
                    "/Products/LowStock",
                    "Abriendo el inventario con stock bajo");
            }
            else {
                setStatus(
                    "El stock bajo solo está disponible para el administrador.",
                    "error");
                speak("Esta opción requiere una cuenta de administrador.");
            }
            return;
        }

        if (includesAny(command, [
            "nuevo producto", "crear producto", "registrar producto"
        ])) {
            if (isAdmin) {
                goTo(
                    "/Products/Create",
                    "Abriendo el registro de productos");
            }
            else {
                setStatus(
                    "Registrar productos requiere una cuenta de administrador.",
                    "error");
                speak("Esta opción requiere una cuenta de administrador.");
            }
            return;
        }

        if (includesAny(command, ["ventas", "panel de ventas"])) {
            if (isAdmin)
                goTo("/Sales", "Abriendo el panel de ventas");
            else {
                setStatus(
                    "El panel de ventas solo está disponible para el administrador.",
                    "error");
                speak("Esta opción requiere una cuenta de administrador.");
            }
            return;
        }

        setStatus(
            `No entendí “${transcript}”. Pulsa Comandos para ver ejemplos.`,
            "error");
        speak(
            "No entendí el comando. Pulsa Comandos para ver algunos ejemplos.");
    };

    const stopListening = () => {
        if (recognition && isListening)
            recognition.abort();
    };

    const startListening = async () => {
        showPanel();
        examples?.setAttribute("hidden", "");

        if (!Recognition) {
            setStatus(
                "Este navegador no permite reconocimiento de voz. Usa Chrome o Edge.",
                "error");
            button.disabled = true;
            retryButton?.setAttribute("disabled", "");
            return;
        }

        if (!window.isSecureContext && location.hostname !== "localhost") {
            setStatus(
                "El micrófono necesita una conexión HTTPS segura.",
                "error");
            return;
        }

        if (isListening) {
            stopListening();
            return;
        }

        try {
            if (navigator.permissions?.query) {
                const permission = await navigator.permissions.query({
                    name: "microphone"
                });

                if (permission.state === "denied") {
                    setStatus(
                        "El micrófono está bloqueado. Permítelo desde el candado de la barra de direcciones.",
                        "error");
                    return;
                }
            }
        }
        catch {
            // Algunos navegadores no permiten consultar este permiso.
        }

        recognition ??= new Recognition();
        recognition.lang = navigator.language?.toLowerCase().startsWith("es")
            ? navigator.language
            : "es-BO";
        recognition.continuous = false;
        recognition.interimResults = true;
        recognition.maxAlternatives = 3;

        recognition.onstart = () => {
            isListening = true;
            button.classList.add("is-listening");
            button.setAttribute("aria-label", "Detener asistente de voz");

            if (retryButton)
                retryButton.textContent = "Detener";

            setStatus("Escuchando… habla ahora.", "listening");
        };

        recognition.onresult = event => {
            const result = event.results[event.resultIndex];
            const transcript = result[0]?.transcript?.trim() ?? "";

            if (!result.isFinal) {
                setStatus(`Escuchando: “${transcript}…”`, "listening");
                return;
            }

            setStatus(`Escuché: “${transcript}”`, "success");
            executeCommand(transcript);
        };

        recognition.onerror = event => {
            const messages = {
                "not-allowed": "El micrófono está bloqueado. Permítelo desde el candado del navegador.",
                "service-not-allowed": "El servicio de voz está bloqueado por el navegador.",
                "no-speech": "No escuché ninguna voz. Acércate al micrófono e inténtalo otra vez.",
                "audio-capture": "No se encontró un micrófono disponible.",
                "network": "No se pudo conectar al servicio de voz. Revisa tu Internet.",
                "language-not-supported": "El idioma español no está disponible en este navegador.",
                "aborted": "Escucha detenida."
            };

            setStatus(
                messages[event.error]
                    ?? `No se pudo reconocer la voz (${event.error}).`,
                event.error === "aborted" ? "" : "error");
        };

        recognition.onend = () => {
            isListening = false;
            button.classList.remove("is-listening");
            button.setAttribute("aria-label", "Activar asistente de voz");

            if (retryButton)
                retryButton.textContent = "Escuchar";
        };

        try {
            recognition.start();
        }
        catch (error) {
            isListening = false;
            setStatus(
                "El asistente ya estaba activo. Espera un momento y vuelve a intentarlo.",
                "error");
            console.error(
                "No se pudo iniciar el reconocimiento de voz:",
                error);
        }
    };

    button.addEventListener("click", startListening);
    retryButton?.addEventListener("click", startListening);
    helpButton?.addEventListener("click", showHelp);

    closeButton?.addEventListener("click", () => {
        stopListening();
        panel.hidden = true;
        button.setAttribute("aria-expanded", "false");
    });

    examples?.addEventListener("click", event => {
        const commandButton =
            event.target.closest("[data-voice-command]");

        if (commandButton)
            executeCommand(commandButton.dataset.voiceCommand ?? "");
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && !panel.hidden) {
            stopListening();
            panel.hidden = true;
            button.setAttribute("aria-expanded", "false");
        }
    });

    window.addEventListener("pagehide", stopListening);
})();
