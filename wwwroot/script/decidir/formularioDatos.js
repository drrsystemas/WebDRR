
const txtNumero = document.getElementById("txtNumeroTarjeta");



function limpiarErrores() {
    var listaSpan = jQuery("span");

    for (i = 0; i < listaSpan.length; i++) {
        var span = listaSpan[i];
        while (span.hasChildNodes()) {
            span.removeChild(span.lastChild);
        }
    }

}


$(document).ready(function () {

    limpiarErrores();

    txtNumero.addEventListener('focusout', (event) => {

        var numero = $(event.target).val();

        if (!(numero === undefined || numero === null))
        {
            if (numero.length >= 6) {
                datosBin(numero.substring(0,6));
            }
            
        }
        
    });

});





function datosBin(bin) {

    var dato = bin;
    var urlAction = $("#txtNumeroTarjeta").data("urlaction");

    var parametros = {
        "bin": dato
    }

    $.ajax({
        type: "POST",
        url: urlAction,
        data: parametros,
        dataType: "json",
        success: function (response) {
           
            document.getElementById("helpNumeroTarjeta").innerHTML = response;
        },
        failure: function (response)
        {
            alert(response.responseText);
        },
        error: function (response) {
            alert(response.responseText);
        }
    });
}


function enviarFormularioDatosTarjeta() {
    var frm = document.getElementById("formularioDatosTarjeta");
    $(frm).submit();
}



