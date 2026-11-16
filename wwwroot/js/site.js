// JavaScript compartido por todas las páginas de Secret Party.

// ---------- VISTA PREVIA DE FOTOS ----------
// Cualquier <input type="file" data-vista-previa="ID"> muestra la foto elegida
// dentro del elemento con ese ID, ANTES de mandarla al servidor.
document.querySelectorAll("input[type=file][data-vista-previa]").forEach(function (input) {
  input.addEventListener("change", function () {
    var archivo = input.files[0];
    if (!archivo) return;

    var destino = document.getElementById(input.dataset.vistaPrevia);
    // URL.createObjectURL crea una dirección temporal para ver el archivo local
    var url = URL.createObjectURL(archivo);

    var img = destino.tagName === "IMG" ? destino : destino.querySelector("img");
    if (!img) {
      destino.textContent = ""; // quita la inicial
      img = document.createElement("img");
      img.className = destino.classList.contains("avatar") ? "avatar-foto" : "";
      destino.appendChild(img);
    }
    img.src = url;
  });
});
