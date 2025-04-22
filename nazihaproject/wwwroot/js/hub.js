"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("http://localhost:5187/venteHub").build();

// Function to get state text based on state code

    


function GetStateText(state) {
    switch (state) {
        case 0: return "Initial";
        case 1: return "en cours de traitement";
        case 2: return "traité";
        case 3: return "Refusé Center";
        case 4: return "Refusé Terrain";
        default: return "Unknown";
    }
}

function GetStateColor(state) {
    switch (state) {
        case 0: return "darkblue-text";
        case 1: return "orange-text";
        case 2: return "green-text";
        case 3: return "darkred-text";
        case 4: return "red-text";
        default: return "black-text";
    }
}

function formatDate(date) {
    var d = new Date(date);
    var day = ('0' + d.getDate()).slice(-2);
    var month = ('0' + (d.getMonth() + 1)).slice(-2);
    var year = d.getFullYear();
    return `${day}/${month}/${year}`;
}

connection.on("VenteDeleted", function (venteId) {
    $("example2 tbody tr").each(function () {
        if ($(this).find("td:first").text().trim() === venteId.toString()) {
            $(this).remove();
            return false;
        }
    });
});

connection.on("ReceiveUpdate", function (ventes) {
    console.log("Received ventes:", ventes);
    var table = $('#example2').DataTable();
    table.clear().draw(); // Clear existing data
    ventes.forEach(function (vente) {
        addTableRow(vente);
    });
});

connection.on("NewVenteAdded", function (vente) {
    console.log("New Vente added:", vente);
    addTableRow(vente);
});

function addTableRow(vente) {
    var formattedDate = formatDate(vente.dateEmission);
    var newRow = '<tr class="odd ' + GetStateColor(vente.state) + ' blink">' +
        '<td class="dtr-control sorting_1" tabindex="0">' + vente.id + '</td>' +
        '<td>' + vente.cinNum + '</td>' +
        '<td>' + formattedDate + '</td>' +
        '<td>' + vente.type + '</td>' +
        '<td>' + GetStateText(vente.state) + '</td>' +
        '<td>' + (vente.reclamationCenter || '') + '</td>' +
        '<td>' + (vente.reclamationTerrain || '') + '</td>' +
        '<td>' + vente.contratNumber + '</td>' +
        '<td>' + vente.articleId + '</td>' +
        '<td>' +
        '<form id="updateStateForm" action="/Ventes/UpdateStateToInProgress" method="post" style="display:inline-block;">' +
        '<input type="hidden" name="venteId" value="' + vente.id + '" />' +
        '<button type="submit" id="btnvalider">' +
        '<i class="fas fa-info-circle"></i>' +
        '</button>' +
        '</form>' +
        '<form action="/Ventes/DeleteVente" method="post" onsubmit="return confirmDelete()" style="display:inline-block;">' +
        '<input type="hidden" name="venteId" value="' + vente.id + '" />' +
        '<button type="submit" id="btnvalide">delete</button>' +
        '</form>' +
        '</td>' +
        '</tr>';

    $('#example2').DataTable().row.add($(newRow)).draw(); // Add new row
}

connection.start().then(function () {
    console.log('Connected to hub');
}).catch(function (err) {
    return console.error(err.toString());
});

function confirmDelete() {
    return confirm("Êtes-vous sûr de vouloir supprimer cette vente ?");
}
