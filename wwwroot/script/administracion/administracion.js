let _urlAtras = "";

////window.onbeforeunload = function (e) {
////    if (_urlAtras !== "") {
////        //_urlAtras = history.go(-1);
////        //e.preventDefault();
////        //document.location.href = ;
        
////    }
////};


//window.addEventListener('beforeunload', (event) => {
//    // Cancel the event as stated by the standard.
//    event.preventDefault();
//    // Chrome requires returnValue to be set.
//    event.returnValue = '';

//        if (_urlAtras !== "") {
//        //_urlAtras = history.go(-1);
//        window.location = _urlAtras;
//    }
//});

//function noatras() {
//    window.location.hash = "no-back-button";
//    window.location.hash = "Again-No-back-button"
//    window.onhashchange = function () {
//        window.location.hash = "no-back-button";
//    }
//}


//function modificarUrlAtras(url) {


//    let numeroDeEntradas = window.history.length;






//    const stateObj = { panel: 'Panel de Control' };
//    history.replaceState(stateObj, '', url);
//}





//window.onpopstate = function (e) {

//    if (e.state) {
//        document.getElementById("content").innerHTML = _urlAtras;
//        document.title = e.state.pageTitle;
//    }


//    //if (_urlAtras !== "") {
//    //    //_urlAtras = history.go(-1);
//    //    //e.preventDefault();
//    //    //document.location.href = ;

//    //}
//};

function agregarHistoria() {
    window.history.pushState({ page: "another" }, "agrego manual", _urlAtras);
}


window.onpopstate = function (e) {
    if (e.state) {
        if (_urlAtras !== "") {
            e.preventDefault();
            window.location.href = _urlAtras;

        }
    }
};
