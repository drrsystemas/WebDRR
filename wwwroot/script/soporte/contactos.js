




$(document).keyup(function (event) {
    var code = event.key;

    if (code === "Enter") {
        if ($('#tablaContactos >tbody >tr').length == 1) {
            var enlace = $("#btn_1").attr('href');
            window.location = enlace;
        }
    }
});



$(document).ready(function () {

    $.validator.addMethod("telformato", function (value, element, param)
    {
        //const r = /[0-9]{3}-[0-9]{4}[-][0-9]{5,}/g;
        //return r.test(value);
        return validarTelefono(value);
    });

    document.getElementById("inputBuscarContacto").focus();

});

$("#formcontacto").validate({
    rules: {
        ApellidoNombre: {
            required: true,
            maxlength:50
        },
        Telefono: {
            required: true,
            telformato: true
        },
        EntidadSucId: {
            required: true,
            min:1
        }
    },
    messages: {

        ApellidoNombre: {
            required: "Por favor, ingresé el Apellido y Nombre del Contacto",
            maxlength: "El campo el muy grande, maximo 50 caracteres"
        },
        Telefono: "Por favor, Ingrese el número teléfonico, el formato valido son 3n-4n-5n o + n*",
        EntidadSucId: "El cliente no se selecciono, seleccione 1 cliente para agregarle el contacto."
    }
    ,
    errorElement: 'span'
});

function validarTelefono(telefono) {
    const r = /[0-9]{3}-[0-9]{4}[-][0-9]{5,}/g;
    return r.test(telefono);
}



$(function () {
    $('[data-toggle="popover"]').popover()
})

$("#Telefono").on({
    "focus": function (event) {
        $(event.target).focus();
    },
    "keyup": function (event) {
        $(event.target).val(function (index, value) {

            var tecla = event.key;
            var cantidadCaracteres = value.length;

            if (cantidadCaracteres === 4)
            {
                if (tecla !== "Backspace") {
                    value = value.substring(0, cantidadCaracteres - 1);
                    value = value + "-" + tecla;
                }
                else {
                    value = value.substring(0, cantidadCaracteres - 1);
                    value = value + "-";
                }

            }
            else if (cantidadCaracteres == 9) {
                if (tecla !== "Backspace") {
                    value = value.substring(0, cantidadCaracteres - 1);
                    value = value + "-" + tecla;
                }
                else {
                    value = value.substring(0, cantidadCaracteres - 1);
                    value = value + "-";
                }
            }
            else {

                //Solamante va a funcionar 1 ves
                if (tecla === "Enter") {
                    //Verifica el data-pegar
                    //elem.getAttribute(nombre) – obtiene el valor.
                    //    elem.setAttribute(nombre, valor) – establece el valor.

                    var valor = document.getElementById("Telefono").getAttribute('data-pegar');

                    if (valor == '0') {
                        document.getElementById("Telefono").setAttribute('data-pegar', '1');
                        value = value.replace(/[-+()\s]/g, '');

                        if (cantidadCaracteres > 3) {
                            value = (value).slice(0, 3) + "-" + (value).slice(3);
                            cantidadCaracteres += 1;
                        }

                        if (cantidadCaracteres > 8) {
                            value = (value).slice(0, 8) + "-" + (value).slice(8);
                        }                        
                    }

                }
            }

            //if()

            return value;
        });
    }

});






function filtarContactos() {
    // Declare variables 
    var input, filter, cuerpoTabla, tr, td, i, j, visible;

    input = document.getElementById("inputBuscarContacto");

    filter = input.value.toUpperCase();

    cuerpoTabla = document.getElementById("tablaContactosCuerpo");

    tr = cuerpoTabla.getElementsByTagName("tr");

    // Loop through all table rows, and hide those who don't match the search query
    for (i = 0; i < tr.length; i++) {
        visible = false;
        /* Obtenemos todas las celdas de la fila, no sólo la primera */
        td = tr[i].getElementsByTagName("td");
        for (j = 0; j < td.length; j++) {
            if (td[j] && td[j].innerHTML.toUpperCase().indexOf(filter) > -1) {
                visible = true;
            }
        }
        if (visible === true) {
            tr[i].style.display = "";
        } else {
            tr[i].style.display = "none";
        }
    }
}