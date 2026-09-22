(() => {
    const assistant = document.getElementById("voiceAssistant");
    const button = document.getElementById("voiceButton");
    const panel = document.getElementById("voicePanel");
    const status = document.getElementById("voiceStatus");

    if (!assistant || !button || !panel || !status)
        return;

    const SpeechRecognition =
        window.SpeechRecognition || window.webkitSpeechRecognition;

    if (!SpeechRecognition) {
        button.disabled = true;
        button.title = "El asistente de voz requiere Chrome o Edge";
        status.textContent =
            "El reconocimiento de voz no está disponible en este navegador.";
        panel.hidden = false;
        return;
    }

    const recognition = new SpeechRecognition();
    recognition.lang = "es-BO";
    recognition.continuous = false;
    recognition.interimResults = false;

    const isAuthenticated =
        assistant.dataset.isAuthenticated === "true";
    const isAdmin = assistant.dataset.isAdmin === "true";

    const normalize = text => text
        .toLowerCase()
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .trim();

    const speak = message => {
        if (!("speechSynthesis" in window))
            return;

        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(message);
        utterance.lang = "es-BO";
        window.speechSynthesis.speak(utterance);
    };

    const goTo = (path, message) => {
        status.textContent = message;
        speak(message);
        window.setTimeout(() => {
            window.location.href = path;
        }, 650);
    };

    const executeCommand = transcript => {
        const command = normalize(transcript);

        if (command.includes("inicio")) {
            goTo("/", "Abriendo el inicio");
            return;
        }

        if (command.includes("menu") || command.includes("productos")) {
            goTo("/Products", "Abriendo el menú");
            return;
        }

        if (command.startsWith("buscar ")) {
            const term = transcript.substring(
                transcript.toLowerCase().indexOf("buscar") + 6).trim();

            if (term) {
                goTo(
                    `/Products?search=${encodeURIComponent(term)}`,
                    `Buscando ${term}`);
                return;
            }
        }

        if (command.includes("ingresar") || command.includes("iniciar sesion")) {
            goTo("/Account/Login", "Abriendo el inicio de sesión");
            return;
        }

        if (command.includes("registrar") || command.includes("crear cuenta")) {
            goTo("/Account/Register", "Abriendo el registro");
            return;
        }

        if (isAuthenticated && command.includes("carrito")) {
            goTo("/Cart", "Abriendo el carrito");
            return;
        }

        if (isAuthenticated &&
            (command.includes("mis compras") || command.includes("historial"))) {
            goTo("/Sales/MyPurchases", "Abriendo tus compras");
            return;
        }

        if (isAdmin && command.includes("carritos abandonados")) {
            goTo("/Reports#carritos", "Abriendo los carritos abandonados");
            return;
        }

        if (isAdmin &&
            (command.includes("conciliacion") ||
             command.includes("comisiones") ||
             command.includes("pagos"))) {
            goTo("/Reports#pagos", "Abriendo la conciliación de pagos");
            return;
        }

        if (isAdmin &&
            (command.includes("reporte") ||
             command.includes("productos mas vendidos"))) {
            goTo("/Reports", "Abriendo los reportes");
            return;
        }

        if (isAdmin && command.includes("ventas")) {
            goTo("/Sales", "Abriendo el panel de ventas");
            return;
        }

        if (isAdmin && command.includes("stock bajo")) {
            goTo("/Products/LowStock", "Abriendo los productos con stock bajo");
            return;
        }

        if (isAdmin && command.includes("nuevo producto")) {
            goTo("/Products/Create", "Abriendo el registro de productos");
            return;
        }

        status.textContent = `No entendí: “${transcript}”`;
        speak("No entendí el comando. Intenta decir menú, inicio o buscar seguido del producto.");
    };

    button.addEventListener("click", () => {
        panel.hidden = false;
        button.setAttribute("aria-expanded", "true");
        button.classList.add("is-listening");
        status.textContent = "Escuchando… habla ahora.";

        try {
            recognition.start();
        }
        catch {
            recognition.stop();
        }
    });

    recognition.addEventListener("result", event => {
        const transcript = event.results[0][0].transcript;
        status.textContent = `Escuché: “${transcript}”`;
        executeCommand(transcript);
    });

    recognition.addEventListener("error", event => {
        const messages = {
            "not-allowed": "Debes permitir el uso del micrófono.",
            "no-speech": "No escuché ninguna voz. Inténtalo otra vez.",
            "audio-capture": "No se encontró un micrófono disponible."
        };

        status.textContent = messages[event.error]
            ?? "No se pudo reconocer la voz.";
    });

    recognition.addEventListener("end", () => {
        button.classList.remove("is-listening");
        button.setAttribute("aria-expanded", "false");
    });
})();
