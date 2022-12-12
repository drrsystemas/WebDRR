let _jsonData;
let _urlCargarProd;
let _urlImagenesProd;
let _listaProductosRelacionados;
let _prodBase;
let _prod
let _urlObservacion;


$(document).ready(function ()
{
    
    cargarImagenes(_prodBase, _prod,  _urlImagenesProd);

    cargarProductoRelacionados(_jsonData, _urlCargarProd);

    verObservacion(_urlObservacion, _prodBase);
});





/**
 * Me trae la informacion de los producto relacionados.
 * @param {any} jsonListaIds
 * @param {any} urlProdRel
 */
function cargarProductoRelacionados(jsonListaIds, urlProdRel) {

    //if (jsonListaIds !== "[]") {
    //    $.ajax({
    //        url: urlProdRel, // Url
    //        data: { json: jsonListaIds },
    //        type: "post",
    //        success: function (result) {

    //            if (result !== "") {
    //                let array = jsonListaIds.split(",");
    //                let cantidad = array.length;
    //                document.getElementById("divProdctosRelacionados").innerHTML = result;
    //                iniciarCarousel(cantidad);
    //            }

    //        }
    //    });
    //}
}



function cargarImagenes(prodbase, prod, url) {

    if (prodbase !== "") {
        $.ajax({
            url: url, // Url
            data: { productoBase: prodbase, productoId: prod },
            type: "post",
            success: function (result) {
                if (result !== "")
                {
                    //elimino el div de cargando.
                    document.getElementById("cargando").remove();

                    document.getElementById("galeriaImagenes").innerHTML = result;

                    // iniciamos fotorama de forma manual.
                    var $fotoramaDiv = $('#fotorama').fotorama();

                    //// 2. Get the API object.
                    //var fotorama = $fotoramaDiv.data('fotorama');

                    //// 3. Inspect it in console.
                    //console.log(fotorama);

                }
            }
        });
    }
}


function verImagenesProducto(url, id)
{
  
    $.ajax({
        url: url, // Url
        data: { idProductoBase: id },
        type: "post",
        success: function (result)
        {
            
            var res = result;
            var img1 = res[0];


            var hola = "ddd";

        }
     });
}




function generarEnlace(url, id) {
    $.ajax({
        url: url, // Url
        data: { id: id},
    type: "post",
        success: function (result) {

    //$('#datosEnlace').show();
    //$('#datosEnlace').html(result);
    ////$('#datosEnlace').hide();

    //        var codigoACopiar = document.getElementById("datosEnlace");

    //var seleccion = document.createRange();
    //seleccion.selectNodeContents(codigoACopiar);
    //window.getSelection().removeAllRanges();
    //window.getSelection().addRange(seleccion);
    //var res = document.execCommand('copy');
    //window.getSelection().removeRange(seleccion);

            var copyTextarea = document.createElement("textarea");
            copyTextarea.style.position = "fixed";
            copyTextarea.style.opacity = "0";

            copyTextarea.textContent = result;

            document.body.appendChild(copyTextarea);
            copyTextarea.select();
            document.execCommand("copy");
            document.body.removeChild(copyTextarea);



}
            });
         }



function wpProducto(urlWp, codigo, nombre) {

    var wp = urlWp + "/?text=Consultar sobre: [" + codigo + "] " + nombre;
    //alert(wp);
    var win = window.open(wp, '_blank');
    win.focus();
}



function verObservacion(url, id) {
    $.ajax({
        url: url, // Url
        data: { idProductoBase: id },
        type: "post",
        success: function (result)
        {

            var divObservacion = document.getElementById("txtObservacion");
            divObservacion.innerHTML = result;
        }
    });
}


function CargarImagenes() {
    $(".post-Carga").each(function () {

        var img = $(this).attr("data-src");
        $(this).attr('src', img);

        $(this).fadeIn();

    })
}









function iniciarCarousel(cantidad) {

    if (cantidad === 1) {
        $('.owl-carousel').owlCarousel({
            loop: true,
            margin: 10,
            nav: true,
            responsive: {
                0: {
                    items: 1
                },
                600: {
                    items: 1
                },
                1000: {
                    items: 1
                }
            }
        });

    }
    else if (cantidad === 2) {

        $('.owl-carousel').owlCarousel({
            loop: true,
            margin: 10,
            nav: true,
            responsive: {
                0: {
                    items: 1
                },
                600: {
                    items: 2
                },
                1000: {
                    items: 2
                }
            }
        });

    }
    else if (cantidad >= 3)
    {
        $('.owl-carousel').owlCarousel({
            loop: true,
            margin: 10,
            nav: true,
            responsive: {
                0: {
                    items: 1
                },
                600: {
                    items: 2
                },
                1000: {
                    items: 3
                }
            }
        });
    }





}