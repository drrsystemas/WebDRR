var urlRetornoJS;
var urlCantidadJsonJS;
var urlBonificacionJsonJS;



$('#tablaProductos').on('keypress', ':input', function (event) {
    var keycode = (event.keyCode ? event.keyCode : event.which);
    if (keycode == '13') {
        var value = $(this).val(); //obtiene el valor actual del input.
        var name = $(this).prop('name');

        if (name === "cantidad") {

            var idProducto = $(this).attr('data-id');   //.dataset.idProducto;
            var idItem = $(this).attr('data-idItem');
            var cantidad = $(this).val();
            var url = urlRetornoJS;

            $.ajax({
                url: urlCantidadJsonJS, // Url
                data: {
                    idProducto: idProducto,
                    idItemCarrito: idItem,
                    cantidad: cantidad,
                    urlRetorno: url
                },
                type: "post",
                success: function () {
                    location.reload();
                }
            });


        }
        else if (name === "bonificacion") {
            var idProducto = $(this).attr('data-id');   //.dataset.idProducto;
            var idItem = $(this).attr('data-idItem');
            var bonificacion = $(this).val();
            var url = "@Model.UrlRetorno";


            $.ajax({
                url: urlBonificacionJsonJS, // Url
                data: {
                    idProducto: idProducto,
                    idItemCarrito: idItem,
                    bonificacion: bonificacion,
                    urlRetorno: url
                },
                type: "post",// Verbo HTTP
                dataType: 'json',
                success: function () {
                    location.reload();
                }
            });
        }
    }
});


$('#tablaProductos').on('focusout', ':input', function (event) {

    var value = $(this).val(); //obtiene el valor actual del input.
    var name = $(this).prop('name');

    if (name === "cantidad") {

        var idProducto = $(this).attr('data-id');   //.dataset.idProducto;
        var idItem = $(this).attr('data-idItem');
        var cantidad = $(this).val();
        var url = urlRetornoJS;

        $.ajax({
            url: urlCantidadJsonJS, // Url
            data: {
                idProducto: idProducto,
                idItemCarrito: idItem,
                cantidad: cantidad,
                urlRetorno: url
            },
            type: "post",
            success: function () {
                location.reload();
            }
        });


    }
    else if (name === "bonificacion") {
        var idProducto = $(this).attr('data-id');   //.dataset.idProducto;
        var idItem = $(this).attr('data-idItem');
        var bonificacion = $(this).val();
        var url = "@Model.UrlRetorno";


        $.ajax({
            url: urlBonificacionJsonJS, // Url
            data: {
                idProducto: idProducto,
                idItemCarrito: idItem,
                bonificacion: bonificacion,
                urlRetorno: url
            },
            type: "post",// Verbo HTTP
            dataType: 'json',
            success: function () {
                location.reload();
            }
        });
    }
});






function seleccionarTransporte(url) {

    var id = $("#cbTransporte").val();
    $.ajax({
        url: url, // Url
        data: {
            idTransporte: id
        },
        type: "post",// Verbo HTTP
        dataType: 'json',
        success: function () {
        }
    });

}


function abrirPantallaVentanaBonificacion() {
    $('#ventanaBonificacion').modal('show');
}


function abrirFormularioWp() {
    $('#ventanaFormularioWp').modal('show');
}

function activarDesactivarBtnComprar(cantidad) {
    var itemCarrito = cantidad;

    if (itemCarrito > 0) {
        $("#btnComprar").removeClass("disabled");
    }
    else {
        $("#btnComprar").addClass("disabled")
    }

}


function aplicarBonificacion(action)
{
    $('#ventanaBonificacion').modal('hide');

    $.post(action, { bonificacion: $("#ventanaBonificacionInput").val()}, function (data) {
        location.reload();
    });

}


function abrirPantallaVentanaDescuento() {
    $('#ventanaDescuento').modal('show');
}


function aplicarDescuento(action) {
    $('#ventanaDescuento').modal('hide');

    $.post(action, { descuento: $("#ventanaDescuentoInput").val() }, function (data) {
        location.reload();
    });

}



