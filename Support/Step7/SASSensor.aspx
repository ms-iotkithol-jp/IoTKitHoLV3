<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SASSensor.aspx.cs" Inherits="IoTWeb.SASSensor" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>Stored Sensor Data by Stream Analytics</title>
    <script type="text/javascript" src="Scripts/jquery-1.10.2.min.js" ></script>
    <script type="text/javascript" src="Scripts/jquery.signalR-2.2.0.min.js"></script>
    <script type="text/javascript" src="SignalR/hubs" ></script>
    <script type="text/javascript">
        $(function () {
            var rows = 0;
            var table = document.getElementById("table1");

            function AddRow(pId,pAccelX,pAccelY,pAccelZ,pTemp,pTime) {
                var row = table.insertRow(++rows);
                var colName = row.insertCell(0);
                colName.innerHTML = pId;
                var colDesc1 = row.insertCell(1);
                colDesc1.innerHTML = pAccelX;
                var colDesc2 = row.insertCell(2);
                colDesc2.innerHTML = pAccelY;
                var colDesc3 = row.insertCell(3);
                colDesc3.innerHTML = pAccelZ;
                var colDesc4 = row.insertCell(4);
                colDesc4.innerHTML = pTemp;
                var colDesc5 = row.insertCell(5);
                colDesc5.innerHTML = pTime;
            }
            $.get("/api/SASSensor", {},
                function (result) {
                    // alert(result);
                    for (var i = 0; i < result.length&&i<50; i++) {
                        var sr = result[i];
                        AddRow(sr.deviceId, sr.accelx, sr.accely, sr.accelz, sr.temp, sr.time);
                    }
                });
            var deviceStatusHub = $.connection.DeviceStatusHub;
            deviceStatusHub.on("Update", function (packet) {
                var ndid=document.getElementById('notice-device-id');
                var nds=document.getElementById('notice-device-status');
                var nt=document.getElementById('notice-time');
                    ndid.innerHTML=packet.DeviceId;
                nds.innerHTML=packet.Status;
                nt.innerHTML=packet.time;
            });
            $.connection.hub.start();

        });
    </script>
</head>
<body>
    <p>SASSensor</p>
    <form id="form1" runat="server">
        <div>
            <p>Device Sensor Alert</p>
            <table>
                <tr><th>DeviceId</th><th>Status</th><th>Time</th></tr>
                <tr><td><div id="notice-device-id"/></td><td><div id="notice-device-status" /></td><td><div id="notice-time" /></td></tr>
            </table>
        </div>
    <div>
        <p>Stream Analytics Service Stored Sensor Data</p>
        <table id="table1" border="1">
        <tr><th>DeviceId</th><th>AccelX</th><th>AccelY</th><th>AccelZ</th><th>Temperature</th><th>Time</th></tr>
    </table>
    </div>
    </form>
</body>
</html>
