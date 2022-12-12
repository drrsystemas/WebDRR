

$(document).ready(function ()
{
    //Carga la pagina transformo el esquema de moneda con . sino se complica.
     var importe = $('#importe').val();
    $("#importe").val(importe.replace(",", "."));


    $.validator.addMethod("formatoMoneda", function (value, element, param) {
        return validateDecimal(value);
    });


    var min = $('#minutos').val();
    var imp = $('#importe').val();





    let fechaAsignado = document.getElementById("fechaAsignado").value;

    if (fechaAsignado.length > 15) {
        fechaAsignado = fechaAsignado.substring(0, 16);
        $('#fechaAsignado').val(fechaAsignado);
    }


    let fechaInicio = document.getElementById("fechaInicio").value;

    if (fechaInicio.length > 15) {
        fechaInicio = fechaInicio.substring(0, 16);
        $('#fechaInicio').val(fechaInicio);
    }


    let fechaCompletado = document.getElementById("fechaCompletado").value;

    if (fechaCompletado.length > 15) {
        fechaCompletado = fechaCompletado.substring(0, 16);
        $('#fechaCompletado').val(fechaCompletado);
    }


    calcularImporteTotal(min, imp);
});



function calcularImporteTotal(minutos, importe) {
    if (esVacio(minutos) === false && esVacio(importe) === false) {

        //var importe = importe.replace(",", ".");

        var resultado = parseFloat((minutos * importe) / 60).toFixed(2);

        $('#totalNeto').val(resultado);
    }

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


function calcular(tipoOperacion) {
    
    var tecnico = $("#tecnico").val();
    var codigoProducto = $("#codigoId").val();
    var fechaInicio = $('#fechaInicio').val();
    var fechaCompletado = $('#fechaCompletado').val();
    var importe = $('#importe').val().replace(".", ",");
    var minutos = $('#minutos').val();
    var total = $('#totalNeto').val().replace(".",",")
    //var controlFechaInicio = esVacio(fechaInicio);

  /*  if (controlFechaInicio === false) {*/

        var urlAction = "/Soporte/AjaxPostServicioTecnicoCalculo";

        $.ajax({
            type: "POST",
            url: urlAction,
            data: {
                TecnicoId: tecnico,
                CodigoId: codigoProducto,
                FechaInicio: fechaInicio,
                FechaFin: fechaCompletado,
                Importe: importe,
                Minutos: minutos,
                TipoCambio: tipoOperacion,
                Total: total
            },
            dataType: "text",
            success: function (response) {

                if (response != null) {
                    var obj = JSON.parse(response);

                    $('#importe').val(obj.importe);
                    $('#minutos').val(obj.minutos);
                    $('#totalNeto').val(obj.total);
                    $('#fechaInicio').val(obj.fechaInicio);
                    $('#fechaCompletado').val(obj.fechaFin);

                    $("#EtapaId").val(30);

                    $("#error").val(obj.error);
                }
                else {
                    alert("------");
                }
            },
            failure: function (response) {
                alert(response.responseText);
            },
            error: function (response) {
                alert(response.responseText);
            }
        });
    //}





}




function serviciosTecnico() {

    var tecnico = $("#tecnico").val();

    if (esVacio(tecnico) === false && parseInt(tecnico) > 0) {

        var urlAction = "/Soporte/AjaxPostServicioTecnico";

        $.ajax({
            type: "POST",
            url: urlAction,
            data: {
                tecnicoId: tecnico
            },
            dataType: "text",
            success: function (response) {

                if (response != null) {

                   
                  
                    var obj = JSON.parse(response);

                    if (esVacio(obj) === false) {


                        $("#codigoId").children().remove();

                        for (i = 0; i < obj.length; i++) {
                            var item = obj[i];

                            $("#codigoId").append("<option value=" + item.codigoId + ">" + item.descripcionAdicional+"</option>");

                        }
                    }
                }
                else {
                    alert("------");
                }
            },
            failure: function (response) {
                alert(response.responseText);
            },
            error: function (response) {
                alert(response.responseText);
            }
        });


    }
}



function validateDecimal(valor) {
    var RE = /^\d*\.?\d*$/;
    if (RE.test(valor)) {
        return true;
    } else {
        return false;
    }
}


$("#frmAgregarEditarTarea").validate({
    rules: {
        DescripcionResolucion: {
            required: {
                depends: function (element) {
                    var etapa = parseInt($("#EtapaId").val());
                    var respuesta = false;
                    if (etapa === 30) {
                        respuesta = true;
                    }
                    return respuesta;
                }
            }
        },
        FechaHoraInicio: {
            required: {
                depends: function (element) {
                    var etapa = parseInt($("#EtapaId").val());
                    var respuesta = false;
                    if (etapa === 30) {
                        respuesta = true;
                    }
                    return respuesta;
                }
            }

        },
        FechaHoraCompletado: {
            required: {
                depends: function (element) {
                    var etapa = parseInt($("#EtapaId").val());
                    var respuesta = false;
                    if (etapa === 30) {
                        respuesta = true;
                    }
                    return respuesta;
                }
            }

        },
        ImporteNeto: {
            formatoMoneda: true
        },
        TecnicoSoporteId: {
            required: true
        },
        CodigoId: {
            required: true
        },
        TotalNeto: {
            formatoMoneda: true
        },
        DescripcionTarea: {
            maxlength: 100
        }
    },
    messages: {

        DescripcionResolucion: {
            required: "Si la etapa se completo, realice una breve descripcion de lo realizado. "
        },
        FechaHoraInicio: "Si la etapa se marca como finalizada se tiene que cargar la fecha en que se incia la tarea.",
        FechaHoraCompletado: "Si la etapa se marca como finalizada se tiene que cargar la fecha en que la tarea fue completada",
        ImporteNeto: "El formato de un valor monetario es 55.34, el separador de decimales es el '.' punto.",
        TecnicoSoporteId: "Seleccione el recurso humano encargado de realizar la tarea.",
        CodigoId: "Seleccione el Servicio a realizar.",
        ImporteNeto: "El formato es incorrecto el separador de decimales es el '.' ",
        DescripcionTarea: "La descripcion de la tarea no puede tener mas de 100 caracteres.",
    },
    errorElement: 'span'
});








function enviarFormulario()
{
    // var importe = $('#importe').val();
    //$("#importe").val(importe.replace(".", ","));

    $("#frmAgregarEditarTarea").submit();

    //var valor = $("#frmTipoOperacion").val();

    //var tipoOperacion = parseInt(valor);

    //var contacto = $("#contactoId").val();
    //var cliente = $("#clienteId").val();

    //if (tipoOperacion === 1) {
    //    if (cliente === "0" || contacto === "0") {
    //        $("#errorContactoCliente").text("Se necesita seleccionar el contacto en caso de no existir se tiene que agregar en la base de datos.");

    //        moverseA("errorContactoCliente");
    //    }
    //    else {
    //        $("#frmAgregarCaso").submit();
    //    }
    //}
    //else {
    //    $("#frmAgregarCaso").submit();
    //}
       
}