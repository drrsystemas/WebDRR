function mostrarInfo() {
    alert("Web Page Razor");
}

function iniciarMapa() {

    var lat = parseFloat("-26.402501");
    var lon = parseFloat("-54.588052");
    //Posicion inicial-
    var drrsystemas = { lat: lat, lng: lon };

    //Instancion el mapa
    map = new google.maps.Map(
        document.getElementById('map'), {
        zoom: 18, center: drrsystemas,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    });
}