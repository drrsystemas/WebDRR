let urlCargarImgJson = "";

$(document).ready(function ()
{
    //CargarImagenes();

    CargarImagenesJson(urlCargarImgJson);
});


function CargarImagenesJson(url) {

    if (urlCargarImgJson !== "") {
        $.ajax({
            type: "POST",
            url: url,
            success: function (response) {

                for (var i = 0; i < response.length; i++) {
                    var elemento = response[i];
                    if (elemento.ruta !== null) {
                        $("#img-prod-" + elemento.id).attr('src', elemento.ruta);

                        //momentaneo.
                        $("#img-prod-cel-" + elemento.id).attr('src', elemento.ruta);

                    }
                }
            },
            failure: function (response) {
                alert(response.responseText);
            },
            error: function (response) {
                alert(response.responseText);
            }
        });
    }

}




function CargarImagenes() {

    $(".post-Carga").each(function () {

        var img = $(this).attr("data-src");
        $(this).attr('src', img);

        $(this).fadeIn();

    })
}


function enlaceWpProducto(codigo, nombre, url)
{

    var wp = url + "/?text=Consultar sobre: [" + codigo + "] " + nombre;
    //alert(wp);
    var win = window.open(wp, '_blank');
    win.focus();
}



function funcionFiltrarFamilias() {
    var input, filter, table, listaEnlaces, enlace, i, visible;

    input = document.getElementById("txtFiltroFamilia");

    filter = input.value.toUpperCase();

    table = document.getElementById("menuItemsFamilia");

    listaEnlaces = table.getElementsByTagName("a");

    for (i = 0; i < listaEnlaces.length; i++) {
        visible = false;
        enlace = listaEnlaces[i];

        if (enlace && enlace.innerHTML.toUpperCase().indexOf(filter) > -1) {
            visible = true;
        }

        if (visible === true) {
            enlace.style.display = "";
        }
        else {
            enlace.style.display = "none";
        }
    }
}




function funcionFiltrarMarcas() {
    var input, filter, table, listaEnlaces, enlace, i, visible;

    input = document.getElementById("txtFiltroMarca");

    filter = input.value.toUpperCase();

    table = document.getElementById("menuItemsMarcas");

    listaEnlaces = table.getElementsByTagName("a");

    for (i = 0; i < listaEnlaces.length; i++) {
        visible = false;
        enlace = listaEnlaces[i];

        if (enlace && enlace.innerHTML.toUpperCase().indexOf(filter) > -1) {
            visible = true;
        }

        if (visible === true) {
            enlace.style.display = "";
        }
        else {
            enlace.style.display = "none";
        }
    }
}


function FiltrarFamilias_lista()
{
    var input, filter, table, items, enlace, i, visible;

    input = document.getElementById("txtFiltroFamilia");

    filter = input.value.toUpperCase();

    table = document.getElementById("listaTablaFamilia");

    items = table.getElementsByTagName("li");

    for (i = 0; i < items.length; i++) {
        visible = false;
        var elemento = items[i];
        //var enlace = elemento.children[0] + elemento.children[1];
        var enlace = elemento.innerText;

        //var nombreMarca = enlace.innerText.toUpperCase();
        var nombreFamilia = enlace.toUpperCase();
        if ((nombreFamilia !== "") && nombreFamilia.indexOf(filter) > -1)
        {
            visible = true;
        }

        if (visible === true)
        {
            elemento.style.display = "";
        }
        else
        {
            elemento.style.display = "none";
        }
    }
}

function FiltrarMarcas_lista() {
    var input, filter, table, items, enlace, i, visible;


    input = document.getElementById("txtFiltroMarca");

    filter = input.value.toUpperCase();

    table = document.getElementById("listaTablaMarca");
    items = table.getElementsByTagName("li");

    for (i = 0; i < items.length; i++) {
        visible = false;
        var elemento = items[i];
        var enlace = elemento.children[0];


        var nombreFamilia = enlace.innerText.toUpperCase();

        if ((nombreFamilia !== "") && nombreFamilia.indexOf(filter) > -1) {
            visible = true;
        }

        if (visible === true) {
            elemento.style.display = "";
        }
        else {
            elemento.style.display = "none";
        }
    }
}




