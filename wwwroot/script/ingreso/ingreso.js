window.onload = function () {
    document.getElementById("frmIngreso").addEventListener('submit', antesDeEnviarFormulario);

    verificarLS();
    
};


function verificarLS() {
    if (typeof (localStorage) !== "undefined") {
        //alert("localStorage actvado.");
    } else {
        alert("El navegaro web no soporta almacenamiento local.");
    }
}


function antesDeEnviarFormulario()
{
    var recordarDatos = document.getElementById('checkRecordarDatos').checked;

    if (recordarDatos == true) {
        //alert("Se van a recordar los datos");

        var correo = document.getElementById('txtCorreo').value;
        var clave = document.getElementById('txtClave').value;

        var miObjeto = { 'correo': correo, 'clave': clave};

        // Guardo el objeto como un string
        localStorage.setItem('datoUsuario', JSON.stringify(miObjeto));

    }

}