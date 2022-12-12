var _haySucursal = "";
var _ingresoBusqueda = false;

$(document).ready(function () {

    if (_haySucursal === "False") {
        $("#ventana-formulario-fucursales").modal('show');
    }
    else {
        document.getElementById("productoActual").style.display = 'none';


        document.getElementById("btnBuscar").addEventListener("click", buscarProducto, false);


        document.getElementById("txtBuscarProducto").focus();
    }


});


//Manejador del evento click del btnBuscar.
function buscarProducto()
{

    document.getElementById("cargando").insertAdjacentHTML("afterbegin", '<div class="my-1 spinner-border text-dark" style="width:3rem; height:3rem;"  role="status"><span class="sr-only">Loading...</span></div>');

    mostrarTabla();

    let btnBuscar = document.getElementById("btnBuscar");

    btnBuscar.disabled = true;


    let modoVisor = document.getElementById("ch-activar-visor").checked;

    let elementoTxt = document.getElementById("txtBuscarProducto");

    let dato = elementoTxt.value;

    elementoTxt.value = "";

    let productoActual = document.getElementById("productoActual");
    productoActual.innerHTML = "";

    let frm = document.getElementById("frmBusquedaColector");

    var urlAction = frm.dataset.urlBuscar;

    var parametros = {
        "datoProducto": dato,
        "modoVisor": modoVisor
    }

    

    $.ajax({
        type: "POST",
        url: urlAction,
        data: parametros,
        dataType: "html",
        success: function (response)
        {
            document.getElementById("cargando").innerHTML = "";

            if (isNullEmpty(response) == false) {

                let resp = JSON.parse(response);

                if (resp.tipo == 1) {
                    modificarFila(resp.idProducto, resp.cantidad);
                }
                if (resp.tipo == 3) {
                    agregarFila(resp.idProducto, resp.codigoBarra, resp.dato, resp.cantidad, resp.fecha, resp.registro);

                    elementoTxt.focus();
                }
                else if (resp.tipo == 10){
                    

                    let divModal = document.getElementById("frmModal");
                    divModal.innerHTML = "";
                    divModal.innerHTML = resp.html;
                    $("#frm-seleccionar-producto").modal('show');
                }
            }

            _ingresoBusqueda = false;
            btnBuscar.disabled = false;
        },
        failure: function (response)
        {
            document.getElementById("cargando").innerHTML = "";

            elementoTxt.focus();
            _ingresoBusqueda = false;
            btnBuscar.disabled = false;
        },
        error: function (response)
        {
            
            document.getElementById("cargando").innerHTML = "";

            alert("Para realizar la busqueda ingrese el cod/nombre del producto");

            elementoTxt.focus();
            _ingresoBusqueda = false;
            btnBuscar.disabled = false;
        }
    });
}

function btnLimpiarProducto() {

    mostrarTabla();

    document.getElementById("productoActual").style.display = 'none';
    document.getElementById("productoActual").innerHTML = "";

    let elementoTxt = document.getElementById("txtBuscarProducto");
    elementoTxt.focus();

}


function mostrarTabla() {

    var tabla_tr = document.getElementById("tablaProductos").getElementsByTagName("tbody")[0].rows;

    for (var i = 0; i < tabla_tr.length; i++)
    {
        var tr = tabla_tr[i];
        tr.className = "mostrar";
    }
}


//Manejador del evento click del btnBuscar.
function seleccionarProducto(btn) {

    //mostrarTabla();

    //$(btn).prop('disabled', true);
    btn.disabled = true;

    var idProducto = $(btn).data("productoId");

    var urlAction = $("#frmBusquedaColector").data("urlBuscar");

    var parametros = {
        "datoProducto": idProducto
    }

    $.ajax({
        type: "POST",
        url: urlAction,
        data: parametros,
        dataType: "html",
        success: function (response) {

            btn.disabled = false;

            document.getElementById("productoActual").style.display = 'block';

            document.getElementById("productoActual").innerHTML = response;



            var idProducto = $("#parrafoVerProducto").data("prodid");
            var suma = $("#parrafoVerProducto").data("suma");

            buscarTabla(idProducto, suma);
        },
        failure: function (response) {
            $("#btnBuscar").prop('disabled', false);
        },
        error: function (response) {
            $("#btnBuscar").prop('disabled', false);
            alert("Para realizar la busqueda ingrese el cod/nombre del producto");
        }
    });
}


$("#txtBuscarProducto").keypress(function (e) {
    var keycode = (e.keyCode ? e.keyCode : e.which);


    if (keycode == '13')
    {
        if (_ingresoBusqueda == false) {
            _ingresoBusqueda = true;

            buscarProducto();
        }

        e.preventDefault();
        return false;
    }
});




function buscarTabla(idProducto, suma) {

    var tabla_tr = document.getElementById("tablaProductos").getElementsByTagName("tbody")[0].rows;

    for (var i = 0; i < tabla_tr.length; i++) {

        var tr = tabla_tr[i];

        var idCeldaTabla = parseInt(tr.cells[0].innerText);

        if (idProducto === idCeldaTabla) {
            tr.className = "mostrar";

            var cantidadActual = parseInt(tr.cells[2].innerText);
            if (suma == "True") {
                cantidadActual = cantidadActual + 1;
            }
            else {
                cantidadActual = cantidadActual - 1;
            }
            tr.cells[2].innerHTML = cantidadActual;

        }
        else {
            tr.className = "ocultar";
        }
    }
}


