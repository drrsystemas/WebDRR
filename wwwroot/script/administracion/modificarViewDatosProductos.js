let _valorRadioInactivo = 0;

$(document).ready(function()
{
    if (_valorRadioInactivo === 0) {
        $('#radioInactivo').attr('checked', true);
    }
    else {
        $('#radioActivo').attr('checked', true);
    }
});


$('#frmModificar').on('submit', function (e) {
    e.preventDefault();

    let data = $("#txtRojo").val() + "|" +
        $("#txtAmarillo").val() + "|" +
        $("#txtVerde").val();
    $("#txtExtraDos").val(data);


    let dataExtra = $("#txtDesdeSemaforo").val() + "|" +
        $("#txtHastaSemaforo").val();

        $("#txtExtraSemaforo").val(dataExtra);
    
    

        this.submit();
    
});


function enviarFrm() {

    let frm = $('#frmModificar');

    let impCod = $("#txt-codigo");
    let impTip = $("#txt-tipo");
            //    <input type="hidden" asp-for="Codigo" class="form-control" />
            //<input type="hidden" name="tipo" value="3" class="form-control" />
    frm.cli(impCod);
    frm.appendChild(impTip);
    frm.submit();
}

function abrirFrmClasificaciones() {
    //document.getElementById('ventana-formulario-clasificaciones').style.display = 'block';
    $("#ventana-formulario-clasificaciones").modal("show");
}

function cerrarFrmClasificaciones() {
//    document.getElementById('ventana-formulario-clasificaciones').style.display = 'none';
    $("#ventana-formulario-clasificaciones").modal("hide");
}

function FiltrarClasificaciones_lista() {
    var input, filter, table, items, enlace, i, visible;


    input = document.getElementById("txtFiltroClasificacion");



    filter = input.value.toUpperCase();

    table = document.getElementById("listaTablaClasificacion");
    items = table.getElementsByTagName("li");

    for (i = 0; i < items.length; i++) {
        visible = false;
        var elemento = items[i];
        var enlace = elemento.children[0];


        var nombreFamilia = enlace.innerText.toUpperCase();

        if ((nombreFamilia !== "") && nombreFamilia.indexOf(filter) > -1) {
            visible = true;
        }

        if (visible === true) {
            elemento.style.display = "";
        }
        else {
            elemento.style.display = "none";
        }
    }
}



//$(function () {
//    agregarQuitarClasificacion();

//    function agregarQuitarClasificacion(tipoOperacion, id) {

//        cerrarFrmClasificaciones();
//        $("#frmModalClasificaciones").html("");

//        $.ajax({
//            dataType: "/Video/LoadVideosViewComponent/",
//            url: getUri + "?name=Leia",
//            success: function (cust) {
//                $("#ajaxCust").append(cust);
//            }
//        })
//    }
//})



function agregarQuitarClasificacion(tipoOperacion, id) {




    cerrarFrmClasificaciones();

    $('.modal').remove();
    $('.modal-backdrop').remove();
    $('body').removeClass("modal-open");

    $("#frmModalClasificaciones").html("");

    let parametros = {
        "tipoOperacion": tipoOperacion,
        "id": id
    }

    //$.get("/Administracion/CallViewComponentClasificacion", function (parametros) {
    //    $("#frmModalClasificaciones").html(response);
    //});

    $.ajax({
        method: "post",
        url: "/Administracion/CallViewComponentClasificacion_Dos",
        data: parametros,
        dataType: "html",
        success: function (response) {

           let data = JSON.parse(response)

            $("#frmModalClasificaciones").html(data.componente);
            $("#txt-lista-clasificaciones").val(data.valores);

        },
        failure: function (response) {
        },
        error: function (response) {

        }
    });
}
