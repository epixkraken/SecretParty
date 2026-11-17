// Chat de Secret Party: baja al último mensaje y cada 4 segundos pregunta si hay mensajes nuevos.
(function () {
  var chat = document.getElementById("chat");
  if (!chat) return;

  var otroId = chat.dataset.otro;
  var ultimoId = parseInt(chat.dataset.ultimo, 10) || 0;
  var fin = document.getElementById("fin");

  function bajarAlFinal() {
    fin.scrollIntoView({ block: "end" });
  }
  bajarAlFinal();

  // Dibuja un globo nuevo. OJO: usamos textContent y NO innerHTML,
  // así si alguien escribe "<script>..." se ve como texto y nunca se ejecuta (evita XSS).
  function agregarGlobo(m) {
    var vacio = chat.querySelector(".chat-vacio");
    if (vacio) vacio.remove();

    var globo = document.createElement("div");
    globo.className = "globo " + (m.mio ? "globo-mio" : "globo-otro");

    var p = document.createElement("p");
    p.textContent = m.texto;

    var hora = document.createElement("span");
    hora.className = "globo-hora";
    hora.textContent = m.hora;

    globo.appendChild(p);
    globo.appendChild(hora);
    chat.insertBefore(globo, fin);
  }

  // fetch = pedirle algo al servidor sin recargar la página
  // Cada cuánto preguntamos. Si algo falla, esperamos cada vez más (backoff) en vez de insistir a lo loco:
  // el paper advierte que reintentar sin parar es un ANTIPATRÓN.
  var ESPERA_NORMAL = 4000;   // 4 s
  var ESPERA_MAXIMA = 30000;  // 30 s
  var espera = ESPERA_NORMAL;
  var aviso = document.getElementById("aviso-conexion");

  function mostrarAviso(texto) {
    if (!aviso) return;
    aviso.textContent = texto;
    aviso.style.display = texto ? "" : "none";
  }

  function esperarMas() {
    espera = Math.min(espera * 2, ESPERA_MAXIMA); // 4 → 8 → 16 → 30 s
  }

  // fetch = pedirle algo al servidor sin recargar la página
  function buscarNuevos() {
    fetch("/Mensajes/Nuevos/" + otroId + "?despuesDe=" + ultimoId, { credentials: "same-origin" })
      .then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);

        if (r.headers.get("X-Sin-Conexion") === "1") {
          // El servidor usó su PLAN B (fallback): no pudo leer la base
          esperarMas();
          mostrarAviso("Sin conexión con el chat. Reintentando en " + espera / 1000 + " s…");
        } else {
          espera = ESPERA_NORMAL;
          mostrarAviso("");
        }
        return r.json();
      })
      .then(function (nuevos) {
        if (nuevos.length === 0) return;
        nuevos.forEach(function (m) {
          agregarGlobo(m);
          ultimoId = m.id;
        });
        bajarAlFinal();
      })
      .catch(function () {
        // El servidor ni respondió (apagado, sin internet): RETRY con espera creciente
        esperarMas();
        mostrarAviso("Sin conexión. Reintentando en " + espera / 1000 + " s…");
      })
      .finally(function () {
        setTimeout(buscarNuevos, espera); // programa la siguiente vuelta
      });
  }

  setTimeout(buscarNuevos, espera);

  // Enter envía; Shift+Enter hace un salto de línea
  var texto = document.getElementById("texto-chat");
  if (texto) {
    texto.addEventListener("keydown", function (e) {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        if (texto.value.trim() !== "") texto.form.submit();
      }
    });
    texto.focus();
  }
})();
