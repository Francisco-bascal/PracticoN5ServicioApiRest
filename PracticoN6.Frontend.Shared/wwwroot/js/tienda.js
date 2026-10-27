// Funciones de apoyo que usa la interfaz (Blazor las invoca con IJSRuntime).

// Vuelve a la pantalla anterior si hay historial; devuelve false si no hay a dónde volver.
window.tiendaVolver = function () {
    if (window.history.length > 1) {
        window.history.back();
        return true;
    }
    return false;
};

// Descarga un archivo en el navegador (host web de pruebas). En la app móvil se usa la hoja de compartir de Android.
window.tiendaDescargar = function (nombre, tipoMime, base64) {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const url = URL.createObjectURL(new Blob([bytes], { type: tipoMime }));
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombre;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10000);
};

// Almacenamiento local del navegador (host web de pruebas).
window.tiendaAlmacen = {
    leer: clave => window.localStorage.getItem(clave),
    guardar: (clave, valor) => window.localStorage.setItem(clave, valor),
    eliminar: clave => window.localStorage.removeItem(clave)
};
