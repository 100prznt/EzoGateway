self.addEventListener('message', function (e) {
    var data = e.data;
    var executionComplete = true;

    switch (data.cmd) {
        case 'start':
            setInterval(function () {
                if (executionComplete) {
                    executionComplete = false;
                    data = JSON.parse(Get('../../../api/sensor/2/live'));
                    self.postMessage(data.measValue);
                    executionComplete = true;
                }
            }, 50);
            break;
        case 'stop':
            self.close();
            break;
        default:
            self.postMessage('Unknown command: ' + data.cmd);
    };
}, false);

function Get(yourUrl) {
    var Httpreq = new XMLHttpRequest(); // a new request
    Httpreq.open("GET", yourUrl, false);
    Httpreq.send(null);
    return Httpreq.responseText;
}