function FiltrarClasificaciones_lista() {
    var input, filter, table, items, enlace, i, visible;


    input = document.getElementById("txtFiltroClasificacion");

    filter = input.value.toUpperCase();

    table = document.getElementById("listaTablaClasificacion");
    items = table.getElementsByTagName("li");

    for (i = 0; i < items.length; i++) {
        visible = false;
        var elemento = items[i];
        var enlace = elemento.children[0];


        var nombreFamilia = enlace.innerText.toUpperCase();

        if ((nombreFamilia !== "") && nombreFamilia.indexOf(filter) > -1) {
            visible = true;
        }

        if (visible === true) {
            elemento.style.display = "";
        }
        else {
            elemento.style.display = "none";
        }
    }
}







function mostrarModal(cantidad)
{

    if (cantidad >= 1) {
        $('#mostrarmodal').modal('show');
    }
    else {
        $('#mostrarmodal').modal('hide');
    }
}


function activarDesactivarFamilia(input) {


    if (input.checked == false) {
        $("#input-familia-identificador").val(0);
    }
    else {

        var valor = $(input).data("familia");

        $("#input-familia-identificador").val(valor);
    }

   

}



function restarCantidadCarrito() {

    alert("Se resta la cantidad del carrito");
}

function empty(val) {
    if (val === undefined)
        return true;

    if (typeof (val) == 'function' || typeof (val) == 'number' || typeof (val) == 'boolean' || Object.prototype.toString.call(val) === '[object Date]')
        return false;

    if (val == null || val.length === 0)        // null or 0 length array
        return true;

    if (typeof (val) == "object") {
        // empty object

        var r = true;

        for (var f in val)
            r = false;

        return r;
    }

    return false;
}




