$(document).ready(function () {
    var control = document.getElementById("txtIdEmpresa");
    var tipoOp = control.getAttribute("data-tipoOp");

    desactivarTxt(tipoOp, control)
});

function desactivarTxt(tipoOp, control) {
    if (tipoOp != 1)
    {
        control.readOnly = true;
    }
}