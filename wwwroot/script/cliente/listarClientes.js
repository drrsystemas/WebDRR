


//window.onbeforeunload = function (e)
//{
//    //console.log("Se va a ir para Atras.");
//    let url = document.getElementById("urlVolver").getAttribute("href");

//    if (url === "")
//    {
//        url = history.go(-1);
//    }

//    document.location = url;
//};

$(document).ready(function ()
{
    quitarClasesBtn();
});


function quitarClasesBtn() {

    let ancho = $(window).width();
    if (ancho <= 550) {
            $("th").removeClass("colBtn");
        }
        else {
            $("#tbBtnFicha").addClass("colBtn");
            $("#tbBtnSeleccion").addClass("colBtn");
        }
    
}



$(window).resize(function () {
    let ancho = $(window).width();

    if (ancho <= 550) {
        $("th").removeClass("colBtn");
    }
    else {
        $("#tbBtnFicha").addClass("colBtn");
        $("#tbBtnSeleccion").addClass("colBtn");
    }
});






function verEstadosDeCuentas(url, json)
{

    if (empty(json) === false) {

        let dv = document.getElementById("div-spinner");
       
        $(dv).removeClass("ocultarDiv");

        $.ajax({
            url: url, // Url
            data: {
                jsonListaClientes: json
            },
            type: "post"  // Verbo HTTP
        })
            // Se ejecuta si todo fue bien.
            .done(function (result) {
                if (result != null) {

                    document.getElementById("divTablaCliente").innerHTML = "";
                    document.getElementById("divTablaCliente").innerHTML = result;

                    $(dv).addClass("ocultarDiv");
                }
                else {
                }
            })
            // Se ejecuta si se produjo un error.
            .fail(function (xhr, status, error) {
                $(dv).addClass("ocultarDiv");
            })
            // Hacer algo siempre, haya sido exitosa o no.
            .always(function () {
                $(dv).addClass("ocultarDiv");
            });
    }


}