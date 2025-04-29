"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("http://localhost:5187/venteHub").build();

// Function to get state text based on state code
function GetStateText(state) {
    switch (state) {
        case 0:
            return "Initial";
        case 1:
            return "en cours de traitement";
        case 2:
            return "traité";
        default:
            return "Unknown";
    }
}

// Function to get state color based on state code (you need to define this function)
function GetStateColor(state) {
    // Implement this function according to your requirements
    // Return appropriate CSS class or color based on the state code
}

connection.on("VenteDeleted", function (venteId) {
    // Find and remove the row with the corresponding venteId
    $("#example2 tbody tr").each(function () {
        if ($(this).find("td:first").text().trim() === venteId.toString()) {
            $(this).remove(); // Remove the row
            return false; // Exit the loop after removing the row
        }
    });
});

connection.on("ReceiveUpdate", function (vente) {
    console.log("Received vente:", vente);
    // Update the UI with the updated Vente data
    updateTableRow(vente);
});

connection.on("NewVenteAdded", function (vente) {
    console.log("New Vente added:", vente);
    // Update the UI with the new Vente data
    updateTableRow(vente);
});

function updateTableRow(vente) {
    // Construct the new row HTML
    
    var newRow = '<tr class="odd ' + GetStateColor(vente.State) + '">' +
        '<td class="dtr-control sorting_1" tabindex="0">' + vente.Id + '</td>' +
        '<td>' + vente.cinNumber + '</td>' +
        '<td>' + vente.DateEmission + '</td>' +
        '<td>' + vente.Type + '</td>' +
        '<td>' + GetStateText(vente.State) + '</td>' +
        '<td>non traité</td>' +
        '<td>22233365555554</td>' +
        '<td>' + vente.ContratNumber + '</td>' +
        '<td>' + vente.City + '</td>' +
        '<td>' + vente.Country + '</td>' +
        '<td>edawa5</td>' +
        '<td> ' +
        '<form id="updateStateForm" action="' + '@Url.Action("UpdateStateToInProgress", "Ventes")' + '" method="post">' +
        '<input type="hidden" name="venteId" value="' + vente.Id + '" />' +
        '<button type="submit" id="btnvalider">i</button>' +
        '</form>' +
        '</td>' +
        '</tr>';

    // Append the new row to the table body
    $('#example2 tbody').append(newRow);
}
connection.on("NewPointageAdded", function (pointage) {
    // Add the new Pointage to the table dynamically
    var newRow = '<tr>' +
        '<td>' + pointage.nom + '</td>' +
        '<td>' + pointage.prénom + '</td>' +
        '<td>' + pointage.Date_pointage + '</td>' +
        '<td>' +
        '<form method="post" id="updateStateForm" action="/Pointage/Pointage_detail">' +
        '<input type="hidden" name="agent_terrein_id" id="agent_terrein_id" value="' + pointage.IdUserTerrain + '" />' +
        '<button type="submit" id="btnvalider">i</button>' +
        '</form>' +
        '</td>' +
        '</tr>';

    $('#example3 tbody').append(newRow);
});
connection.start().then(function () {
    console.log('Connected to hub');
});
