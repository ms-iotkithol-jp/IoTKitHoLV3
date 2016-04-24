$(function () {
    var mobileAppName = "[Mobile App Name]";
    $.ajax({
        url: 'http://' + mobileAppName + '.azurewebsites.net/tables/DeviceEntry',
        type: 'GET',
        headers: {
            'ZUMO-API-VERSION': '2.0.0'
        },
        success: function (results) {

            // Handle update
            $(document.body).on('click', '.item-update', function () {
                var itemId = getDEItemId(this);
                var itemDevId = getDEItemDevId(this);
                var elSA = document.getElementById('item-sa-' + itemDevId);
                var isServiceAvailable = true;
                if (elSA.checked != true) {
                    isServiceAvailable = false;
                }
                var elIHEP = document.getElementById('item-ihep-' + itemDevId);
                var ihep = elIHEP.value;
                var elDK = document.getElementById('item-dk-' + itemDevId);
                var dk = elDK.value;
                var updateEntry = { ServiceAvailable: isServiceAvailable, IoTHubEndpoint: ihep, DeviceKey: dk };
                $.ajax({
                    url: 'http://' + mobileAppName + '.azurewebsites.net/tables/DeviceEntry/' + itemId,
                    type: 'PATCH',
                    headers: {
                        'Content-Type': 'application/json',
                        'ZUMO-API-VERSION': '2.0.0'
                    },
                    data: JSON.stringify(updateEntry),
                    dataType: 'json',
                    success: function (body) {
                        var result = body;
                    }
                });
            });

            // On initial load, start by fetching the current data
            refreshDeviceEntries(results);

        }

    });

    // Read current data and rebuild UI.
    // If you plan to generate complex UIs like this, consider using a JavaScript templating library.
    function refreshDeviceEntries(deItems) {

        var listItems = $.map(deItems, function (item) {
            return $('<li class="device-entries">')
                .attr('data-deitem-id', item.id).attr('data-deitem-device-id', item.deviceId)
                .append($('<button class="item-update">Update</button>'))
                .append($('<input type="checkbox" class="di-complete">').prop('checked', item.serviceAvailable).attr('id', 'item-sa-' + item.deviceId))
                .append($('<div>').append('<label class="di-text">DeviceId:' + item.deviceId + '</lablel>'))
                .append($('<div>').append('<label class="di-text">IoT Hub Endpoint:</lablel>')
                .append($('<input type="text" class="di-text">').attr('id', 'item-ihep-' + item.deviceId).attr('value', item.iotHubEndpoint)))
                .append($('<div>').append('<label>Device Key:</lablel>')
                .append($('<input type="text" class="di-text">').attr('id', 'item-dk-' + item.deviceId).attr('value', item.deviceKey)));

        });

        $('#device-entries').empty().append(listItems).toggle(listItems.length > 0);
        $('#summary').html('<strong>' + deItems.length + '</strong> entry(s)');
    }

    function handleError(error) {
        var text = error + (error.request ? ' - ' + error.request.status : '');
        $('#errorlog').append($('<li>').text(text));
    }

    function getDEItemId(formElement) {
        return $(formElement).closest('li').attr('data-deitem-id');
    }
    function getDEItemDevId(formElement) {
        return $(formElement).closest('li').attr('data-deitem-device-id');
    }

});