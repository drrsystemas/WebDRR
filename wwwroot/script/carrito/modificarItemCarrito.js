var productoGenerico = false;
var _viewControlStock = 0;
var _viewOcultar;

$(document).ready(function () {

 
    productoGenerico = $('#inputEsGenerico').val();

    if (_viewOcultar == "True") {
        document.getElementById("txtCantidad").readOnly = true;
    }


    if (productoGenerico === "False")
    {
        var precioN = $('#txtPrecioNeto').text();
        precioN = precioN.replace('.', '');
        precioN = precioN.replace(',', '.');

        var precioB = $('#txtPrecioBruto').text();
        precioB = precioB.replace('.', '');
        precioB = precioB.replace(',', '.');

        
        var cantidad = document.getElementById("txtCantidad").value;
        cantidad = cantidad.replace(',', '.');
        document.getElementById("txtCantidad").value = cantidad;

        $('#txtPrecioNeto').text(precioN);
        $('#txtPrecioBruto').text(precioB);
       

    }
    else {
        
        var precioN = $('#inputPrecioNeto').val();
        precioN = precioN.replace('.', '');
        precioN = precioN.replace(',', '.');

        var cantidad = document.getElementById("txtCantidad").value;
        cantidad = cantidad.replace(',', '.');
        document.getElementById("txtCantidad").value = cantidad;

        $('#inputPrecioNeto').val(precioN);
    }

    recalcularSubtotal();

    document.getElementById("txtCantidad").focus();
});




function cantidadesBtn(tipo) {
    var numero = document.getElementById("txtCantidad").value;

    if (tipo === 1) {
        numero = Number( numero) + 1;
       
        document.getElementById("txtCantidad").value = numero;
    }
    else {

        if (numero > 1) {
            numero = Number(numero) - 1;

            document.getElementById("txtCantidad").value = numero;        }
    }

    recalcularSubtotal();
}





function recalcularSubtotal() {

    if (productoGenerico === "False")
    {
        var numero = document.getElementById("txtCantidad").value;

        numero = Number(numero);

        var precioB = $('#txtPrecioBruto').text();

        precioB = Number(precioB.replace('$', ''));

        var bonf = Number($('#txtBonificacion').val());

        var stotal = precioB * numero;

        if (bonf > 0) {
            let stotalbonificada = stotal - ((bonf * stotal) / 100);


            document.getElementById("lbSubtotal").innerHTML = "Subtotal $" + stotal.toFixed(2);

            document.getElementById("lbSubtotalBonificado").innerHTML = "Subtotal Bonf. $" + stotalbonificada.toFixed(2);

            let ahorro = stotal - stotalbonificada;
            document.getElementById("lbAhorro").innerHTML = "Ahorro $" + ahorro.toFixed(2);

            $("#lbSubtotalBonificado").show();
            $("#lbAhorro").show();

        }
        else {
            document.getElementById("lbSubtotal").innerHTML = "Subtotal $" + stotal.toFixed(2);

            document.getElementById("lbSubtotalBonificado").innerHTML = "";
            document.getElementById("lbAhorro").innerHTML = "";

            $("#lbSubtotalBonificado").hide();
            $("#lbAhorro").hide();
        }

        
    }
    else
    {
        var precioN = Number($('#inputPrecioNeto').val());

        var numero = document.getElementById("txtCantidad").value;

        numero = Number(numero);

        var bonf = Number($('#txtBonificacion').val());

        var stotal = precioN * numero;

        if (bonf > 0)
        {
            stotal = stotal - ((bonf * stotal) / 100);
        }

        document.getElementById("lbSubtotal").innerHTML = "Subtotal Neto $" + stotal.toFixed(2);
    }

}


function cambiaPresentacion()
{

    $("#cambialaPresentacion").val("SI");

    if (productoGenerico == "True") {
        $('#inputPrecioNeto').val($('#inputPrecioNeto').val().replace('.', ','))
    }

    document.getElementById("txtCantidad").value = document.getElementById("txtCantidad").value.replace('.', ',');

    $("#frmModificar").submit();
}



function enviarFormulario(frm) {

    if (parseInt(frm) === 1)
    {
        if (productoGenerico == "True") {
            $('#inputPrecioNeto').val($('#inputPrecioNeto').val().replace('.', ','))
        }

        document.getElementById("txtCantidad").value = document.getElementById("txtCantidad").value.replace('.', ',');


        $("#frmModificar").submit();
    }
    else {
        $("#frmEliminar").submit();
    }
}


$("#txtCantidad").change(function () {
    recalcularSubtotal();
});


$("#txtBonificacion").change(function () {
    recalcularSubtotal();
});

$("#txtBonificacion").on("click", function () {
    $(this).select();
});

$("#txtBonificacion").focus(function () {
    $(this).select();
});

$("#txtBonificacion").focusin(function () {
    $(this).select();
});