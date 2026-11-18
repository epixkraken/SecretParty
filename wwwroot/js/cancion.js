// Buscador de canciones del formulario de hang ("Se escucha").
// Escribes -> esperamos 400 ms a que dejes de teclear -> le preguntamos a NUESTRO servidor
// (/Eventos/BuscarCancion), que a su vez le pregunta a iTunes -> mostramos la lista.
(function () {
  var buscador = document.getElementById("buscar-cancion");
  if (!buscador) return;

  var lista = document.getElementById("resultados-cancion");
  var elegida = document.getElementById("cancion-elegida");
  var campos = {
    titulo: document.getElementById("CancionTitulo"),
    artista: document.getElementById("CancionArtista"),
    portada: document.getElementById("CancionPortada"),
    preview: document.getElementById("CancionPreview")
  };

  var temporizador = null;
  var ultimaBusqueda = "";

  // "Debounce": si sigues escribiendo, cancelamos la búsqueda anterior.
  // Así no mandamos una petición por CADA letra.
  buscador.addEventListener("input", function () {
    clearTimeout(temporizador);
    var texto = buscador.value.trim();
    if (texto.length < 2) { esconderLista(); return; }
    temporizador = setTimeout(function () { buscar(texto); }, 400);
  });

  // Enter dentro del buscador NO debe mandar el formulario entero
  buscador.addEventListener("keydown", function (e) {
    if (e.key === "Enter") e.preventDefault();
    if (e.key === "Escape") esconderLista();
  });

  function buscar(texto) {
    ultimaBusqueda = texto;
    mostrarMensaje("Buscando…");
    fetch("/Eventos/BuscarCancion?q=" + encodeURIComponent(texto), { credentials: "same-origin" })
      .then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      })
      .then(function (canciones) {
        if (texto !== ultimaBusqueda) return; // llegó tarde una búsqueda vieja: la ignoramos
        if (canciones.length === 0) {
          mostrarMensaje("No encontramos nada 😕 (o iTunes no respondió). Prueba con otro nombre.");
          return;
        }
        dibujarLista(canciones);
      })
      .catch(function () {
        mostrarMensaje("No pudimos buscar ahora. Intenta otra vez en un ratito.");
      });
  }

  // OJO: armamos todo con createElement + textContent (NUNCA innerHTML con datos de afuera),
  // así si una canción se llamara "<script>..." se vería como texto y no se ejecutaría (XSS).
  function dibujarLista(canciones) {
    lista.textContent = "";
    canciones.forEach(function (c) {
      var li = document.createElement("li");
      var boton = document.createElement("button");
      boton.type = "button";
      boton.className = "resultado-cancion";

      var img = document.createElement("img");
      img.src = c.portada;
      img.alt = "";
      img.className = "cancion-portada";

      var textoDiv = document.createElement("div");
      textoDiv.className = "cancion-texto";
      var titulo = document.createElement("strong");
      titulo.className = "cancion-titulo";
      titulo.textContent = c.titulo;
      var artista = document.createElement("span");
      artista.className = "cancion-artista";
      artista.textContent = c.artista;
      textoDiv.appendChild(titulo);
      textoDiv.appendChild(artista);

      boton.appendChild(img);
      boton.appendChild(textoDiv);
      boton.addEventListener("click", function () { elegir(c); });

      li.appendChild(boton);
      lista.appendChild(li);
    });
    lista.hidden = false;
  }

  function mostrarMensaje(texto) {
    lista.textContent = "";
    var li = document.createElement("li");
    li.className = "nota resultado-mensaje";
    li.textContent = texto;
    lista.appendChild(li);
    lista.hidden = false;
  }

  function esconderLista() {
    lista.hidden = true;
    lista.textContent = "";
  }

  // Al elegir: llenamos los campos escondidos y mostramos la tarjetita
  function elegir(c) {
    campos.titulo.value = c.titulo;
    campos.artista.value = c.artista;
    campos.portada.value = c.portada;
    campos.preview.value = c.preview || "";

    elegida.querySelector("img").src = c.portada;
    elegida.querySelector(".cancion-titulo").textContent = c.titulo;
    elegida.querySelector(".cancion-artista").textContent = c.artista;
    elegida.hidden = false;

    buscador.value = "";
    esconderLista();
  }

  // La X: sin canción
  document.getElementById("quitar-cancion").addEventListener("click", function () {
    Object.keys(campos).forEach(function (k) { campos[k].value = ""; });
    elegida.hidden = true;
    buscador.focus();
  });
})();
