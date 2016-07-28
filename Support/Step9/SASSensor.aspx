<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SASSensor.aspx.cs" Inherits="IoTWeb.SASSensor" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>Stored Sensor Data by Stream Analytics</title>
    <link href="visuals.css" rel="stylesheet" />
    <script type="text/javascript" src="Scripts/jquery-1.10.2.min.js" ></script>
    <script type="text/javascript" src="Scripts/powerbi-visuals.all.min.js"></script>
    <style>
        .visual {
            'background-color' : 'white',
            'padding' : '10px',
            'margin' : '5px'
        }
    </style>

    <script type="text/javascript">
        $(function () {
            
            var dataOfDevices = [];
            var dataViews = [];
            var columns = [];
            var graphMetadata = {
                columns: [
                    {
                        displayName: 'Time',
                        isMeasure: true,
                        queryName: 'timestamp',
                        type: powerbi.ValueType.fromDescriptor({ dateTime: true }),
                    },
                ]
            };
            function CreateGraph(items) {
                var count = 0;
                items.sort(function (v1, v2) {
                    return (v1.time > v2.time ? 1 : -1);
                });
                items.forEach(function (item) {
                    if (dataOfDevices.length === 0) {
                        var dataSetOfDevice = { deviceId: item.deviceId, data: [item] };
                        dataOfDevices.push(dataSetOfDevice);
                    }
                    else {
                        var existed = false;
                        var i = 0;
                        for (; i < dataOfDevices.length; i++) {
                            if (dataOfDevices[i].deviceId === item.deviceId) {
                                existed = true;
                                break;
                            }
                        }
                        if (existed) {
                            dataOfDevices[i].data.push(item);
                        }
                        else {
                            var dataSetOfDevice = { deviceId: item.deviceId, data: [item] };
                            dataOfDevices.push(dataSetOfDevice);
                        }
                    }
                    var deviceId = item.deviceId;
                });

                var fieldExpr = powerbi.data.SQExprBuilder.fieldExpr({ column: { entity: "table1", name: "time" } });

                dataOfDevices.forEach(function (dod) {
                    var producedGD = {
                        temperatures: [],
                        timestamps: []
                    };
                    count++;
                    dod.data.forEach(function (itemData) {
                        producedGD.temperatures.push(itemData.temp);
                        var dateTime = new Date(itemData.time);
                        if (!dateTime.replase) {
                            dateTime.replace = ('' + this).replace;
                        }
                        producedGD.timestamps.push(dateTime);
                    });

                    var categoryValues = producedGD.timestamps;
                    var categoryIdentities =
                        categoryValues.map(
                        function (value) {
                            var expr = powerbi.data.SQExprBuilder.equal(fieldExpr, powerbi.data.SQExprBuilder.text(value));
                            return powerbi.data.createDataViewScopeIdentity(expr);
                        }
                    );

                    graphMetadata.columns.push({
                        displayName: dod.deviceId,
                        isMeasure: true,
                        format: "0.00",
                        queryName: dod.deviceId + ':temperature',
                        type: powerbi.ValueType.fromDescriptor({ numeric: true })
                    });

                    columns.push({
                        source: graphMetadata.columns[count],
                        values: producedGD.temperatures
                    });

                    var dataValues = dataViewTransform.createValueColumns(columns);

                    var categoryMetadata = {
                        categories: [{
                            source: graphMetadata.columns[0],
                            values: categoryValues,
                            identity: categoryIdentities
                        }],
                        values: dataValues
                    };

                    var dataView = {
                        metadata: graphMetadata,
                        categorical: categoryMetadata
                    };
                    dataViews.push(dataView);


                });

                var viewport = { height: height, width: width };

                if (visual.update) {
                    // Call update to draw the visual with some data
                    visual.update({
                        dataViews: dataViews,
                        viewport: viewport,
                        duration: 0
                    });
                } else if (visual.onDataChanged && visual.onResizing) {
                    // Call onResizing and onDataChanged (old API) to draw the visual with some data
                    visual.onResizing(viewport);
                    visual.onDataChanged({ dataViews: dataViews });
                }

            }

            function createDefaultStyles() {
                var dataColors = new powerbi.visuals.DataColorPalette();

                return {
                    titleText: {
                        color: { value: 'rgba(51,51,51,1)' }
                    },
                    subTitleText: {
                        color: { value: 'rgba(145,145,145,1)' }
                    },
                    colorPalette: {
                        dataColors: dataColors,
                    },
                    labelText: {
                        color: {
                            value: 'rgba(51,51,51,1)',
                        },
                        fontSize: '11px'
                    },
                    isHighContrast: false,
                };
            }

            var pluginService = powerbi.visuals.visualPluginFactory.create();
            var defaultVisualHostServices = powerbi.visuals.defaultVisualHostServices;
            var width = 600;
            var height = 400;

            var element = $('.visual');
            element.height(height).width(width);


            // Get a plugin
            var visual = pluginService.getPlugin('lineChart').create();

            var dataViewTransform = powerbi.data.DataViewTransform;

            function initializeVisual() {
                powerbi.visuals.DefaultVisualHostServices.initialize();

                visual.init({
                    // empty DOM element the visual should attach to.
                    element: element,
                    // host services
                    host: defaultVisualHostServices,
                    style: createDefaultStyles(),
                    viewport: {
                        height: height,
                        width: width
                    },
                    settings: { slicingEnabled: true },
                    interactivity: { isInteractiveLegend: false, selection: false },
                    animation: { transitionImmediate: true }
                });
            }
            initializeVisual();

            function ShowGraph() {
                var itemCurrentTime = document.getElementById("id-current-time");
                var currentTime = new Date();
                itemCurrentTime.innerHTML = currentTime.toDateString() + " - " + currentTime.toTimeString();
                $.get("/api/SASSensor?during=1", {},
                    function (result) {
                        CreateGraph(result);
                    });
            }

            ShowGraph();
            // Reload sensor data and redraw graph after 10 min
            setInterval(ShowGraph, 600000);
        });
    </script>
</head>
<body>
    <p>SASSensor</p>
    <form id="form1" runat="server">
        <div>
            <p>Stream Analytics Service Stored Sensor Raw Temperature Data</p>
            <p><div id="id-current-time"></div></p>
            <div class="visual" style="position:relative"></div>
        </div>
    </form>
</body>
</html>
