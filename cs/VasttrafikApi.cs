using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using VasttrafikSharp.Objects;
using System.Xml;
using System.Xml.Serialization;

namespace VasttrafikSharp
{
    public class VasttrafikApi
    {
        public string ApiKey { get; set; }

        public VasttrafikApi(string apiKey)
        {
            this.ApiKey = apiKey;
        }

        public List<VehicleInfo> GetVehicleInfos(string stationId)
        {
            var result = GetStation(stationId, DateTime.Now);

            List<VehicleInfo> vehicleInfos = new List<VehicleInfo>();

            var orderedResult = result.Where(p => p.Value[0].Type != "LOC").OrderBy(p => p.Value[0].Name.Split(' ')[1]);

            foreach (KeyValuePair<string, List<Departure>> kvp in orderedResult)
            {
                if (kvp.Value.Count > 0)
                {
                    VehicleInfo vehicleInfo = new VehicleInfo();
                    vehicleInfo.Number = kvp.Value[0].Name.Split(' ')[1];
                    string direction = kvp.Value[0].Direction;
                    if (direction.Contains(" via "))
                        direction = direction.Substring(0, direction.IndexOf(" via "));

                    vehicleInfo.Destination = direction;
                    vehicleInfo.BackgroundColor = kvp.Value[0].ForegroundColor;
                    vehicleInfo.ForegroundColor = kvp.Value[0].BackgroundColor != "#00abe5" ? kvp.Value[0].BackgroundColor : "#000000";

                    // Calculate time differences
                    // Note: One could check here if RealtimeTime == null => Time is from time table and "ca" could be added
                    if (kvp.Value.Count == 1)
                        vehicleInfo.NextMin = GetMinutesDifference(GetDepartureTime(kvp.Value[0]), true).ToString();
                    else
                    {
                        // Order the times, they might be in wrong order
                        List<Departure> valueList = kvp.Value;
                        List<Departure> orderedValues = valueList.OrderBy(p => GetMinutesDifference(GetDepartureTime(p))).ToList();

                        vehicleInfo.NextMin = GetMinutesDifference(GetDepartureTime(orderedValues[0]), true).ToString();
                        vehicleInfo.NextNextMin = GetMinutesDifference(GetDepartureTime(orderedValues[1]), true).ToString();
                    }
                    vehicleInfos.Add(vehicleInfo);
                }
            }

            return vehicleInfos;
        }

        private int GetMinutesDifference(string time, bool negativeToZero = false)
        {
            DateTime dateTime = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + time);
            int diff = (int)Math.Round(dateTime.Subtract(DateTime.Now).TotalMinutes, MidpointRounding.AwayFromZero);
            
            if (negativeToZero)
                return diff >= 0 ? diff : 0;
            else
                return diff;
        }

        public Dictionary<string, List<Departure>> GetStation(string stationId, DateTime date)
        {
            Dictionary<string, List<Departure>> departureBins = new Dictionary<string, List<Departure>>();

            int attempts = 0;
            string lastDate = date.ToString("yyyy-MM-dd");
            string lastTime = date.ToString("HH:mm");

            while (!AllBinsHaveAtleastTwoItems(departureBins) && attempts < 5)
            {
                attempts++;

                DepartureBoard result = GetStation(stationId, lastDate, lastTime);
                if (result.Departures.Count > 0)
                {
                    lastDate = result.Departures.Last().Date;
                    lastTime = GetDepartureTime(result.Departures.Last());
                }
                PlaceDeparturesInBins(result.Departures, departureBins);
            }

            return departureBins;
        }

        private string GetDepartureTime(Departure departure)
        {
            return departure.RealtimeTime != null ? departure.RealtimeTime : departure.Time;
        }

        private bool AllBinsHaveAtleastTwoItems(Dictionary<string, List<Departure>> departureBins)
        {
            if (departureBins.Count == 0)
                return false;

            foreach (KeyValuePair<string, List<Departure>> kvp in departureBins)
            {
                if (kvp.Value.Count < 2)
                    return false;
            }

            return true;
        }

        private void PlaceDeparturesInBins(List<Departure> departures, Dictionary<string, List<Departure>> departureBins)
        {
            foreach (Departure departure in departures)
            {
                string key = departure.Name + "," + departure.Direction;

                // Don't add departures that has already departed
                // or departures that departs in more than 1 hour
                int minutesFromNow = GetMinutesDifference(GetDepartureTime(departure));
                if (minutesFromNow >= 0 && minutesFromNow < 60)
                {
                    // Add an empty bin if none exists
                    if (!departureBins.ContainsKey(key))
                    {
                        departureBins.Add(key, new List<Departure>());
                    }

                    // Make sure that we don't add any duplicates of journeys (look at the journey id)
                    if (departureBins[key].Where(p => p.JourneyId == departure.JourneyId).Count() == 0)
                    {
                        departureBins[key].Add(departure);
                    }
                }
            }
        }

        public DepartureBoard GetStation(string stationId, string date, string time)
        {
            VasttrafikSharp.Objects.DepartureBoard departureBoard = new DepartureBoard() { Departures = new List<Departure>() };
            try
            {
                Dictionary<string, string> p = new Dictionary<string, string>();
                p.Add("authKey", this.ApiKey);
                p.Add("date", date);
                p.Add("time", time);
                p.Add("id", stationId);

                System.Net.WebResponse response = GetREST("http://api.vasttrafik.se/bin/rest.exe/v1/departureBoard", this.ApiKey, p);

                XmlReader reader = XmlReader.Create(response.GetResponseStream());

                XmlSerializer serializer = new XmlSerializer(typeof(VasttrafikSharp.Objects.DepartureBoard));

                departureBoard = (VasttrafikSharp.Objects.DepartureBoard)serializer.Deserialize(reader);
                reader.Close();
            }
            catch (Exception e)
            {
                departureBoard = new DepartureBoard() { Departures = new List<Departure>() };
            }
            return departureBoard;
        }

        private WebResponse GetREST(string url, string accessToken, Dictionary<string, string> parameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";

            // Build query string
            string queryString = string.Empty;
            foreach (KeyValuePair<string, string> kvp in parameters)
            {
                queryString = queryString + kvp.Key + "=" + kvp.Value + "&";
            }
            queryString = queryString.TrimEnd('&');

            string postData = string.Format(queryString);
            byte[] data = Encoding.UTF8.GetBytes(postData);

            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "application/xml";
            request.ContentLength = data.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(data, 0, data.Length);
            }

            try
            {
                WebResponse response = request.GetResponse();
                return response;
            }
            catch (WebException ex)
            {
                throw ex;
            }
        }
    }
}
