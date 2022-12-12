//$("#pass").rules("add", {
//    required: true,
//    minlength: 6,
//    messages: {
//        required: "Ingrese la clave para la cuenta",
//        minlength: jQuery.validator.format("Ingrese una clave con al menos {0} caracteres")
//    }
//});

//$("#email").rules("add", {
//    required: true,
//    email: true,
//    messages: {
//        required: "Ingrese la cuenta de correo del usuario",
//        email: "El formato del correo no es valido"
//    }
//});

//$("#frmCrubUsuarioWeb").validate({
//    rules: {
//        clave: {
//            required: true,
//            minlength: 6
//        },
//        correo: {
//            email: true,
//            required: true
//        }
//    },
//    messages: {

//        clave: {
//            required: "Ingrese la clave de acceso para la cuenta"
//        },
//        correo: "El correo electronico ingresado no es valido."

//    },
//    errorElement: 'span'
//});




function analizarTipoOperacion(tipoOperacion)
{
    if (parseInt(tipoOperacion) === 4) {
        $('#frmCrubUsuarioWeb :input').attr('readonly', 'readonly');
    }
}




function enviarFromularioCrub() {
    //Se tendria que disparar la validacion del frm y despues enviar.

    var valPass = $("#pass").validate();
    var valCorreo = $("#email").validate();

    $("#frmCrubUsuarioWeb").submit();
}


function seleccionarEntidadSuc() {

    $("#inputDatoBusqueda").val("");
    $("#divDatos").text("");


    $(".modal").modal("show");

    var rol = $("#tipoUsuario").val();

    if (rol === "2") {
        $("#tituloModal").text("Seleccionar Cliente");
    }
    else if (rol === "4") {
        $("#tituloModal").text("Seleccionar Vendedor");
    }
    else if (rol === "8") {
        $("#tituloModal").text("Seleccionar Cliente Fidelizado");
    }
    else if (rol === "128") {
        $("#tituloModal").text("Seleccionar Administrador");
    }
    else {

        document.getElementById("msjError").innerHTML="Seleccione 1 rol, para poder realizar la seleccion."

    }
}




function seleccionarRegistro(id, nombre, numero, correo,idClienteVendedor)
{
    //EntidadSucID.
    $("#txtentidadSucId").val = id;
    
    $(".modal").modal("hide");

    $("#entidadSucId").val(id + " - " + nombre + " - " + numero);

    $("#idClienteVendedor").val(idClienteVendedor);

    if (correo !== "") {
        $("#email").val(correo);
    }
    
}


function verificarClientePorDni(url) {

    $("#giro").addClass("preloader");

    var dato = $("#numeroDni").val();

    var parametros = {
        "dni": dato
    }


    $.ajax({
        type: "POST",
        url: url,
        data: parametros,
        success: function (response) {

            $("#txtAyN").val(response.apellidoyNombre);
            $("#txtTel").val(response.celular);
            $("#txtCodP").val(response.codigoPostal);
            $("#txtLoc").val(response.localidad);
            $("#txtDir").val(response.direccion);

            var partesFecha = response.referencias.split('/');

            var mes = parseInt(partesFecha[1]); //obteniendo mes
            var dia = parseInt(partesFecha[0]); //obteniendo dia
            var ano = parseInt(partesFecha[2]); //obteniendo año
            if (dia < 10)
                dia = '0' + dia; //agrega cero si el menor de 10
            if (mes < 10)
                mes = '0' + mes //agrega cero si el menor de 10
            document.getElementById('txtFN').value = ano + "-" + mes + "-" + dia;

            $("#giro").removeClass("preloader");
        },
        failure: function (response) {
            alert(response.responseText);
        },
        error: function (response) {
            alert(response.responseText);
        }
    });
}









function getComboRoles(selectObject) {

    let value = parseInt(selectObject.value);

    let elemento = $("#div-seleccionar-usurio");

    if (value !== 2) {

        $(elemento).removeClass("ocultarDiv");
    }
    else {

        $(elemento).addClass("ocultarDiv");
    }
}
