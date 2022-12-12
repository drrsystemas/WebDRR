var map;
var markers = [];
var origen;
var destino;
var directionsService;
var directionsDisplay;


function iniciarMapa() {

    var lat = parseFloat("-26.402501");
    var lon = parseFloat("-54.588052");
    //Posicion inicial-
    var drrsystemas = { lat: lat, lng: lon };

    //Instancion el mapa
    map = new google.maps.Map(
        document.getElementById('map'), {
            zoom: 14, center: drrsystemas,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    });




    directionsService = new google.maps.DirectionsService();
    directionsRenderer = new google.maps.DirectionsRenderer();


    //Obtengo el item seleccionado del combo de sucursales.
    var option = $("#cbSucursal option:selected");
    var direccionSucursal = option.data("dirmapa");

    var geocoder = new google.maps.Geocoder();

    geocoder.geocode({ 'address': direccionSucursal }, function (results, status) {

        if (status == google.maps.GeocoderStatus.OK) {
            var latitude = results[0].geometry.location.lat();
            var longitude = results[0].geometry.location.lng();
            var direccion = results[0].formatted_address;

            origen = { lat: latitude, lng: longitude };

            ////genero el primer punto.
            //var marker = new google.maps.Marker({
            //    position: origen,
            //    map: map,
            //    draggable: true,
            //    title: direccionSucursal
            //});


            ////Nuevo
            //var infowindow = new google.maps.InfoWindow({
            //    content: direccionSucursal+" Origen",
            //});

            ////infowindow.open(map, marker);
            //marker.addListener("click", () => {
            //    infowindow.open(map, marker);

            //    //seleccionarCombo(marker);
            //});
            ////Fin

            //map.setCenter(marker.position);
            //marker.setMap(map);

            //markers.push(marker);

        }

    });


    var direccionCliente = $("#txtDestinoDireccion").val();
    var geocoderDestino = new google.maps.Geocoder();
    geocoderDestino.geocode({ 'address': direccionCliente }, function (results, status) {

        if (status == google.maps.GeocoderStatus.OK) {

            var latitude = results[0].geometry.location.lat();
            var longitude = results[0].geometry.location.lng();
            var direccionDestino = results[0].formatted_address;

            destino = { lat: latitude, lng: longitude };




            ////genero el primer punto.
            //var markerDestino = new google.maps.Marker({
            //    position: destino,
            //    map: map,
            //    draggable: true,
            //    title: direccionDestino
            //});


            ////Nuevo
            //var infowindowDestino = new google.maps.InfoWindow({
            //    content: direccionDestino + " Destino",
            //});

            ////infowindow.open(map, marker);
            //markerDestino.addListener("click", () => {
            //    infowindowDestino.open(map, markerDestino);

            //    //seleccionarCombo(marker);
            //});
            ////Fin

            //map.setCenter(markerDestino.position);
            //markerDestino.setMap(map);

            //markers.push(markerDestino);


            map.setCenter(origen);
            comoLlegar();
        }

    });



    directionsRenderer.setMap(map);
    directionsRenderer.setPanel(document.getElementById("indicators"));
}


function comoLlegar() {

    directionsService.route(
        {
            origin: origen,
            destination: destino,
            travelMode: google.maps.TravelMode.DRIVING,
        },
        (response, status) => {
            if (status === "OK")
            {
                alert('La distancia total del viaje es: ' + (response.routes[0].legs[0].distance.value / 1000).toFixed(2) + ' km');
                directionsRenderer.setDirections(response);
            } else
            {
                window.alert("Directions request failed due to " + status);
            }
        }
    );
}

function calculateRoute() {

    directionsService = new google.maps.DirectionsService();

    directionsService.route({ origin: origen, destination: destino,  travelMode: google.maps.TravelMode.DRIVING, }, (response, status) =>
    {
            if (status === google.maps.DirectionsStatus.OK)
            {
            this.directionsDisplay.setDirections(response);
        } else {
            alert('Could not display directions due to: ' + status);
        }
    });
}


function getPosicion_Direccion(dato) {

    var geocoder = new google.maps.Geocoder();

    var address = dato;

    geocoder.geocode({ 'address': address }, function (results, status)
    {
        if (status == google.maps.GeocoderStatus.OK) {
            var latitude = results[0].geometry.location.lat();
            var longitude = results[0].geometry.location.lng();
        } 

    });

    return [latitude, longitude];
}


