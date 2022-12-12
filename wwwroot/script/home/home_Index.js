

function recuperarDatos(urlAction) {

    var dato = localStorage.getItem('datoUsuario');

    if (dato != null && dato != "" && dato != undefined)
    { //comprobamos que nuestra variable exista
        var datosObj = JSON.parse(dato);

        alert("Se esta inciando la session, espere unos segundos");

        $.post({
            url: urlAction,

            type: "Post",

            data: { Correo: datosObj.correo, Clave: datosObj.clave },

            success: function (result) {

                if (result.success)
                {
                    window.location.reload();
                }
                else
                {
                    //Significa que hubo error!
                }

            }
        });
    }
}