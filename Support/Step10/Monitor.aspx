<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Monitor.aspx.cs" Inherits="IoTWeb.Monitor" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>IoT HoL Remote Monitoring</title>
    <link href="visuals.css" rel="stylesheet" />
    <script type="text/javascript" src="Scripts/jquery-1.10.2.min.js" ></script>
    <script type="text/javascript" src="Scripts/jquery.signalR-2.2.1.min.js"></script>
    <script type="text/javascript" src="SignalR/hubs"></script>
    <style>
        .visual {
            'background-color' : 'white',
            'padding' : '10px',
            'margin' : '5px'
        }
    </style>

    <script type="text/javascript">
        $(function () {
            var deviceStatusHub = $.connection.DeviceStatusHub;
            var hub = $.connection.hub;
            deviceStatusHub.on("Update", function (packet) {
                var dsTable = document.getElementById('device-status-table');
                var dsEntry = document.getElementById('device-row-' + packet.DeviceId);
                if (dsEntry === null) {
                    var dsEntry = dsTable.insertRow(-1);
                    dsEntry.id = "device-row-" + packet.DeviceId;
                    var dsId = dsEntry.insertCell(-1);
                    var dsAX = dsEntry.insertCell(-1);
                    var dsAY = dsEntry.insertCell(-1);
                    var dsAZ = dsEntry.insertCell(-1);
                    var dsTemp = dsEntry.insertCell(-1);
                    var dsTime = dsEntry.insertCell(-1);
                    dsId.id = "notice-device-id-" + packet.DeviceId;
                    dsAX.id = "notice-device-ax-" + packet.DeviceId;
                    dsAY.id = "notice-device-ay-" + packet.DeviceId;
                    dsAZ.id = "notice-device-az-" + packet.DeviceId;
                    dsTemp.id = "notice-device-temp-" + packet.DeviceId;
                    dsTime.id = "notice-device-time-" + packet.DeviceId;
                    dsId.innerHTML = packet.DeviceId;
                    dsAX.innerHTML = packet.accelxavg;
                    dsAY.innerHTML = packet.accelyavg;
                    dsAZ.innerHTML = packet.accelzavg;
                    dsTemp.innerHTML=packet.tempavg;
                    dsTime.innerHTML = packet.time;
                } else {
                    var ndid = document.getElementById('notice-device-id-'+packet.DeviceId);
                    var ndax = document.getElementById('notice-device-ax-' + packet.DeviceId);
                    var nday = document.getElementById('notice-device-ay-' + packet.DeviceId);
                    var ndaz = document.getElementById('notice-device-az-' + packet.DeviceId);
                    var ndtemp = document.getElementById('notice-device-temp-' + packet.DeviceId);
                    var nt = document.getElementById('notice-device-time-' + packet.DeviceId);
                    ndid.innerHTML = packet.DeviceId;
                    ndax.innerHTML = packet.accelxavg;
                    nday.innerHTML = packet.accelyavg;
                    ndaz.innerHTML = packet.accelzavg;
                    ndtemp.innerHTML = packet.tempavg;
                    nt.innerHTML = packet.time;
                }
            });
            deviceStatusHub.on("Prediction", function (packet) {
                var dsTable = document.getElementById('pdevice-status-table');
                var dsEntry = document.getElementById('pdevice-row-' + packet.DeviceId);
                if (dsEntry === null) {
                    var dsEntry = dsTable.insertRow(-1);
                    dsEntry.id = "pdevice-row-" + packet.DeviceId;
                    var dsId = dsEntry.insertCell(-1);
                    var dsStatus = dsEntry.insertCell(-1);
                    var dsProb = dsEntry.insertCell(-1);
                    var dsTime = dsEntry.insertCell(-1);
                    dsId.id = "pnotice-device-id-" + packet.DeviceId;
                    dsStatus.id = "pnotice-device-status-" + packet.DeviceId;
                    dsProb.id = "pnotice-device-probability-" + packet.DeviceId;
                    dsTime.id = "pnotice-device-time-" + packet.DeviceId;
                    dsId.innerHTML = packet.DeviceId;
                    dsStatus.innerHTML = packet.PredictedTempStatus;
                    dsProb.innerHTML = packet.probability;
                    dsTime.innerHTML = packet.time;
                } else {
                    var ndid = document.getElementById('pnotice-device-id-' + packet.DeviceId);
                    var nds = document.getElementById('pnotice-device-status-' + packet.DeviceId);
                    var ndp = document.getElementById('pnotice-device-probability-' + packet.DeviceId);
                    var nt = document.getElementById('pnotice-device-time-' + packet.DeviceId);
                    ndid.innerHTML = packet.DeviceId;
                    nds.innerHTML = packet.PredictedTempStatus;
                    ndp.innerHTML = packet.probability;
                    nt.innerHTML = packet.time;
                }
            });
            hub.start();

        });
    </script>
</head>
<body>
    <h1>Device Sensor Monitoring</h1>
    <form id="form1" runat="server">
        <div>
    <h2>Statistics from Stream Analytics Processing</h2>
            <p>AVG:Average values are populated by Stream Analytics</p>
            <table id="device-status-table" border="1" >
                <tr><th>DeviceId</th><th>Accel X Avg</th><th>Accel Y Avg</th><th>Accel Z Avg</th><th>Temperature Avg</th><th>Time</th></tr>
            </table>
        </div>
    <h2>Prediction from Machine Learning via Stream Analytics Processing</h2>
        <div>
            <p>Predicted Temperature Status by Accel, DeviceId and Time</p>
            <table id="pdevice-status-table" border="1">
                <tr><th>DeviceId</th><th>Status</th><th>Probability</th><th>Time</th></tr>
            </table>
        </div>
    </form>
</body>
</html>
