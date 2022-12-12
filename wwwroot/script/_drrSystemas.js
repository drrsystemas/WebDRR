

function empty(val) {
    if (val === undefined)
        return true;

    if (typeof (val) === 'function' || typeof (val) === 'number' || typeof (val) === 'boolean' || Object.prototype.toString.call(val) === '[object Date]')
        return false;

    if (val == null || val.length === 0)        // null or 0 length array
        return true;

    if (typeof (val) === "object") {
        // empty object

        var r = true;

        for (var f in val)
            r = false;

        return r;
    }

    return false;
}


function isNullEmpty(e) {
    switch (e) {
        case "":
        case 0:
        case "0":
        case null:
        case false:
        case typeof (e) == "undefined":
            return true;
        default:
            return false;
    }
}



function formatoMoneda_SinSignoMoneda(valor) {

    //var numero = parseFloat(valor);
    //if (numero === 0) {
    //    valor = "0";
    //}

    var negativo = valor.includes('-');

    valor =  valor.replace(/\D/g, "")
        .replace(/([0-9])([0-9]{2})$/, '$1,$2')
        .replace(/\B(?=(\d{3})+(?!\d)\.?)/g, ".");


    if (negativo === true) {
        valor = "-" + valor;
    }

    return valor;
}