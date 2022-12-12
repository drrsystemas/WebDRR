
//$(document).ready(
//    function () {

//        fotorama.resize({
//            width: auto,
//            height: 50
//        });

//    });


function onEventoCambiaImg(fotorama, lista) {

    var indice = fotorama.activeIndex;

    var titulo = lista[indice];

    alert("Cambio de img." + titulo);
}


function verProducto(url)
{
    window.location = url;
}




function frmConsulta(url) {

    var fw = document.getElementById("ventanaFormularioWp");

    if (fw === null) {
        $.ajax({
            url: url,
            type: "Post",
            success: function (result) {



                var frm = document.getElementById("frm-consulta-reclamo");
                frm.innerHTML = result;

                $("#ventanaFormularioWp").modal("show");
            }
        });
    }
    else {
        $("#ventanaFormularioWp").modal("show");
    }

}



function enviarWhatsApp(urlWp, msj) {

    if (msj === "") {
        let cliente = document.getElementById("pw_name").value;
        let mensaje = document.getElementById("pw_mensaje").value;

        msj = "Consulta/Reclamo.%0A";
        msj += "Cliente: " + cliente + "%0A";
        msj += "Mensaje: " + mensaje + "%0A";
    }


    let wp = urlWp + "/?text=" + msj;
    //alert(wp);
    let win = window.open(wp, '_blank');
    win.focus();
}

function abrirWhatsApp(elemento) {

    //const l = elemento.parentNode.parentNode;
    //let lurlWp = l.dataset.url;
    //let lnumWp = l.dataset.numero;

    const link = document.getElementById('wpEmpresaContacto');
    let urlWp = link.dataset.url;
    let numWp = link.dataset.numero;

    let wp = urlWp + "/"+numWp;
    //alert(wp);
    let win = window.open(wp, '_blank');
    win.focus();
}



