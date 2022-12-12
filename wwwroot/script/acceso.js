function onSignIn(googleUser) {
    // Useful data for your client-side scripts:
    var profile = googleUser.getBasicProfile();
    var id_token = googleUser.getAuthResponse().id_token;

    //console.log("ID: " + profile.getId()); // Don't send this directly to your server!
    //console.log('Full Name: ' + profile.getName());
    //console.log('Given Name: ' + profile.getGivenName());
    //console.log('Family Name: ' + profile.getFamilyName());
    //console.log("Image URL: " + profile.getImageUrl());
    //console.log("Email: " + profile.getEmail());
    
    //console.log("ID Token: " + id_token);



    $("#inputId").val(profile.getId());
    $("#inputCorreo").val(profile.getEmail());
    $("#inputNombre").val(profile.getName());
    $("#inputImagen").val(profile.getImageUrl());
    $("#inputToken").val(id_token);
    $("#dato").show();
    

    //var url = '@Url.Action("Gmail", "Acceso")';
    //var url = url + "?Id=" + profile.getId() + "&Nombre=" +
    //    profile.getName() + "&Imagen=" + profile.getImageUrl() +
    //    "&Token=" + id_token;

    //location.href = url;
}



function signOut() {
    var auth2 = gapi.auth2.getAuthInstance();
    auth2.signOut().then(function () {
        console.log('User signed out.');
    });
}






//function onSignIn(googleUser) {
//    // Useful data for your client-side scripts:
//    var profile = googleUser.getBasicProfile();

//    //console.log("ID: " + profile.getId()); // Don't send this directly to your server!
//    //console.log('Full Name: ' + profile.getName());
//    //console.log('Given Name: ' + profile.getGivenName());
//    //console.log('Family Name: ' + profile.getFamilyName());
//    //console.log("Image URL: " + profile.getImageUrl());
//    //console.log("Email: " + profile.getEmail());

//    //// The ID token you need to pass to your backend:
//    //var id_token = googleUser.getAuthResponse().id_token;
//    //console.log("ID Token: " + id_token);

//    var datos = new Object();
//    datos.Id = profile.getId();
//    datos.Nombre = profile.getName();
//    datos.Imagen = profile.getImageUrl();
//    datos.Correo = profile.getEmail();
//    datos.Token = googleUser.getAuthResponse().id_token;

//    return datos;
//}



//function cambiarNombreBtnGoogle() {
//    setTimeout(function () {
//        $('#GoogeSigninButton div div span span:last').text("Ingresar");
//        $('#GoogeSigninButton div div span span:first').text("Ingresar");
//        $('#GoogeSigninButton div div span span:last').css("font-family", "Arial");//for font formatting
//        $('#GoogeSigninButton div div span span:first').css("font-family", "Arial");//for font formatting
//    }, 1000);
//}

