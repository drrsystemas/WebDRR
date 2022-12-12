var seCargaronDatos = false;

$(document).ready(function () {
    $("#cbPago").trigger('change');
});




function Changed_MetodosDePago()
{

    $("#txtNombreIdPago").val($("#cbPago option:selected").text());

    var codigo = parseInt($("#cbPago option:selected").val());


    $("#btnSiguiente").show();
    $("#divTransferencia").removeClass('mostrarDiv').addClass('ocultarDiv');
    $("#divTarjetas").removeClass('mostrarDiv').addClass('ocultarDiv');
    $("#imgFormaPago").attr("src", "");



    if (codigo === 1)
    {
        $("#imgFormaPago").attr("src", "/img/pago/coordinaCliente.png");
    }
    else if (codigo === 2)
    {
        //src="~/img/pago/transferenciaBancaria.png" 
        $("#imgFormaPago").attr("src", "/img/pago/transferenciaBancaria.png");

       var bco =  $("#txtBco").val();
        var cbu = $("#txtCbu").val();
        var alias = $("#txtAlias").val();

        if (cbu === "" || cbu === null) {
            $("#p_txtBco").text('Los datos para realizar la transferencia se las entregara el encargado/a de ventas');
        }
        else {

            $("#p_txtBco").text('Banco: ' + bco);
            $("#p_txtCbu").text('CBU: ' + cbu);
            $("#p_txtAlias").text('Alias: ' + alias);
        }

        $("#divTransferencia").removeClass('ocultarDiv').addClass('mostrarDiv');
    }
    else if (codigo === 3) {
        $("#imgFormaPago").attr("src", "/img/pago/mercadoPago.png");

    }
    else if (codigo === 5)
    {
        $("#imgFormaPago").attr("src", "/img/pago/tarjetas.png");
        $("#divTarjetas").removeClass('ocultarDiv').addClass('mostrarDiv');
    }
}



function mostrarFormularioDatosTrajetas() {


    var urlAction = $("#divTarjetas").data("action");
    window.location = urlAction;

    //var parametros = {
    //    "dato": dato,
    //    "rol": rol
    //}

    //$.ajax({
    //    type: "POST",
    //    url: urlAction,
    //    //data: parametros,
    //    dataType: "html",
    //    success: function (response) {

    //        $("#divTarjetas").html(response);
    //    },
    //    failure: function (response) {
    //        alert(response.responseText);
    //    },
    //    error: function (response) {
    //        alert(response.responseText);
    //    }
    //});
}