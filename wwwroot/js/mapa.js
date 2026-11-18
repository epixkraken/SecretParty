// Mapa "¿qué pasa cerca?" con Leaflet + OpenStreetMap (gratis, sin API key).
// Cóctel = hang público, candado = hang privado. Tocas uno y ves el resumen + link al hang.
(function () {
  var contenedor = document.getElementById("mapa");
  if (!contenedor || typeof L === "undefined") return; // L = Leaflet (si no cargó, no hacemos nada)

  var QUITO = [-0.1807, -78.4678];

  // El mapa, centrado en Quito
  var mapa = L.map("mapa", { zoomControl: false }).setView(QUITO, 13);
  L.control.zoom({ position: "bottomright" }).addTo(mapa);

  // Las "baldosas" (imágenes del mapa) vienen de OpenStreetMap.
  // La atribución es OBLIGATORIA por la licencia de OSM.
  L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 18,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
  }).addTo(mapa);

  // Los iconitos (son los SVG de wwwroot/img)
  function icono(archivo, oficial) {
    var tam = oficial ? 44 : 34; // los oficiales de Secret Party, más grandes
    return L.icon({
      iconUrl: "/img/" + archivo,
      iconSize: [tam, tam],
      iconAnchor: [tam / 2, tam / 2],
      popupAnchor: [0, -tam / 2],
      className: oficial ? "pin-oficial" : ""
    });
  }
  var coctel = icono("pin-coctel.svg", false);
  var candado = icono("pin-candado.svg", false);
  var coctelOficial = icono("pin-coctel.svg", true);
  var candadoOficial = icono("pin-candado.svg", true);

  // Los datos que mandó el servidor (sin direcciones: solo un punto aproximado)
  var puntos = JSON.parse(document.getElementById("puntos-mapa").textContent);

  puntos.forEach(function (p) {
    var ic = p.esPublico ? (p.esOficial ? coctelOficial : coctel)
                         : (p.esOficial ? candadoOficial : candado);
    L.marker([p.lat, p.lng], { icon: ic, title: p.titulo })
      .addTo(mapa)
      .bindPopup(armarPopup(p));
  });

  // El globito al tocar un iconito. Con createElement + textContent (NUNCA innerHTML con el título):
  // así un título raro no puede meter código en la página (XSS).
  function armarPopup(p) {
    var div = document.createElement("div");
    div.className = "popup-hang";

    var titulo = document.createElement("strong");
    titulo.textContent = (p.esOficial ? "✦ " : "") + p.titulo;

    var info = document.createElement("span");
    info.textContent = p.fecha + " · " + p.zona;

    var tipo = document.createElement("span");
    tipo.className = "nota";
    tipo.textContent = p.esPublico ? "Público: entras directo" : "Privado 🔒: el anfitrión te acepta";

    var link = document.createElement("a");
    link.href = "/Eventos/Detalle/" + encodeURIComponent(p.id);
    link.className = "boton boton-lila boton-chico";
    link.textContent = "Ver hang";

    div.appendChild(titulo);
    div.appendChild(info);
    div.appendChild(tipo);
    div.appendChild(link);
    return div;
  }

  // "Estás aquí": si la persona da permiso de ubicación Y está en Quito, ponemos el pin verde.
  // (El navegador SIEMPRE pregunta antes; si dice que no, simplemente no aparece.)
  if (navigator.geolocation) {
    navigator.geolocation.getCurrentPosition(function (pos) {
      var yo = [pos.coords.latitude, pos.coords.longitude];
      if (mapa.distance(yo, QUITO) > 30000) return; // a más de 30 km de Quito: no lo mostramos
      L.marker(yo, {
        icon: L.icon({ iconUrl: "/img/pin-tu.svg", iconSize: [32, 40], iconAnchor: [16, 38] }),
        title: "Estás aquí",
        zIndexOffset: 1000
      }).addTo(mapa);
    }, function () { /* sin permiso: no pasa nada */ }, { timeout: 8000 });
  }
})();
