$(document).ready(function () {

  
});




function aceptarPedido()
{
    if ($("#txtCelular").val() == "") {
        alert("El Teléfono/Celular no puede esta vacio");
    }
    else {

        $("#btnAceptar").hide();


        var urlAction = $("#btnAceptar").data("action");
        finalizarCompra(urlAction);
    }
}



function finalizarCompra(urlBase) {
    var observacion = $("#txtObservacion").val();
    var celular = $("#txtCelular").val();
    var codigoPostal = $("#txtcodigopostal").val();
    var direccion = $("#txtdireccion").val();

    var dateObj = new Date();
    var dateFormat = GetDateFormat(dateObj);
    dateFormat += " " + checkTime(dateObj.getHours()) + ":" + checkTime(dateObj.getMinutes());

    var url = urlBase + "?obs=" + observacion + "&cel=" + celular + "&cp=" + codigoPostal + "&dir=" + direccion + "&dte=" + dateFormat;

    window.location.href = url;
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