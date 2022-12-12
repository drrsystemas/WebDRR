

function change_cbPais(url) {

    //Aca tengo que disparar una funcion para recargar el modelo de
    var paisId = $("#cbPais").val();

    $.ajax({
        url: url,
        data: {paisId: paisId},
        type: "Post",
        success: function (result) {

            var listaProvincias = JSON.parse(result);

            var cbProvincia = document.getElementById("cbProvincia");

            var cantidadElementos = listaProvincias.length;

            if (cantidadElementos > 0) {

                if (cbProvincia.hasAttribute("hidden")) {
                    cbProvincia.removeAttribute("hidden")
                }
                else {
                    //hay que borrar los elementos con value 

                    for (var i = 0, cantidad = cbProvincia.options.length; i < cantidad; i++) {
                        opt = cbProvincia.options[i];

                        if (opt === undefined) {
                            break;
                        }

                        var number = Number(opt.value);

                        if (Number.isInteger(number) === true) {
                            cbProvincia.removeChild(opt);
                            i--;
                        }
                    }


                    //--Para que si cambia el combo de pais y estaba mostrando ciudades se borren.
                    var cbCiudad = document.getElementById("cbCiudad");

                    for (var i = 0, cantidad = cbCiudad.options.length; i < cantidad; i++) {
                        opt = cbCiudad.options[i];

                        if (opt === undefined) {
                            break;
                        }

                        var number = Number(opt.value);

                        if (Number.isInteger(number) === true) {
                            cbCiudad.removeChild(opt);
                            i--;
                        }
                    }

                }
                //$("#cbProvincia").removeAttr("hidden");
            }


            for (var i = 0; i < cantidadElementos; i++) {
                var item = listaProvincias[i];

                var option = document.createElement("option");
                option.innerHTML = item.DescripcionProvinciaEstado;
                option.value = item.EstadoProvinciaId;

                cbProvincia.appendChild(option);
            }
        }
    });




}

function change_cbProvincia(url) {

    var provinciaId = $("#cbProvincia").val();

    $.ajax({
        url: url,
        data: { provinciaId: provinciaId },
        type: "Post",
        success: function (result) {

            var listaCiudades = JSON.parse(result);

            var cbCiudad = document.getElementById("cbCiudad");

            var cantidadElementos = listaCiudades.length;

            if (cantidadElementos > 0) {

                if (cbCiudad.hasAttribute("hidden")) {
                    $("#cbCiudad").removeAttr("hidden");
                }
                else {
                    //hay que borrar los elementos con value 

                    for (var i = 0, cantidad = cbCiudad.options.length; i < cantidad; i++) {
                        opt = cbCiudad.options[i];

                        if (opt === undefined) {
                            break;
                        }

                        var number = Number(opt.value);

                        if (Number.isInteger(number) === true) {
                            cbCiudad.removeChild(opt);
                            i--;
                        }
                    }
                }
               
            }
    

            for (var i = 0; i < cantidadElementos; i++) {
                var item = listaCiudades[i];

                var option = document.createElement("option");
                option.innerHTML = item.DescripcionCiudad;
                option.value = item.CiudadId;

                cbCiudad.appendChild(option);
            }
        }
    });

}

function filtrarBusquedaCiudad(url) {

    var ciudadId = $("#cbCiudad").val();

    if (ciudadId > 0) {
        var urlParametro = url + "?ciudadId=" + ciudadId;
        window.location.href = urlParametro;
    }
    else {
        Alert("Seleccione 1 ciudad para poder ejecutar el filtro");
    }


}