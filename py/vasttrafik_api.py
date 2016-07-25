import datetime
import urllib
import urllib2
import json
import re

class VasttrafikApi:
    _auth_key = None
    _callback = None

    def __init__(self, auth_key):
        self._auth_key = auth_key

    def get_departures(self, station_id):
        now  = datetime.datetime.now()
        date = now.isoformat()[0:10]
        time = now.isoformat()[11:16]

        return self._get_station(self._auth_key, station_id, date, time)

    def _get_station(self, auth_key, station_id, date, time):
        departure_bins = {}

        attempts = 0
        last_date = date
        last_time = time

        while not self._all_bins_have_atleast_two_items(departure_bins) and attempts < 5:
            attempts += 1

            result = self._get_departure_board(auth_key, station_id, date, time)
            if result is not None and 'DepartureBoard' in result and 'Departure' in result['DepartureBoard']:
                departures = result['DepartureBoard']['Departure']

                if len(departures) > 0:
                    last_departure = departures[-1]

                    last_date = last_departure['date']
                    last_time = self._get_departure_time(last_departure)

                self._place_departures_in_bins(departures, departure_bins)

        return self._process_departures(departure_bins)

    def _get_departure_board(self, auth_key, station_id, date, time):
        parameters = {'authKey': auth_key,
                      'date'   : date,
                      'time'   : time,
                      'id'     : station_id,
                      'format' : 'json',
                     }
        url = 'http://api.vasttrafik.se/bin/rest.exe/v1/departureBoard'
        data = urllib.urlencode(parameters)
        request = urllib2.Request(url + '?' + data)
        try:
            response = urllib2.urlopen(request)
            result = json.load(response)
            return result
        except Exception, e:
            return {}

    def _get_departure_time(self, departure):
        return departure['rtTime'] if 'rtTime' in departure and departure['rtTime'] != None else departure['time']

    def _all_bins_have_atleast_two_items(self, departure_bins):
        if len(departure_bins) == 0:
            return False

        for k in departure_bins:
            if len(departure_bins[k]) < 2:
                return False

        return True

    def _place_departures_in_bins(self, departures, departure_bins):
        for departure in departures:
            key = departure['name'] + "," + departure['direction']

            # Don't add departures that has already departed
            # or departures that departs in more than 1 hour
            minutes_from_now = self._get_minutes_difference(self._get_departure_time(departure), False)
            if minutes_from_now >= 0 and minutes_from_now < 60:
                # Add an empty bin if none exists
                if not key in departure_bins:
                    departure_bins[key] = []

                # Make sure that we don't add any duplicates of journeys (look at the journey id)
                if len([p for p in departure_bins[key] if p['journeyid'] == departure['journeyid']]) == 0:
                    departure_bins[key].append(departure)

    def _get_minutes_difference(self, time, negative_to_zero = False):
        now = datetime.datetime.now()
        date_time = datetime.datetime.now().replace(hour = int(time.split(':')[0]), minute = int(time.split(':')[1]))

        diff = int(round((date_time - now).total_seconds()/60))

        if negative_to_zero:
            return diff if diff >= 0 else 0
        else:
            return diff

    def _process_departures(self, departures):
        vehicle_infos = []
      
        # Turn the associative array to an array of tuples (to be able to filter and sort it)
        departures_array = []
        for k in departures:
            departures_array.append([k, departures[k]])

        filtered_result = [ a for a in departures_array if a[1][0]['name'] != 'LOC' ]
        sorted_result   = sorted(filtered_result, lambda a,b: 1 if int(re.sub("[^0-9]", "", a[1][0]['sname'])) > int(re.sub("[^0-9]", "", b[1][0]['sname'])) else -1)
        
        for result in sorted_result:
            key = result[0]
            if len(result[1]) > 0:
                value = result[1][0]

                vehicle_info = {}
                vehicle_info['number'] = value['sname']
                direction = value['direction']
                if direction.find(" via ") != -1:
                    direction = direction[0:direction.find(" via ")]

                vehicle_info['destination'] = direction
                vehicle_info['fgColor']     = value['fgColor']
                vehicle_info['bgColor']     = value['bgColor'] if value['bgColor'] != "#00abe5" else "#000000"

                # Calculate time differences
                # Note: One could check here if RealtimeTime == None => Time is from time table and "ca" could be added
                if len(result[1]) == 1:
                    vehicle_info['nextMin'] =self._get_minutes_difference(self._get_departure_time(value), True)
                else:
                    # Order the times, they might be in wrong order
                    value_list     = result[1]
                    ordered_values = sorted(value_list, key = lambda p: self._get_minutes_difference(self._get_departure_time(p)))

                    vehicle_info['nextMin']     = self._get_minutes_difference(self._get_departure_time(ordered_values[0]), True)
                    vehicle_info['nextNextMin'] = self._get_minutes_difference(self._get_departure_time(ordered_values[1]), True)

                vehicle_infos.append(vehicle_info)

        return vehicle_infos