function sumarrestar_CantidadCarrito(idproducto, iditemcarrrito, tipo, fila, urlAction, representada)
{
    var identificadorCarrito = iditemcarrrito;

    if (tipo === 2) {
        identificadorCarrito = $("#btnRestar_" + fila).attr("data-id");
    }


    $.ajax({
        url: urlAction,
        data: {
            idproducto: idproducto,
            idItemCarrito: identificadorCarrito,
            tipoOp: tipo,
            representada: representada
        },
        type: "Post",
        success: function (result) {

            var verifica = empty(result.error)

            if (verifica == true) {
                var itemCarrito = result.itemCarrito;
                var totalesCarrito = result.totalesCarrito;

                var nombreProducto = itemCarrito.producto.nombreCompleto;

                var esquecaCantidad = "";

                if (parseInt(itemCarrito.cantidad) > 0) {
                    esquecaCantidad = "(" + itemCarrito.cantidad + ")";
                }

                var esquemaCodigo = "[" + idproducto + "]";

                var can = "";

                var cantidadElementos = parseInt(itemCarrito.producto.cantidad);

                if (cantidadElementos > 1) {
                    can = "{x" + cantidadElementos + "}";
                }

                var filaProducto = esquecaCantidad + esquemaCodigo + nombreProducto + can;

                var esquemaCarrito = "";

                if (parseInt(totalesCarrito.id) > 0) {
                    var esquemaCarrito = "[" + totalesCarrito.id + "] " + totalesCarrito.nombre;
                }


                if (tipo === 1) {

                    $("#esquema_" + fila).text(filaProducto);
                    $("#esquema_" + fila).addClass("itemEstaEnCarrito");

                    $("#infoCarrito").text(esquemaCarrito);

                    $("#btnRestar_" + fila).removeClass("ocultarDiv").addClass("mostrarDiv");
                    $("#btnRestar_" + fila).attr('data-id', itemCarrito.idItemCarrito);


                    //var urlModificar = $("#urlEsquema_" + fila).attr("data-url");
                    var urlModificar = "/Carrito/ModificarItemCarrito?IdItemCarrito=" + itemCarrito.idItemCarrito;


                    var urlverProducto = $("#urlEsquema_" + fila).attr("href");

                    $("#urlEsquema_" + fila).attr('data-url', urlverProducto);
                    $("#urlEsquema_" + fila).attr('href', urlModificar);



                    //----------------------Modo pantalla Grande -----------------------------------//

                    $("#cantidad_" + fila).val(itemCarrito.cantidad);



                    $("#tbp_enlaceVerProducto_" + fila).addClass("itemEstaEnCarrito");

                    $("#btnModificarAg_" + fila).attr('data-id', itemCarrito.idItemCarrito);

                    var urlModificar = "/Carrito/ModificarItemCarrito?IdItemCarrito=" + itemCarrito.idItemCarrito;
                    var urlverProducto = $("#tbp_enlaceVerProducto_" + fila).attr("href");
                    $("#tbp_enlaceVerProducto_" + fila).attr('data-url', urlverProducto);
                    $("#tbp_enlaceVerProducto_" + fila).attr('href', urlModificar);

                    //****//
                    if (result.extra === "AbrirEditar")
                    {
                        //esto abre la pantalla para editar directamanete.
                        document.location = urlModificar;
                    }

                    //****//



                }
                else {

                    //Dos posibilidades al quitar, que se elimine completamente el producto o se reduzca la cantidad.

                    if (parseInt(itemCarrito.cantidad) === 0) {

                        $("#esquema_" + fila).text(filaProducto);
                        $("#esquema_" + fila).removeClass("itemEstaEnCarrito");

                        $("#infoCarrito").text(esquemaCarrito);
                        $("#btnRestar_" + fila).removeClass("mostrarDiv").addClass("ocultarDiv");


                        var urlverProducto = $("#urlEsquema_" + fila).attr("data-url");
                        var urlModificar = $("#urlEsquema_" + fila).attr("href");

                        $("#urlEsquema_" + fila).attr('data-url', urlModificar);
                        $("#urlEsquema_" + fila).attr('href', urlverProducto);

                    }
                    else {
                        $("#esquema_" + fila).text(filaProducto);
                        $("#infoCarrito").text(esquemaCarrito);
                    }



                    //----------------------Modo pantalla Grande -----------------------------------//

                    $("#btnModificarAg_" + fila).attr('data-id', "0");
                    $("#tbp_enlaceVerProducto_" + fila).removeClass("itemEstaEnCarrito");

                    var urlverProducto = $("#tbp_enlaceVerProducto_" + fila).attr("data-url");
                    var urlModificar = $("#tbp_enlaceVerProducto_" + fila).attr("href");

                    $("#tbp_enlaceVerProducto_" + fila).attr('data-url', urlModificar);
                    $("#tbp_enlaceVerProducto_" + fila).attr('href', urlverProducto);

                    $("#cantidad_" + fila).val("0");

                }




            }
            else {

                //alert(result.error);
                window.location.href = result.url;
            }



        },
        error: function (req, status, error) {
            alert("Ocurrio un error, repita por favor la operación");
        }
    });



}



