window.onload = function () {
    RefreshProgrammes(true);
}

function RefreshProgrammes(skip = false){
    var programmes = document.getElementsByClassName("Programme");
    //Hide all programmes and unselect all checkboxes
    for (var i = 0; i < programmes.length; i++) {
        programmes[i].style.display = "none";
        if (!skip)
            programmes[i].getElementsByTagName("input")[0].checked = false;
    }

    //Get selected group selected option name
    const selectElement = document.querySelector('#GroupId');
    const selectedOption = selectElement.options[selectElement.selectedIndex];
    const groupName = selectedOption.text;

    var bachelorProgrammes = document.getElementsByClassName("Bachelor");
    var masterProgrammes = document.getElementsByClassName("Master");
    var toShow = [];

    if (groupName === "Bachelor" || groupName === "Bakalářská"){
        toShow = bachelorProgrammes;
    }
    else if (groupName === "Master" || groupName === "Diplomová"){
        toShow = masterProgrammes;
    }

    for (i = 0; i < toShow.length; i++) {
        toShow[i].style.display = "block";
    }
}