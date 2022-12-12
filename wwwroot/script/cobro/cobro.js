function abrirPantallaCobros(){
    $("#frm-cobros").modal("show");
}


function agregarCobros(url, parametro){
    $("#frm-cobros").modal("hide");
    url += "?esquema=" + parametro;

    window.location.href = url;
}


function viewFrmAsignarNumeroReparto(){
    $("#frm-asignar").html("");

    $.ajax({
        method: "get",
        url: "/Cobro/AsignarNumeroRepartoCobrador",
        dataType: "html",
        success: function (response) {
            $("#frm-asignar").html(response);
            $("#frm-venta-numero-reparto-cobrador").modal("show");
        },
            failure: function (response) {
        },
            error: function (response) {
        }
    });
}


function frmCerrarNumeroReparto() {
    $("#frm-venta-numero-reparto-cobrador").modal("hide");
}


function frmGuardarNumeroReparto(url) {

    frmCerrarNumeroReparto();

    var parametros = {
        "Numero": document.getElementById("txt-numero-reparto").value
    };

    $.ajax({
        method: "post",
        url: url,
        data: parametros,
        dataType: "html",
        success: function (response) {
            $("#frm-asignar").html(response);
        },
        failure: function (response) {
        },
        error: function (response) {
        }
    });
}


function enviar()

{
    $("#frm-asignar-numerito").submit();
}