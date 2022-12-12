

function seleccionarRadioActInac(valor) {

    if (valor === 0) {
        $('#radioInactivo').attr('checked', true);
    }
    else {
        $('#radioActivo').attr('checked', true);
    }
}
