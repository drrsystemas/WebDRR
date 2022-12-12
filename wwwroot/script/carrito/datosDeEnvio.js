//#region Variables Globales
var map;
var markers = [];
var makerPosicionActual;
var dir;
var Posiciondrrsystemas;
//#endregion


//#region MAPA -- Google

function iniciarMapa(direcciones)
{

    var lat = -26.402501;
    var lon = -54.588052;

    lat = parseFloat(lat);
    lon = parseFloat(lon);

    //Posicion inicial-
    Posiciondrrsystemas = { lat: lat, lng: lon };

    //Instancion el mapa
    map = new google.maps.Map(
        document.getElementById('map'), {
            zoom: 18, center: Posiciondrrsystemas,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    });

    dir = direcciones;

    ubicacionesSucursales(direcciones);

}


function ubicacionesSucursales(direcciones) {

    var geocoder = new google.maps.Geocoder();

    if (direcciones === undefined) {
        direcciones = dir;
    }

    var direc = direcciones.split("_");

    var cantidad = direc.length;

    direc.forEach(element =>
        geocoder.geocode({ address: element }, (results, status) => {
            if (status === "OK") {

                //genero el primer punto.
               var marker = new google.maps.Marker({
                   position: results[0].geometry.location,
                   map: map,
                   draggable: false,
                   title: element
                });


                //Nuevo
                var infowindow = new google.maps.InfoWindow({
                    content: element,
                });

                //infowindow.open(map, marker);
                marker.addListener("click", () => {
                    infowindow.open(map, marker);

                    seleccionarCombo(marker);
                    //Caundo se selecciona 1 marker se hacerca el zoom del mapa-
                    map.setZoom(18);
                });
                //Fin

                map.setCenter(marker.position);
                marker.setMap(map);

                markers.push(marker);

            }
        }
        ));


    //Limpiar si es que tienen algo los campos de envio.
    $("#txtProvincia").val("");
    $("#txtLocalidad").val("");
    $("#txtCodigoPostal").val("");
    $("#txtDireccion").val("");
    $("#txtInfoAdicional").val("");
    
}


function showLocation(position) {

    //Verificar que no este el psocion-
    if (makerPosicionActual !== undefined) {
        makerPosicionActual.setMap(null);
    }

    var latitude = position.coords.latitude;
    var longitude = position.coords.longitude;
    var pos = { lat: latitude, lng: longitude };
    //genero el primer punto.

    makerPosicionActual = new google.maps.Marker({
        position: pos, map: map,
        draggable: true,
        title: "Mi ubicacción",
        icon: {
            url: "http://maps.google.com/mapfiles/kml/pal4/icon21.png"
        }
    });


    //google.maps.event.addListener(makerPosicionActual, 'dragend', seMovio());
    google.maps.event.addListener(makerPosicionActual, 'dragend', function (evt) {
       
        datosPosicion(evt.latLng);

    });

    datosPosicion(makerPosicionActual.getPosition());
}


//Siendo posicion la lat y log
function datosPosicion(posicion) {

    var geocoder = new google.maps.Geocoder();
    geocoder.geocode({ location: posicion }, (results, status) => {
        if (status === "OK") {
            if (results[0]) {

                var resultado = results[0];

                var prov = "";
                var loc = "";
                var codPostal = "";
                var direccion = "";
                var numeroDireccion = "";

                var cantidad = resultado.address_components.length;

                for (var i = 0; i < cantidad; i++) {

                    var item = resultado.address_components[i];

                    if (item.types[0] === "postal_code") {
                        codPostal = item.long_name;
                    }
                    else if (item.types[0] === "administrative_area_level_1") {
                        prov = item.long_name;
                    }
                    else if (item.types[0] === "locality") {
                        loc = item.long_name;
                    }
                    else if (item.types[0] === "route") {
                        direccion = item.long_name;
                    }
                    else if (item.types[0] === "street_number") {
                        numeroDireccion = item.long_name;
                    }
                }

                $("#txtProvincia").val(prov);
                $("#txtLocalidad").val(loc);
                $("#txtCodigoPostal").val(codPostal);
                $("#txtDireccion").val(direccion + " "+numeroDireccion);


                infowindow = new google.maps.InfoWindow({
                    content: results[0].formatted_address,
                });

                infowindow.open(map, makerPosicionActual);

                map.setCenter(makerPosicionActual.position);
                makerPosicionActual.setMap(map);

            } else {
                window.alert("No hay resultados");
            }
        } else {
            window.alert("Geolocalización fallo : " + status);

        }
    });
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
    } else {
        alert("Su navegador no soporta la funcione de geolocalización!");


        makerPosicionActual = new google.maps.Marker({
            position: Posiciondrrsystemas, map: map,
            draggable: true,
            title: "Posicion inicial",
            icon: {
                url: "http://maps.google.com/mapfiles/kml/pal4/icon21.png"
            }
        });

        map.setCenter(makerPosicionActual.position);
        makerPosicionActual.setMap(map);
    }
}


