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
  function buscarNuevos() {
    fetch("/Mensajes/Nuevos/" + otroId + "?despuesDe=" + ultimoId, { credentials: "same-origin" })
      .then(function (r) { return r.ok ? r.json() : []; })
      .then(function (nuevos) {
        if (nuevos.length === 0) return;
        nuevos.forEach(function (m) {
          agregarGlobo(m);
          ultimoId = m.id;
        });
        bajarAlFinal();
      })
      .catch(function () { /* sin internet un momento: lo intentamos en la siguiente vuelta */ });
  }

  setInterval(buscarNuevos, 4000);

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
