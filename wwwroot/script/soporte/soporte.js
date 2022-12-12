////window.onbeforeunload = function (e) {
////    //console.log("Se va a ir para Atras.");
////    let url = document.getElementById("urlVolver").getAttribute("href");

////    if (url === "") {
////        url = history.go(-1);
////    }

////    document.location = url;
////};


$("#frmbusquedaAjax").keypress(function (e) {
    if (e.which == 13)
    {
        e.preventDefault();
        buscarDatos();
    }
});

function abrirBuscarCliente() {

    $("#modalBuscarCliente").modal("show");
    $("#tituloModalCliente").text("Seleccionar Cliente ");
}


function buscarDatos() {

    var rol = 2;
    var dato = $("#inputDatoBusqueda").val();
    var urlAction = $("#inputDatoBusqueda").data("urlaction");

    var parametros = {
        "dato": dato,
        "rol": rol
    }

    $.ajax({
        type: "POST",
        url: urlAction,
        data: parametros,
        dataType: "html",
        success: function (response) {

            $("#divDatos").html(response);
        },
        failure: function (response) {
            alert(response.responseText);
        },
        error: function (response) {
            alert(response.responseText);
        }
    });
}


function seleccionarRegistro(id, nombre, numero, correo, idClienteVendedor) {

    $("#modalBuscarCliente").modal("hide");

    $("#datoFiltroCliente").removeAttr("readonly");
    $("#datoFiltroCliente").val(idClienteVendedor + " - " + nombre + " - " + numero);
    $("#datoFiltroCliente").attr("readonly", "readonly");

    $("#txtIdCliente").val(idClienteVendedor);

}


function quitarDatosCliente() {


    $("#datoFiltroCliente").removeAttr("readonly");
    $("#datoFiltroCliente").val("");
    $("#datoFiltroCliente").attr("readonly", "readonly");

    $("#txtIdCliente").val("");

}


$(document).ready(function ()
{



});


/*
 Soporte/Soporte/GuardarSoporteCaso
 */
function buscarClientes() {

    $("#funcionalidad").val("2");

    $("#frmAgregarCaso").validate().settings.ignore = "*";

    $("#frmAgregarCaso").submit();
}

function buscarContactos() {

    $("#funcionalidad").val("3");

    $("#frmAgregarCaso").validate().settings.ignore = "*";

    $("#frmAgregarCaso").submit();
}


function es_null_indefinido(dato) {
    var tipo = typeof dato;

    if (tipo === 'undefined' || a === null) {
        return true;
    }

    return false;
}


/**
 * Enviar el formulario para lo que es guardar.
 * @param {any} modoGuardar
 * @param {any} item
 */
function enviarFormulario(modoGuardar, item)
{
    $("#btnUno").prop('disabled', true);
    $("#btnDos").prop('disabled', true);



    $("#modoGuardar").val(modoGuardar);

    //frmTipoOperacion Indica el Tipo de Operacion.
    var valor = $("#frmTipoOperacion").val();

    var tipoOperacion = parseInt(valor);

    var contacto = $("#contactoId").val();
    var cliente = $("#clienteId").val();

    if (tipoOperacion === 1) {
        if (cliente === "0" || contacto === "0")
        {

            if (cliente === "0") {
                $("#errorContactoCliente").text("Se necesita seleccionar el CLIENTE en caso de no existir se tiene que agregar en la base de datos.");

            }
            else if (contacto == "0") {
                $("#errorContactoCliente").text("Se necesita seleccionar el CONTACTO en caso de no existir se tiene que agregar en la base de datos.");
            }
            else if (contacto == "0" && cliente === "0") {
                $("#errorContactoCliente").text("Se necesita seleccionar el CLIENTE y CONTACTO en caso de no existir se tiene que agregar en la base de datos.");
            }


            moverseA("errorContactoCliente");

            $("#btnUno").prop('disabled', false);
            $("#btnDos").prop('disabled', false);
        }
        else {
            $("#frmAgregarCaso").submit();

            $("#btnUno").prop('disabled', false);
            $("#btnDos").prop('disabled', false);
        }
    }
    else {
        $("#frmAgregarCaso").submit();

        $("#btnUno").prop('disabled', false);
        $("#btnDos").prop('disabled', false);
    }
       
}


$("#frmAgregarCaso").validate({
    rules: {
        DescripcionProblema: {
            required: true
        },
        EmailNotificacion: {
            email: true
        },
        TecnicoSoporteId: {
            required: {
                depends: function (element) {
                    var tipoOperacion = parseInt($("#frmTipoOperacion").val());
                    var respuesta = true;
                    if (tipoOperacion !== 1) {
                        respuesta = false;
                    }
                    return respuesta;
                }
            }
            
        }
    },
    messages: {

        DescripcionProblema: {
            required: "Se requiere un descripción del problema."
        },
        EmailNotificacion: "El correo electronico ingresado no es valido.",
        TecnicoSoporteId: "Seleccione la persona que realizara la tarea."

    },
    errorElement: 'span'
});



function moverseA(idDelElemento) {
    location.hash = "#" + idDelElemento;
}



function pintar(objeto, col) {
    objeto.style.backgroundColor = col;
}




function verImagenTarea(parrafo) {

    var data = parrafo.getAttribute('data-imagen');
    var imagenModal = document.getElementById("imgPantallaModal");
    imagenModal.setAttribute("src", data);

    $("#modalVerImagenes").modal("show");
}



function esVacio(e) {
    switch (e) {
        case "":
        case 0:
        case "0":
        case null:
        case false:
        case typeof (e) == "undefined":
            return true;
        default:
            return false;
    }
}


function buscarClientes_Filtro(dato,url)
{
    if (esVacio(dato) === false) {
        url += "?codigo=" + dato;
    }

    window.location = url;

}


$("#txtCodigo_BuscarCcontacto").keyup(function (e) {
    var code = e.key; 
    if (code === "Enter") {
        e.preventDefault();
        buscarContactos();
    }

});