function sumarrestar_CantidadCarrito_dos(idproducto, fila, urlAction, representada)
{

    urlAction = urlAction + "_Dos";

    var tipo = 0;

    var identificadorCarrito = $("#btnModificarAg_" + fila).attr("data-id");

    var cantidad = parseInt( $("#cantidad_" + fila).val());

    if (cantidad === 0 || isNaN(cantidad))
    {
        //Eliminar
        if (identificadorCarrito !== "0") {
            //es que esta para eliminar.
            tipo = 2;
        }
        else {
            cantidad = 1;
            tipo = 1;
        }
    }
    else {
    //Agregar - Modificar
        tipo = 1;
    }

    if (tipo > 0) {
        $.ajax({
            url: urlAction,
            data: {
                idproducto: idproducto,
                idItemCarrito: identificadorCarrito,
                tipoOp: tipo,
                cantidad: cantidad,
                representada: representada
            },
            type: "Post",
            success: function (result) {

                var verifica = empty(result.error)

                if (verifica == true) {
                    var itemCarrito = result.itemCarrito;
                    var totalesCarrito = result.totalesCarrito;
                    var nombreProducto = itemCarrito.producto.nombreCompleto;

                    var esquecaCantidad = "";

                    if (parseInt(itemCarrito.cantidad) > 0) {
                        esquecaCantidad = "(" + itemCarrito.cantidad + ")";
                    }

                    var esquemaCodigo = "[" + idproducto + "]";

                    var can = "";

                    var cantidadElementos = parseInt(itemCarrito.producto.cantidad);

                    if (cantidadElementos > 1) {
                        can = "{x" + cantidadElementos + "}";
                    }

                    var filaProducto = esquecaCantidad + esquemaCodigo + nombreProducto + can;

                    var esquemaCarrito = "";

                    if (parseInt(totalesCarrito.id) > 0) {
                        var esquemaCarrito = "[" + totalesCarrito.id + "] " + totalesCarrito.nombre;
                    }



                    if (tipo === 1) {

                        //----------------------Modo Celular -----------------------------------//

                        $("#esquema_" + fila).text(filaProducto);
                        $("#esquema_" + fila).addClass("itemEstaEnCarrito");
                        $("#infoCarrito").text(esquemaCarrito);
                        $("#btnRestar_" + fila).removeClass("ocultarDiv").addClass("mostrarDiv");
                        $("#btnRestar_" + fila).attr('data-id', itemCarrito.idItemCarrito);
                        var urlModificar = "/Carrito/ModificarItemCarrito?IdItemCarrito=" + itemCarrito.idItemCarrito;
                        var urlverProducto = $("#urlEsquema_" + fila).attr("href");
                        $("#urlEsquema_" + fila).attr('data-url', urlverProducto);
                        $("#urlEsquema_" + fila).attr('href', urlModificar);

                        //----------------------Modo pantalla Grande -----------------------------------//


                        $("#cantidad_" + fila).val(itemCarrito.cantidad);



                        $("#tbp_enlaceVerProducto_" + fila).addClass("itemEstaEnCarrito");

                        $("#btnModificarAg_" + fila).attr('data-id', itemCarrito.idItemCarrito);

                        var urlModificar = "/Carrito/ModificarItemCarrito?IdItemCarrito=" + itemCarrito.idItemCarrito;
                        var urlverProducto = $("#tbp_enlaceVerProducto_" + fila).attr("href");
                        $("#tbp_enlaceVerProducto_" + fila).attr('data-url', urlverProducto);
                        $("#tbp_enlaceVerProducto_" + fila).attr('href', urlModificar);
                    }
                    else {

                        //Dos posibilidades al quitar, que se elimine completamente el producto o se reduzca la cantidad.
                        //----------------------Modo Celular -----------------------------------//
                        if (parseInt(itemCarrito.cantidad) === 0) {
                            $("#esquema_" + fila).text(filaProducto);
                            $("#esquema_" + fila).removeClass("itemEstaEnCarrito");
                            $("#infoCarrito").text(esquemaCarrito);
                            $("#btnRestar_" + fila).removeClass("mostrarDiv").addClass("ocultarDiv");
                            var urlverProducto = $("#urlEsquema_" + fila).attr("data-url");
                            var urlModificar = $("#urlEsquema_" + fila).attr("href");
                            $("#urlEsquema_" + fila).attr('data-url', urlModificar);
                            $("#urlEsquema_" + fila).attr('href', urlverProducto);
                        }
                        else {
                            $("#esquema_" + fila).text(filaProducto);
                            $("#infoCarrito").text(esquemaCarrito);
                        }

                        //----------------------Modo pantalla Grande -----------------------------------//

                        $("#btnModificarAg_" + fila).attr('data-id', "0");
                        $("#tbp_enlaceVerProducto_" + fila).removeClass("itemEstaEnCarrito");

                        var urlverProducto = $("#tbp_enlaceVerProducto_" + fila).attr("data-url");
                        var urlModificar = $("#tbp_enlaceVerProducto_" + fila).attr("href");

                        $("#tbp_enlaceVerProducto_" + fila).attr('data-url', urlModificar);
                        $("#tbp_enlaceVerProducto_" + fila).attr('href', urlverProducto);

                        $("#cantidad_" + fila).val("0");

                    }
                }
                else {
                    // alert(result.error);
                    window.location.href = result.url;
                }


             


            },
            error: function (req, status, error) {
                alert("Ocurrio un error, repita por favor la operación");
            }
        });
    }



}




