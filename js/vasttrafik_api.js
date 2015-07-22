function VasttrafikApi (authKey, callback) {
    this.authKey  = authKey;
    this.callback = callback;
    var self = this;
    
    function processDepartures(departures) {
        var vehicleInfos = [];
      
        // Turn the associative array to an array of tuples (to be able to filter and sort it)
        var departuresArray = [];
        for (var k in departures) { departuresArray.push([k, departures[k]]); }
        
        var filteredResult = departuresArray.filter(function(a) { return a[1][0]['name'] != 'LOC'; });
        var sortedResult   = filteredResult.sort(function (a,b) { return parseInt(a[1][0]['name'].split(' ')[1]) > parseInt(b[1][0]['name'].split(' ')[1]) ? 1 : -1; });
        
        for (var i = 0; i < sortedResult.length; i++) {
            var key = sortedResult[i][0];
            if (sortedResult[i][1].length > 0) {
                var value = sortedResult[i][1][0];

                var vehicleInfo = {};
                vehicleInfo['number'] = value['name'].split(' ')[1];
                var direction = value['direction'];
                if (direction.indexOf(" via ") != -1)
                    direction = direction.substring(0, direction.indexOf(" via "));

                vehicleInfo['destination'] = direction;
                vehicleInfo['fgColor'] = value['fgColor'];
                vehicleInfo['bgColor'] = value['bgColor'] != "#00abe5" ? value['bgColor'] : "#000000";

                // Calculate time differences
                // Note: One could check here if RealtimeTime == null => Time is from time table and "ca" could be added
                if (sortedResult[i][1].length == 1)
                    vehicleInfo['nextMin'] = getMinutesDifference(getDepartureTime(value), true);
                else {
                    // Order the times, they might be in wrong order
                    var valueList = sortedResult[i][1];
                    var orderedValues = valueList.sort(function(p) { getMinutesDifference(getDepartureTime(p)) });

                    vehicleInfo['nextMin'] = getMinutesDifference(getDepartureTime(orderedValues[0]), true);
                    vehicleInfo['nextNextMin'] = getMinutesDifference(getDepartureTime(orderedValues[1]), true);
                }
                vehicleInfos.push(vehicleInfo);
            }
        }

        self.callback(vehicleInfos);
    }

    this.getStation = function(authKey, stationId, date, time) {
        var departureBins = new Array();

        var attempts = 0;
        var lastDate = date;
        var lastTime = time;

        function fillBins(result) {
            var departures = result['DepartureBoard']['Departure'];
            if (departures.length > 0) {
                var lastDeparture = departures[departures.length-1];
                
                lastDate = lastDeparture['date'];
                lastTime = getDepartureTime(lastDeparture);
            }
            placeDeparturesInBins(departures, departureBins);
            
            if (!allBinsHaveAtleastTwoItems(departureBins) && attempts < 5) {
                attempts++;
                
                getDepartureBoard(authKey, stationId, lastDate, lastTime, fillBins);
            }
            else {
                processDepartures(departureBins);
            }
        }

        getDepartureBoard(authKey, stationId, date, time, fillBins);
    }

    function getDepartureBoard(authKey, stationId, date, time, callback) {
        var parameters = {'authKey': authKey,
                          'date'   : date,
                          'time'   : time,
                          'id'     : stationId,
                          'format' : 'json',
                          };

        $.ajax({
            method:   'get',
            url:      'http://api.vasttrafik.se/bin/rest.exe/v1/departureBoard',
            dataType: 'jsonp',
            jsonp:    'jsonpCallback',
            async:    false,
            data:     parameters,
            
            success: function (data) {
                callback(data);
            },
            
            error: function(xhr, error) {
                console.log(error);
            },
        });
    }

    function getDepartureTime(departure) {
        return departure['rtTime'] != null ? departure['rtTime'] : departure['time'];
    }

    function allBinsHaveAtleastTwoItems(departureBins) {
        if (departureBins.length == 0)
            return false;

        for (var k in departureBins) {
            if (departureBins[k].length < 2)
                return false;
        }

        return true;
    }

    function placeDeparturesInBins(departures, departureBins) {
        for (var k in departures) {
            var departure = departures[k];
            var key = departure['name'] + "," + departure['direction'];

            // Don't add departures that has already departed
            // or departures that departs in more than 1 hour
            var minutesFromNow = getMinutesDifference(getDepartureTime(departure), false);
            if (minutesFromNow >= 0 && minutesFromNow < 60) {
                // Add an empty bin if none exists
                if (!(key in departureBins)) {
                    departureBins[key] = [];
                }

                // Make sure that we don't add any duplicates of journeys (look at the journey id)
                if (departureBins[key].filter(function(p) { return p['journeyid'] == departure['journeyid']}).length == 0) {
                    departureBins[key].push(departure);
                }
            }
        }
    }

    function getMinutesDifference(time, negativeToZero) {
        var now = new Date();
        var dateTime = new Date(now.getFullYear(), now.getMonth(), now.getDate(), time.split(':')[0], time.split(':')[1], 0, 0);
        
        var diff = Math.round((dateTime-now)/1000/60);
        
        if (negativeToZero)
            return diff >= 0 ? diff : 0;
        else
            return diff;
    }
}

VasttrafikApi.prototype.getDepartures = function(stationId)
{
    var now  = new Date();
    var date = now.toISOString().substring(0,10);
    var time = now.toISOString().substring(11,16);

    this.getStation(this.authKey, stationId, date, time);
}

