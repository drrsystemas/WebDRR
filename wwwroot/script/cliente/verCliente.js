let EditarPermisos;


function activarControles(permisos) {
    if (permisos === "True") {
        $("#txtDomicilio").removeAttr("readonly");
        $("#txtTelefono").removeAttr("readonly");
        $("#txtCorreo").removeAttr("readonly");
        $("#txtDatosEntrega").removeAttr("readonly");

        $("#btnGuardar").removeClass("ocultarDiv");
    }
}

function postAction(url) {
    let form = $('<form></form>');

    form.attr("method", "post");
    form.attr("action", url);

    let input = document.createElement("INPUT");
    input.type = 'text';
    input.name = 'idCliente';
    input.value = "@Model.ClienteID";

    let dom = $("#txtDomicilio");
    let tel = $("#txtTelefono");
    let cor = $("#txtCorreo");
    let dat = $("#txtDatosEntrega");

    form.append(input);
    form.append(dom);
    form.append(tel);
    form.append(cor);
    form.append(dat);

    $(document.body).append(form);
    form.submit();
}