function agregar_Carrito(idproducto, idItemCarrito, representada, urlAction, indice)
{

    $.ajax({
        url: urlAction,
        data: {
            idproducto: idproducto,
            idItemCarrito: idItemCarrito,
            representada: representada
        },
        type: "Post",
        success: function (result)
        {
            var verifica = empty(result.error)

            if (verifica == true)
            {

                var itemCarrito = result.itemCarrito;
                var totalesCarrito = result.totalesCarrito;
                var nombreProducto = itemCarrito.producto.nombreCompleto;

                var esquecaCantidad = "";

                if (parseInt(itemCarrito.cantidad) > 0) {
                    esquecaCantidad = "(" + itemCarrito.cantidad + ")";
                }

                var esquemaCodigo = "[" + idproducto + "]";

                var can = "";

                var cantidadElementos = parseInt(itemCarrito.producto.cantidad);

                if (cantidadElementos > 1) {
                    can = "{x" + cantidadElementos + "}";
                }

                var filaProducto = esquecaCantidad + esquemaCodigo + nombreProducto + can;

                var esquemaCarrito = "";

                if (parseInt(totalesCarrito.id) > 0) {
                    var esquemaCarrito = "[" + totalesCarrito.id + "] " + totalesCarrito.nombre;
                }
               
                var esquemaCarrito = "";

                if (parseInt(totalesCarrito.id) > 0) {
                    esquemaCarrito = "[" + totalesCarrito.id + "] " + totalesCarrito.nombre;
                }

                $("#infoCarrito").text(esquemaCarrito);

                $("#name_prod_" + indice).addClass("itemEstaEnCarrito");

                $('#mostrarmodal').modal('show');
            }
            else
            {
                window.location.href = result.url;
            }
        },
        error: function (req, status, error) {
            alert("Ocurrio un error, repita por favor la operación");
        }
    });
    
}





function FiltrarProductoPagina()
{
    const tableReg = document.getElementById('datos');

    const searchText = document.getElementById('searchTerm').value.toLowerCase();

    let total = 0;

    // Recorremos todas las filas con contenido de la tabla
    for (let i = 1; i < tableReg.rows.length; i++) {

        let mostrarFila = false;

        const cellsOfRow = tableReg.rows[i].getElementsByTagName('td');


        if (urlCargarImgJson === "") {

            var variable = cellsOfRow[3].innerText;
            var compareWith = "";

            if (typeof variable === "undefined") {
                compareWith = cellsOfRow[j].innerHTML;
            }
            else {

                compareWith = variable.toLowerCase();
            }

            // Buscamos el texto en el contenido de la celda
            if (searchText.length == 0 || compareWith.indexOf(searchText) > -1) {
                mostrarFila = true;
                total++;
            }
        }
        else {

            // Recorremos todas las celdas
            for (let j = 0; j < cellsOfRow.length && !mostrarFila; j++) {
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
                    mostrarFila = true;
                    total++;
                }
            }

        }


        if (mostrarFila) {
            tableReg.rows[i].style.display = '';
        } else {
            // si no ha encontrado ninguna coincidencia, esconde la
            // fila de la tabla
            tableReg.rows[i].style.display = 'none';
        }
    }

    // mostramos las coincidencias
    const lastTR = tableReg.rows[tableReg.rows.length - 1];
    const td = lastTR.querySelector("td");
    lastTR.classList.remove("hide", "red");
    if (searchText == "") {
        lastTR.classList.add("hide");
    } else if (total) {
        td.innerHTML = "Se ha encontrado " + total + " coincidencia" + ((total > 1) ? "s" : "");
    } else {
        lastTR.classList.add("red");
        td.innerHTML = "No se han encontrado coincidencias";
    }
}