/**
 * Esta funcion js se actiba en modalidad carrito de compras cuando el usurio finaliza la compra del carrito.
 * -Dependiendo de la configuracion del portal se redireccionara a la pagina prevista.
 * @param {any} url - es la url a donde redirecciona
 * @param {any} datos_envio - indica si el envio/retiro esta activado.
 * @param {any} datos_pago - indica si el pago esta activado.
 * modificada: 04/10/2021
 */
function comprar(url, datos_envio, datos_pago)
{

    //var enlace = "@Url.Action("DatosDeEnvio", "Carrito")";
     var enlace = url;
    $('#vConfirmacionEnlace').attr('href', enlace);

    var titulo = "Completar datos de la Compra";
    $('#vConfirmacionTitulo').text(titulo)

     var msj = "Ahora tiene que configurar en Envio/Retiro y la forma de Pago. ¿Quiere continuar?";

    if (datos_envio === "False" && datos_pago === "False") {
        msj = "Los datos de Envio/Retiro y la forma de Pago se coordinan con un vendedor. ¿Quiere continuar?";
    }
    else if (datos_envio === "True" && datos_pago === "False") {
        var msj = "Ahora tiene que configurar en Envio/Retiro y la forma de Pago se coordina con un vendedor. ¿Quiere continuar?";
    }
    else if (datos_envio === "False" && datos_pago === "True") {
        var msj = "Ahora tiene que configurar la forma de Pago y el envio/retiro se coordina con un vendedor. ¿Quiere continuar?";

    }

     $('#vConfirmacionContenido').text(msj)


    $('#ventanaConfirmacion').modal('show');
}


function registrarPedido(url)
{

    var urlBase = url;

    var dateObj = new Date();
    var dateFormat = GetDateFormat(dateObj); // 07/05/2016
    dateFormat += " " + checkTime(dateObj.getHours()) + ":" + checkTime(dateObj.getMinutes());

    var enlace = urlBase + "?&dte=" + dateFormat;

    $('#vConfirmacionEnlace').attr('href', enlace);

    var titulo = "Guardar Pedido";
    $('#vConfirmacionTitulo').text(titulo)

    var msj = "¿Esta seguro que quiere guardar el pedido?";
    $('#vConfirmacionContenido').text(msj)


    $('#ventanaConfirmacion').modal('show');
}


function modificarPedido(url) {

    var urlBase = url;

    var dateObj = new Date();
    var dateFormat = GetDateFormat(dateObj); // 07/05/2016
    dateFormat += " " + checkTime(dateObj.getHours()) + ":" + checkTime(dateObj.getMinutes());

    var enlace = urlBase + "?&dte=" + dateFormat;

    $('#vConfirmacionEnlace').attr('href', enlace);

    var titulo = "Modificar Pedido";
    $('#vConfirmacionTitulo').text(titulo)

    var msj = "¿Esta seguro que quiere modificar el pedido?";
    $('#vConfirmacionContenido').text(msj)


    $('#ventanaConfirmacion').modal('show');
}


function cancelarPedido (url) {

    var enlace = url;
    $('#vConfirmacionEnlace').attr('href', enlace);

    var titulo = "Cancelar Pedido";
    $('#vConfirmacionTitulo').text(titulo)

    var msj = "¿Esta seguro que quiere cancelar el pedido?";
    $('#vConfirmacionContenido').text(msj)


    $('#ventanaConfirmacion').modal('show');
}



function GetDateFormat(date) {
    var month = (date.getMonth() + 1).toString();
    month = month.length > 1 ? month : '0' + month;
    var day = date.getDate().toString();
    day = day.length > 1 ? day : '0' + day;
    return day + '/' + month + '/' + date.getFullYear();
}


function checkTime(i) {
    return (i < 10) ? "0" + i : i;
}


function pedidoWeb(urlWp, pedido) {

    var wp = urlWp + "/?text=" + pedido;
    //alert(wp);
    var win = window.open(wp, '_blank');
    win.focus();
}



$("#observacion").focusout(function ()
{
    //$("#frmObservacion").submit();


    var data = $("#observacion").val();
    var urlAction = $("#observacion").data("urlaction");

    $.ajax({
        url: urlAction, // Url
        data: {
            observacion: data
        },
        type: "post",// Verbo HTTP
        dataType: 'json',
        success: function () {

        }
    });



});