function agregarFila(id,codb, dato, cantidad, fecha, registro) {

    let fila = document.createElement("tr");

    let celda_codBarras = `<td class="d-none d-sm-table-cell">${codb}</td>`;

    let celda_dato = `<td><a href="/Colector/Modificar?id=${id}&reg=${registro}">${dato}</a></td>`;

    let celda_cantidad = `<td>
                        <div class="input-group input-group-sm cantidadDatos">
                            <div class="input-group-prepend">
                                <button onclick="sumar_restar_colector('${id}','${registro}','0')" class="btn btn-sm btn-danger" type="button">
                                <i class="fas fa-minus"></i>
                                </button>
                            </div>

                            <input id="cantidad_${id}" readonly="" min="0" value="${cantidad}" class="form-control p-1 text-center" aria-describedby="basic-addon2">

                            <div class="input-group-append">
                                <button  onclick="sumar_restar_colector('${id}','${registro}','1')"  class="btn btn-sm btn-primary" type="button">
                                <i class="fas fa-plus"></i>
                                </button>
                            </div>
                        </div>
                        </td>`;

    let celda_fecha = `<td class="d-none d-sm-table-cell">${fecha}</td>`;

    fila.innerHTML = `${celda_codBarras} ${celda_dato} ${celda_cantidad} ${celda_fecha}`;

    $("#tablaProductos>tbody").prepend(fila);

}




function modificarFila(id, cantidad) {

    let celdaCantidad = document.getElementById(`cantidad_${id}`);
    celdaCantidad.value = cantidad;
}


function eliminarFila(id) {

    let trSeleccionado = document.getElementById(`fila_${id}`);

    trSeleccionado.remove();

}



function sumar_restar_colector(id, reg, tipo) {

    let cantidad = document.getElementById(`cantidad_${id}`).value;

    

    if (cantidad === "1" && tipo === "0") {
        
        let btnModal = document.getElementById("frm-modal-btn-modal-aceptar");
        btnModal.dataset.id = id;
        btnModal.dataset.tipo = 2;
        btnModal.dataset.cantidad = cantidad;
        btnModal.dataset.reg = reg;

        $("#frm-modal-si-no").modal("show");
    }
    else {
        suma_resta(id, reg, tipo, cantidad);
    }

}


function suma_resta(id, reg, tipo, cantidad)
{

    if (tipo === 2)
    {
        let btnModal = id;
        reg = btnModal.dataset.reg;
        id = btnModal.dataset.id;
        tipo = btnModal.dataset.tipo;
        cantidad = btnModal.dataset.cantidad;
    }

    let tablaProducto = document.getElementById("tablaProductos");

    let url = tablaProducto.dataset.urlSumarRestar;

    var parametros =
    {
        "registro":reg,
        "idProducto": id,
        "tipo": tipo,
        "cantidad": cantidad
    }

    $.ajax({
        type: "POST",
        url: url,
        data: parametros,
        dataType: "html",
        success: function (response) {

            if (isNullEmpty(response) == false)
            {
                let resp = JSON.parse(response);
             
                if (resp.tipo == 2)
                {
                    eliminarFila(resp.idProducto);
                }
                else
                {
                    if (resp.tipo == 0)
                    {
                        modificarFila(resp.idProducto, resp.cantidad);
                    }
                    else if (resp.tipo == 1)
                    {
                        modificarFila(resp.idProducto, resp.cantidad);
                    }
                }
            }

            document.getElementById("txtBuscarProducto").focus();

        },
        failure: function (response) {
        },
        error: function (response) {

        }
    });
}

function eliminar_colector(id, reg, tipo, url, ir) {


    var parametros =
    {
        "registro": reg,
        "idProducto": id,
        "tipo": tipo,
        "cantidad": 0
    }

    $.ajax({
        type: "POST",
        url: url,
        data: parametros,
        dataType: "html",
        success: function (response) {

            window.location.href = ir;

        },
        failure: function (response) {
        },
        error: function (response) {

        }
    });
}

function selecionarProductoColector(codigoId, codigoBarra, nombre, url) {

    $("#frm-seleccionar-producto").modal('hide');

    let parametros = {
        "codigoId": codigoId,
        "codigoBarra": codigoBarra,
        "nombre": nombre
    }

    $.ajax({
        type: "POST",
        url: url,
        data: parametros,
        dataType: "html",
        success: function (response) {

            let resp = JSON.parse(response);

            if (resp.tipo == 1) {
                //suma + 1
                modificarFila(resp.idProducto, resp.cantidad);
            }
            else if (resp.tipo == 3) {
                //agrega.
                agregarFila(resp.idProducto, resp.codigoBarra, resp.dato, resp.cantidad, resp.fecha, resp.registro);

            }

            document.getElementById("txtBuscarProducto").focus();

        },
        failure: function (response) {
        },
        error: function (response) {

        }
    });
}






function cambiarSucursal() {
   // ventana - formulario - fucursales
    $("#ventana-formulario-fucursales").modal("show");
}