function FiltrarProductoPaginaTarjeta() {

    //var tarjetasVisibles;
    var tarjetas = document.querySelectorAll('div.card');

    const searchText = document.getElementById('searchTerm').value.toLowerCase();

    var cantidad = tarjetas.length;

    // Recorremos todas las filas con contenido de la tabla
    for (var i = 0; i < cantidad; i++) {

        const card = tarjetas[i];

        card.classList.remove('card_ancho_minimo');

        var elemento = card.querySelector('b');

        var textoElmento = elemento.innerHTML.toLowerCase();

        // Buscamos el texto 
        if (textoElmento.includes(searchText)) {
            card.classList.remove('hide');
            card.classList.add('card_ancho_minimo');
        }
        else {
            card.classList.add('hide');
        }
    }

}

function wpProducto(urlWp, codigo, nombre) {
    var wp = urlWp + "/?text=Consultar sobre: [" + codigo + "] " + nombre;
    //alert(wp);
    var win = window.open(wp, '_blank');
    win.focus();
}

function getDato() {
    var valor = $("#txtBusquedaFrmModal").val();
    return valor;
}

$("#btnModal").click(function () {
    var valor = $("#txtBusquedaFrmModal").val();

    window.location.href = "/Producto/Productos/?Dato=" + valor;
});


$('#txtBusquedaFrmModal').on('keypress', function (e) {
    if (e.which === 13) {
        var valor = $("#txtBusquedaFrmModal").val();
        window.location.href = "/Producto/Productos/?Dato=" + valor;
    }
});


$(window).scroll(function () {
    if ($(this).scrollTop() > 300) {
        $('a.scroll-top').fadeIn('slow');
    } else {
        $('a.scroll-top').fadeOut('slow');
    }
});

$('a.scroll-top').click(function (event) {
    event.preventDefault();
    $('html, body').animate({ scrollTop: 0 }, 600);
});



function abrirMenuOpciones() {
    var verImg = true;
    $("#checkImg").prop("value", verImg);
    $("#dialogo1").modal();
}

function abrirMarcas() {
    $("#frmMarca").modal();
}

function abrirRubros() {
    $("#frmRubro").modal();
}


function abrirClasificaciones() {
    $("#frmClasificacion").modal();
}

function abrirOrdenamiento() {
    $("#frmOrdenamiento").modal();
}

function verProducto(url) {
    window.location = url;
}





function copiarEnlaceProducto(url, dato, familiaId, marcaId, paginaActual)
{
    $.ajax({
        url: url,
        data: {
            dato: dato,
            familiaId: familiaId,
            marcaId: marcaId,
            paginaActual: paginaActual
        },
        type: "Post",
        success: function (result) {
            $('#datosEnlace').show();
            $('#datosEnlace').html(result);

            var codigoACopiar = document.getElementById('datosEnlace');
            var seleccion = document.createRange();
            seleccion.selectNodeContents(codigoACopiar);
            window.getSelection().removeAllRanges();
            window.getSelection().addRange(seleccion);
            var res = document.execCommand('copy');
            window.getSelection().removeRange(seleccion);
            $('#datosEnlace').hide();
        }
    });
}




function copiarPagina(url, dato, familiaId, marcaId, paginaActual) {
    $.ajax({
        url: url,
        data: {
            dato: dato,
            familiaId: familiaId,
            marcaId: marcaId,
            paginaActual: paginaActual
        },
        type: "Post",
        success: function (result)
        {
            var copyTextarea = document.createElement("textarea");
            copyTextarea.style.position = "fixed";
            copyTextarea.style.opacity = "0";

            copyTextarea.textContent = result;

            document.body.appendChild(copyTextarea);
            copyTextarea.select();
            document.execCommand("copy");
            document.body.removeChild(copyTextarea);
        }
    });
}


function ver_DetalleProducto(idproducto, iditemcarrrito, tipo, fila, urlAction, representada)
{
   // var url = urlAction + "?idItemCarrito=" + iditemcarrrito + "&idProducto=" + idproducto;

    window.location.href = urlAction;


    //$.ajax({
    //    url: urlAction,
    //    data: {
    //        idproducto: idproducto,
    //        idItemCarrito: iditemcarrrito,
    //        tipoOp: tipo,
    //        representada: representada
    //    },
    //    type: "Post",
    //    success: function (result)
    //    {
    //        let res = result;

    //    },
    //    error: function (req, status, error) {
    //        alert("Ocurrio un error, repita por favor la operación");
    //    }
    //});



}