$('#vConfirmacionEnlace').on('click', function (event) {
    $('#ventanaConfirmacion').modal('hide');
});




function frmConsulta() {
    $("#ventanaFormularioWp").modal("show");
    $("#pw_name").focus();
}

function pedidoWebFormualrio_Enviar()
{
    var camposRellenados = false;

    myElements = document.querySelectorAll('#frmEnviarWp span.text-danger span');

    if (myElements.length > 0) {
        for (var i = 0; i < myElements.length; i++) {
            let item = myElements[i];
            if (item.innerText.length > 0) {
                camposRellenados = true;
                break;
            }
        }
    }


    if (camposRellenados == true) {
        $("#frmEnviarWpValidation").text("Hay errores en el formulario, los campos "
            +"no son obligatorios pero el formato en el documento y teléfono tienen que ser correcto.");
    }
    else {


        $("#ventanaFormularioWp").modal("hide");


        let datosCliente = "-----------Datos Cliente-----------%0A";

        let hayUnDato = false;

        let nya = $("#pw_name").val();
        $("#pw_name").val("");
        let dni = $("#pw_documento").val();
        $("#pw_documento").val("");
        let tel = $("#pw_telefono").val();
        $("#pw_telefono").val("");
        let dir = $("#pw_domicilio").val();
        $("#pw_domicilio").val("");
        let msj = $("#pw_mensaje").val();
        $("#pw_mensaje").val("");

        if (!isNullEmpty(nya)) {
            datosCliente += "NyA: " + nya + "%0A";
            hayUnDato = true;
        }

        if (!isNullEmpty(dni)) {
            datosCliente += "Doc: " + dni + "%0A";
            hayUnDato = true;
        }

        if (!isNullEmpty(tel)) {
            datosCliente += "Tel: <<" + tel + ">> %0A";
            hayUnDato = true;
        }

        if (!isNullEmpty(dir)) {
            datosCliente += "Dir: " + dir + "%0A";
            hayUnDato = true;
        }

        if (!isNullEmpty(msj)) {
            datosCliente += "Msj: " + msj + "%0A";
            hayUnDato = true;
        }

        datosCliente += "-----------------------------------%0A"

        let url = $("#pw_url").val();
        let pedido = $("#pw_pedido").val();
        let wp = "";

        if (hayUnDato == true) {
            wp = url + "/?text=" + datosCliente + pedido;
        }
        else {
            wp = url + "/?text=" + pedido;
        }
        
        ////alert(wp);
        var win = window.open(wp, '_blank');
        win.focus();
    }


}




function enviarWhatsApp(urlWp, msj) {

    if (msj === "") {
        let cliente = document.getElementById("pw_name").value;
        let mensaje = document.getElementById("pw_mensaje").value;

        msj = "Consulta/Reclamo.%0A";
        msj += "Cliente: " + cliente + "%0A";
        msj += "Mensaje: " + mensaje + "%0A";
    }


    let wp = urlWp + "/?text=" + msj;
    //alert(wp);
    let win = window.open(wp, '_blank');
    win.focus();
}




function cambiarCategoriaCliente() {

    var urlAction = $("#link-cambiar-categoria-cliente").data("urlAction");

    $.ajax({
        url: urlAction, // Url
        type: "post",// Verbo HTTP
        dataType: 'json',
        success: function (response) {
            if (response === "true") {

            }
        }
    });

}






function guardarCarritoLocalStorage(url, urlTemporal)
{
    //$.ajax({
    //    url: url,
    //    type: "post",
    //    dataType: 'json',
    //    success: function (response) {

    //        let dato = JSON.stringify(response);
    //        window.localStorage.setItem("MiCarrito", dato);

    //        window.location.href = urlTemporal;

    //    }
    //});

    $("#btnGuardadCarrito").hide();
    window.location.href = urlTemporal;
}


function recuperarCarritoLocalStorage(url) {

   let micarrito =  window.localStorage.getItem("MiCarrito");

    $.ajax({
        url: url,
        type: "post",
        data: {
            micarrito: micarrito
        },
        dataType: 'json',
        success: function (response) {


            location.reload();


        }
    });
}

function eliminarCarritoLocalStorage() {
    window.localStorage.removeItem("MiCarrito");
}