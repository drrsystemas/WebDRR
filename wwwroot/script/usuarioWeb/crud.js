let tipo;
const togglePassword = document.querySelector('#togglePassword');
const password = document.querySelector('#id_password');


$(window).on("load", function() {
    
    
    analizarTipoOperacion(tipo);


    if (parseInt(tipo) === 1) {
        setTimeout(limpiarAsignaciones, 1000);
    }

});


$("#frmbusquedaAjax").keypress(function (e) {
    if (e.which == 13) {
        return false;
        buscarDatos();
    }
});



togglePassword.addEventListener('click', function (e) {
    // toggle the type attribute
    const type = password.getAttribute('type') === 'password' ? 'text' : 'password';
    password.setAttribute('type', type);
    // toggle the eye slash icon
    this.classList.toggle('fa-eye-slash');
});

function buscarDatos() {

    var rol = $("#tipoUsuario").val();
    var dato = $("#inputDatoBusqueda").val();
    var urlAction = $("#inputDatoBusqueda").data("urlaction");

    var parametros = {
        "dato": dato,
        "rol": rol
    }

    $.ajax({
        type: "POST",
        url: urlAction,
        data: parametros,
        dataType: "html",
        success: function (response) {

            $("#divDatos").html(response);
        },
        failure: function (response) {
            alert(response.responseText);
        },
        error: function (response) {
            alert(response.responseText);
        }
    });
}


function limpiarAsignaciones() {

    //document.getElementById('email').value = "";
    //document.getElementById('pass').value = "";

    $("#email").val("");
    $("#id_password").defaultvalue("");
}


