let latitud = 0;
let longitud = 0;
let map;
let marker;
let service;
let geocoder = new google.maps.Geocoder();


window.onload = iniciarMapa();


function showLocation(position)
{
     latitude = position.coords.latitude;
     longitude = position.coords.longitude;
    //alert("Latitude : " + latitude + " Longitude: " + longitude);

    $("#latitud").val(latitude.toFixed(6).replace(".", ","));
    $("#longitud").val(longitude.toFixed(6).replace(".", ","));


    $("#latitudActual").val(latitude.toFixed(6));
    $("#longitudActual").val(longitude.toFixed(6));
}


function errorHandler(err) {
    if (err.code == 1) {
        alert("Error: Access is denied!");
    } else if (err.code == 2) {
        alert("Error: Position is unavailable!");
    }
}


function getLocation()
{
    if (navigator.geolocation) {

        // timeout at 60000 milliseconds (60 seconds)
        var options = { timeout: 60000 };
        navigator.geolocation.getCurrentPosition(showLocation, errorHandler, options);
        iniciarMapa();
    } else {
        alert("Su navegador no soporta la funcione de geolocalización!");
    }
}


function iniciarMapa() {

    var titulo = $("#titulo_rs").text();

    var lat = $("#latitudActual").val();
    var lon = $("#longitudActual").val();



    if (!((lat === null || lat === undefined || lat === "") || (lon === null || lon === undefined || lon === ""))) {

        lat = parseFloat(lat.replace(",", "."));
        lon = parseFloat(lon.replace(",", "."));

        //Posicion inicial-
        var drrsystemas = { lat: lat, lng: lon };

        //Instancion el mapa
        map = new google.maps.Map(
            document.getElementById('map'), { zoom: 16, center: drrsystemas, mapTypeId: google.maps.MapTypeId.ROADMAP });


        //genero el primer punto.
        marker = new google.maps.Marker({
            position: drrsystemas, map: map,
            draggable: true,
            title: titulo
        });

        map.setCenter(marker.position);
        marker.setMap(map);       

    }
    else {

           lat = -26.402501;
            lon = -54.588052;

        var drrsystemas = { lat: lat, lng: lon };

        //Instancion el mapa
        map = new google.maps.Map(
            document.getElementById('map'), { zoom: 16, center: drrsystemas, mapTypeId: google.maps.MapTypeId.ROADMAP });


        //genero el primer punto.
        marker = new google.maps.Marker({
            position: drrsystemas, map: map,
            draggable: true,
            title: titulo
        });

        map.setCenter(marker.position);
        marker.setMap(map);


        geocodeAddress(geocoder, map);
    }


    marker.addListener("click", () => {

        var lat = $("#latitudActual").val();
        var lon = $("#longitudActual").val();

        lat = parseFloat(lat.replace(",", "."));
        lon = parseFloat(lon.replace(",", "."));

        var pos = marker.getPosition();

        var enlace = "https://maps.google.com/?q=";
        enlace += lat + ",";
        enlace += lon;
        const contenido = '<div style="font-size: 10pt;">' +
            '<p>"' + titulo + '"</p>' +
            '</br><a href="' + enlace + '"  target="_blank">Ver google maps</a>';
        const infowindow = new google.maps.InfoWindow({
            content: contenido,
        });


        infowindow.open(map, marker);
    });


    //Arrastrar el maker.
    google.maps.event.addListener(marker, 'dragend', function (evt) {

        $("#latitudActual").val(evt.latLng.lat().toFixed(6));
        $("#longitudActual").val(evt.latLng.lng().toFixed(6));
        $("#latitud").val(evt.latLng.lat().toFixed(6).replace(".", ","));
        $("#longitud").val(evt.latLng.lng().toFixed(6).replace(".", ","));
        map.panTo(evt.latLng);

        getDireccion(marker);
    });

    //Click que muestre el maker en esa posicion.
    map.addListener("click", function (evt) {

        $("#latitudActual").val(evt.latLng.lat().toFixed(6));
        $("#longitudActual").val(evt.latLng.lng().toFixed(6));
        $("#latitud").val(evt.latLng.lat().toFixed(6).replace(".", ","));
        $("#longitud").val(evt.latLng.lng().toFixed(6).replace(".", ","));

        marker.setPosition(evt.latLng);

        getDireccion(marker);
    });





    document.getElementById("btnBuscarDireccion").addEventListener("click", () => {
        geocodeAddress(geocoder, map);
    });


}


function geocodeAddress(geocoder, resultsMap)
{
    //const address = document.getElementById("address").value;
    const address = $("#txtDomicilio").val();

    geocoder.geocode({ address: address }, (results, status) =>
    {
        if (status === "OK") {

            marker.setPosition(results[0].geometry.location);
            map.setCenter(marker.getPosition());

            $("#latitudActual").val(marker.getPosition().lat().toFixed(6));
            $("#longitudActual").val(marker.getPosition().lng().toFixed(6));
            $("#latitud").val(marker.getPosition().lat().toFixed(6).replace(".", ","));
            $("#longitud").val(marker.getPosition().lng().toFixed(6).replace(".", ","));

        } else {

            alert("Geocode was not successful for the following reason: " + status);
        }
    });
}

function getDireccion(marker)
{
    var geocoder = new google.maps.Geocoder();

    geocoder.geocode({ location: marker.getPosition() }, (results, status) => {
        if (status === "OK")
        {

            $("#dirActual").val(results[0].formatted_address);
            
        }
    });
}