

function seleccionarSector() {

    var urlAction = $("#btn-seleccionar-sector").data("url");


        $.ajax({
            type: "POST",
            url: urlAction,
            dataType: "html",
            success: function (response) {

                document.getElementById("div-inyeccion-sector").innerHTML = response;

                $("#ventana-formulario-sectores").modal('show');
            },
            failure: function (response) {
            },
            error: function (response) {
            }
        });
}