//Elimina las ubicaciones -- y la localizacion actual.
function EliminarMakers() {
    //borra las ubicaciones
    for (var i = 0; i < markers.length; i++) {
        markers[i].setMap(null);
    }
    markers = [];

    //Verificar que no este el psocion-
    if (makerPosicionActual !== undefined) {
        makerPosicionActual.setMap(null);
    }
};

//#endregion


//#region Funciones cuando cambia un combo o se selecciona un marker.


function seleccionarCombo(marker) {
    //Obtengo el idSucursal que tiene el marker
    var titulo = marker.get('title');

    //var elementoSelect = $("#cbSucursalRetiro");

    $("#cbSucursalRetiro").find('option').each(function () {
        var opcion = $(this);
        var dirGm = opcion.data("dirmapa");

        if (titulo === dirGm) {
            opcion.attr("selected", true);
        }
    });


    //var option = $("#cbSucursalRetiro option:selected");

    //var nombre = option.text();
    //var valor = option.val();

    //var tel = option.data("tel");
    //var cor = option.data("cor");
    //var dir = option.data("dir");

    //$("#pTel").text(tel);
    //$("#pCor").text(cor);
    //$("#pDir").text(dir);
    //$("#txtNombreIdSucursal").val(nombre);
    //$("#txtIdSucursal").val(valor);
}


function seleccionarMarker(nombre) {
    var resultado = markers.find(mk => mk.get('title') === nombre);
    if (resultado !== undefined) {
        google.maps.event.trigger(resultado, 'click');
    }
}

//#endregion


//#region Combos de pantalla.

////Cuando cambia el tipo de envio se muestran u ocultan los divs y obtengo el nombre
function cambiaTipoEnvio() {

    var selectValue = $("#cbTipoEnvio").val();

   $("#txtNombreIdTipoEnvio").val($("#cbTipoEnvio option:selected").text());

    switch (selectValue) {

        case "1":
            $("#divRetiro").show();
            $("#divEnvio").hide();

            if (makerPosicionActual !== undefined) {
                makerPosicionActual.setMap(null);
            }

            ubicacionesSucursales();

            break;

        case "2":
            $("#divRetiro").hide();
            $("#divEnvio").show();

            EliminarMakers();

            getLocation();

            break;
    }

}


////Cuando cambia el envio se obtiene el costo del mismo y obtengo el nombre
function cambiaEnvio() {

    $("#txtNombreIdEnvio").val($("#cbEnvio option:selected").text());

    var valorEnvio = $("#cbEnvio option:selected").data('valor');

    if (parseFloat(valorEnvio) < 0) {
        $("#txtCostoEnvio").val(0);
        $("#txtCostoEnvioAviso").show();
        $("#txtCostoEnvioAviso").html("El costo de envio se coordina con el cliente");
    }
    else {
        $("#txtCostoEnvio").val(valorEnvio);
        $("#txtCostoEnvioAviso").hide();
        $("#txtCostoEnvioAviso").html("");
    }

}



////Cuando cambia la sucursal obtengo el nombre
function cambiaSucursalRetiro() {

    $("#divDatosSucursal").removeClass("ocultarDiv");

    var option = $("#cbSucursalRetiro option:selected");

    var nombre = option.text();
    var valor = option.val();

    var tel = option.data("tel");
    var cor = option.data("cor");
    var dir = option.data("dir");
    var dirGm = option.data("dirmapa");

    $("#pTel").text(tel);
    $("#pCor").text(cor);
    $("#pDir").text(dir);

    $("#txtNombreIdSucursal").val(nombre);
    $("#txtIdSucursal").val(valor);

    //Selecciona el maker al hacer click
    seleccionarMarker(dirGm);
}

//#endregion
