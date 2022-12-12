let esquema = 0;

//*Falta una funcion que seria la de analizar el estado es para cuando se vuelva para atras o cuando se modifica.*//


/**
 * Esta funcion activa o descativa los comprobantes usando la modalidad de cobranza 1.
 * Realiza una llamada ajax para que los datos se actualicen en el model.
 * @param {any} control - es el control que se va activar - o a desactivar
 * @param {any} comprobante - el nombre del comprobante
 * @param {any} url - la url para activar el modelo
 * @param {any} indice - nos permite localizar el control para ingresar el importe.
 */
function selDesComprobante(control, comprobante, url, indice)
{

    try
    {
        if (comprobante === "Adelanto")
        {
            if (control.checked === true) {
                $("#txtAdelanto").prop("disabled", false);
            }
            else {
                $("#txtAdelanto").prop("disabled", true);
                $("#txtAdelanto").val(0);

                var cantidadElementos = $("#txtCobranza_1").data("cantidad");
                var totalPagar = calcularTotalPago(cantidadElementos);
                var txtSaldo = formatoMoneda_SinSignoMoneda(totalPagar.toString());
                $("#compSeleccionadosSaldo").html("Total a Pagar: " + txtSaldo);

            }
        }
        else
        {
            var activo = false;

            $("#msjError").html("");

            var esnulo = empty(control);

            if (esnulo === false) {
                var saldo = parseFloat($("#txtCobranza_" + indice).data("saldo").replace(',', '.')).toFixed(2);

                var adelanto = parseFloat($("#txtCobranza_" + indice).data("adelanto").replace(',', '.')).toFixed(2);


                //Recontra revisarlo.
                if (saldo < 0) {
                    if (adelanto === '0.00') {
                        adelanto = saldo;
                    }
                }

                if (control.checked === true) {
                    activo = true;

                    $("#txtCobranza_" + indice).prop("disabled", false);

                    if (saldo > 0) {

                        saldo = formatoMoneda_SinSignoMoneda(saldo);

                        $("#txtCobranza_" + indice).val(saldo);

                    }
                    else {

                        adelanto = adelanto.replace('-', '');

                        adelanto = "-" + formatoMoneda_SinSignoMoneda(adelanto);

                        $("#txtCobranza_" + indice).val(adelanto);
                    }

                }
                else {
                    $("#txtCobranza_" + indice).val(0);
                    $("#txtCobranza_" + indice).prop("disabled", true);
                }

            }


            $.ajax(
                {
                    url: url,
                    data:
                    {
                        comprobante: comprobante,
                        activo: activo
                    },
                    type: "post",
                    dataType: 'json',
                    success: function (res) {
                        $("#compSeleccionados").html("Comprobantes Seleccionados: " + res.id);
                        cantidadComprobantes = res.id;

                        //$("#compSeleccionadosSaldo").html("Total a Pagar: " + res.nombre);

                        var cantidadElementos = $("#txtCobranza_1").data("cantidad");

                        var totalPagar = calcularTotalPago(cantidadElementos);

                        var txtSaldo = formatoMoneda_SinSignoMoneda(totalPagar.toString());
                        $("#compSeleccionadosSaldo").html("Total a Pagar: " + txtSaldo);



                    }
                });


        }


    } catch (error)
    {
        console.error(error);
    }



}






/**
 * Esta funcion realiza un control en el esquema 1 de que por lo menos se haya seleccionado 1 comprobante.
 * */
function continuarProcesoCobranza()
{
    if (esquema == 2) {
        if (cantidadComprobantes === 0 || cantidadComprobantes <= 0) {
            $("#msjError").html("Para continuar el proceso de cobranza es necesario seleccionar 1 Comprobante de Venta")
        }
        else {

            $("#msjError").html("");

            var cantidadErrores = document.getElementsByClassName("colorRojo").length;

            if (cantidadErrores === 0) {
                    $("#frmVerificaEsquemaUno").submit();
            }
            else {
                $("#msjError").html(" Revisar que no haya ningun comprobante con un cobro mayor al del saldo del comprobante, se marca en rojo en la tabla");
            }
        }

    }
    else {


        $("#msjErrorEsquema2").html("");

        let verificarImpore = $("#inputImporte").val();

        if (isNullEmpty(verificarImpore) == true) {

            $("#msjErrorEsquema2").html("Para continuar el proceso de cobranza se tiene que agregar el importe")
        }
        else {
            $("#frmVerificaEsquemaDos").submit();
        }
    }

}


/**
 * Permite que se puede cambiar en forma dinamica entre los 2 esquemas de cobranza-
 * 1- esquema que permite seleccione de comprobantes y a los mismos ingresarle el importe.
 * 2- esquema por importe.
 * @param {any} myRadio el radio es el control de esquema 1 o 2
 */
function radioEsquemaClick(myRadio)
{
    var valor = myRadio.value;

    seleccionarEsquema(valor);


}

    function seleccionarEsquema(valor) {
        if (parseInt(valor) === 1) {
            esquema = 1;

            $("#esquemaUno").removeClass("ocultarDiv");
            $("#esquemaDos").addClass("ocultarDiv");
        }

        else {
            esquema = 2;

            $("#esquemaDos").removeClass("ocultarDiv");
            $("#esquemaUno").addClass("ocultarDiv");
        }
    }
