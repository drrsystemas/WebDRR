$(document).ready(function () {


    $("#customSwitch1").change(function () {
        if (this.checked) {

            $("#lbLlave").text("Activado");
        }
        else {
            $("#lbLlave").text("Desactivado");
        }
    });


    var customSwitchActivo = $("#customSwitch1").data("inicial");

    if (customSwitchActivo === 1) {
        $("#customSwitch1").prop("checked", true);
        $("#lbLlave").text("Activado");
    }
    else {
        $("#customSwitch1").prop("checked", false);
        $("#lbLlave").text("Desactivado");
    }
});