
function frmAbrirEntregarReparto(elemento, url) {

    var fw = document.getElementById("ventanaFrmEntregarReparto");

    var datos = {
        id: $(elemento).data("id"),
        detalle: $(elemento).data("detalle")
    };

    if (fw === null) {
        $.ajax({
            url: url,
            data:datos,
            type: "get",
            success: function (result) {



                var frm = document.getElementById("frm-entregar");
                frm.innerHTML = result;

                $("#ventanaFrmEntregarReparto").modal("show");
            }
        });
    }
    else {
        $("#ventanaFrmEntregarReparto").modal("show");
    }

}



function frmGuardarEntregarReparto(url) {

    $("#ventanaFrmEntregarReparto").modal("hide");


    var datos = {
        id: $("#frmEntregarReparto_btnAceptar").data("id"),
        fecha: $("#pw_fecha").val(),
        detalle: $("#pw_observacion").val()
    };

    $.ajax({
        url: url,
        data: datos,
        type: "Post",
        success: function (result) {

            var frm = document.getElementById("frm-entregar");
            frm.innerHTML = result;

            $("#pw_fecha").val("");
            $("#pw_observacion").val("");
        }
    });


}



function abrirModalDetalleVenta(ventaId, url) {


    var datos = {
        ventaId: ventaId,
    };

    $.ajax({
        url: url,
        data: datos,
        type: "Post",



        success: function (result) {

            var frm = document.getElementById("frmDetalleVentaCuerpo");
            frm.innerHTML = "";
            frm.innerHTML = result;

            $("#frmDetalleVenta").modal("show");
        }
    });


 
}





function filtrarTabla() {

    const tableReg = document.getElementById('detalleReparto');

    const searchText = document.getElementById('txtFiltrar').value.toLowerCase();

    let total = 0;

    // Recorremos todas las filas con contenido de la tabla
    for (let i = 1; i < tableReg.rows.length; i++) {

        let found = false;

        const cellsOfRow = tableReg.rows[i].getElementsByTagName('td');

        // Recorremos todas las celdas
        for (let j = 0; j < cellsOfRow.length && !found; j++) {

            if (cellsOfRow[j].classList.contains("omitir-busqueda")) {
                continue;
            }

            var variable = cellsOfRow[j].innerText;
            var compareWith = "";

            if (typeof variable === "undefined") {
                compareWith = cellsOfRow[j].innerHTML;
            }
            else {

                compareWith = variable.toLowerCase();
            }

            // Buscamos el texto en el contenido de la celda
            if (searchText.length == 0 || compareWith.indexOf(searchText) > -1) {
                found = true;
                total++;
            }
        }

        if (found) {
            tableReg.rows[i].style.display = '';
        } else {
            // si no ha encontrado ninguna coincidencia, esconde la
            // fila de la tabla
            tableReg.rows[i].style.display = 'none';
        }
    }

    // mostramos las coincidencias
    //const lastTR = tableReg.rows[tableReg.rows.length - 1];

    //const td = lastTR.querySelector("td");

    //lastTR.classList.remove("hide", "red");

    //if (searchText == "")
    //{
    //    lastTR.classList.add("hide");
    //} else if (total)
    //{
    //    td.innerHTML = "Se ha encontrado " + total + " coincidencia" + ((total > 1) ? "s" : "");
    //} else
    //{
    //    lastTR.classList.add("red");
    //    td.innerHTML = "No se han encontrado coincidencias";
    //}
}