/** Esta codigo afecta a el input del esquema 2 donde se ingresa el importe, el mismo se formatea para que sea mas facil la lectura
 * ya que los importes son bastante grandes y se hace dificil la lectura.
 */
$("#inputImporte").on({

    "focus": function (event)
    {
        $(event.target).select();
    },
    "keyup": function (event)
    {
        $(event.target).val(function (index, value)
        {
            return formatoMoneda_SinSignoMoneda(value);
        });
    }
});




/** Permite el formateo de los importes en la grilla usando el esquema 1 para facilitar la lectura
 * en caso de que el importe sea superior al del comprobante agrega una clase que marca el input en rojo.
 */
$(".ingresarCobro").on({

    "focus": function (event)
    {
        $(event.target).select();
    },
    "keyup": function (event)
    {
        try
        {
            var importeNegativo = false;

            var elemento = event.currentTarget;

            var saldo = parseFloat($(elemento).data("saldo").replace(',', '.')).toFixed(2);
            var adelanto = parseFloat($(elemento).data("adelanto").replace(',', '.')).toFixed(2);

            //*cantidad de elemetos*//
            var cantidadElementos = $(elemento).data("cantidad");

            $(event.target).val(function (index, value) {

                value = formatoMoneda_SinSignoMoneda(value);
                var valorActual = parseFloat(value.replace('.', '').replace(',', '.'));


                var totalPagar = calcularTotalPago(cantidadElementos);

                var txtSaldo = formatoMoneda_SinSignoMoneda(totalPagar.toString());
                $("#compSeleccionadosSaldo").html("Total a Pagar: " + txtSaldo);

                //*CLASES EN ROJO*//
                if (saldo > 0) {
                    //Validamos que el valor ingresado no sea mayor al valor del saldo.
                    if (valorActual > saldo) {
                        $(elemento).addClass("colorRojo");
                    }
                    else {
                        $(elemento).removeClass("colorRojo");

                        $("#msjError").html("");
                    }
                }
                else {

                    adelanto = adelanto.replace('-', '');
                    adelanto = parseFloat(adelanto);

                    if (valorActual > adelanto) {
                        $(elemento).addClass("colorRojo");
                    }
                    else {
                        $(elemento).removeClass("colorRojo");

                        $("#msjError").html("");
                    }

                    importeNegativo = true;
                }


                var retono = value;

                if (importeNegativo === true) {

                    retono = "-" + value;
                }


                return retono;

            });
        } catch (error)
        {
            console.error(error);
        }





    }
});



$("#txtAdelanto").on({

    "focus": function (event) {
        $(event.target).select();
    },
    "keyup": function (event) {
        $(event.target).val(function (index, value) {


            var cantidadElementos = $("#txtCobranza_1").data("cantidad");
            var totalPagar = calcularTotalPago(cantidadElementos);

            var txtSaldo = formatoMoneda_SinSignoMoneda(totalPagar.toString());
            $("#compSeleccionadosSaldo").html("Total a Pagar: " + txtSaldo);


            return formatoMoneda_SinSignoMoneda(value);

        });
    }
});



function calcularTotalPago(cantidadElementos) {
    try {

        var totalPagar = 0;

        for (var i = 0; i < cantidadElementos; i++)
        {
            var indice = i + 1;
            var item = $("#txtCobranza_" + indice).val();
            var estaVacio = empty(item);

            if (estaVacio === false)
            {


                item = formatoMoneda_SinSignoMoneda(item);
                var importeItem = parseFloat(item.replace('.', '').replace(',', '.'));

                //var tipoOp = $("#txttipoOperacion_" + indice).data("tipooperacionid");

                //tipoOp = parseInt(tipoOp);

                //if (tipoOp === 3) {
                //    totalPagar = totalPagar - importeItem;
                //}
                //else {
                //    totalPagar = totalPagar + importeItem;
                //}


                totalPagar = totalPagar + importeItem;
            }
        }

        var elmentoAdelanto = $("#txtAdelanto").val();
        var estaVacioelmentoAdelanto = empty(elmentoAdelanto);

        if (estaVacioelmentoAdelanto === false) {

            elmentoAdelanto = formatoMoneda_SinSignoMoneda(elmentoAdelanto);
            itemAdelanto = parseFloat(elmentoAdelanto.replace('.', '').replace(',', '.'));
            totalPagar = totalPagar + itemAdelanto;
        }

        return totalPagar.toFixed(2);

    } catch (error) {
        console.error(error);
    }


}



function seleccionarSaldoTotal(saldo)
{

    var txtSaldo = formatoMoneda_SinSignoMoneda(saldo.replace(',', ''));

    $("#inputImporte").val(txtSaldo);
}



function asignarImorte_editarCobro(importe) {

    var txtSaldo = formatoMoneda_SinSignoMoneda(importe.replace(',', ''));

    $("#inputImporte").val(txtSaldo);
}