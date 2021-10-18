using System;
using System.Collections.Generic;

using System.Globalization;
using CitySimulation.Tools;
using ClosedXML.Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CitySimulation.Generation.Model2
{
 
    public partial class JsonModel
    {
        [JsonProperty("seed")]
        public int Seed { get; set; }

        [JsonProperty("total_time")]
        public int TotalTime { get; set; }

        [JsonProperty("step")]
        public double Step { get; set; }

        [JsonProperty("print_step", NullValueHandling = NullValueHandling.Ignore)]
        public double? PrintStep { get; set; }
        [JsonProperty("trace_step", NullValueHandling = NullValueHandling.Ignore)]
        public double? TraceStep { get; set; }
        [JsonProperty("persons_count_step", NullValueHandling = NullValueHandling.Ignore)]
        public double? PersonsCountStep { get; set; }

        [JsonProperty("print_console", NullValueHandling = NullValueHandling.Ignore)]
        public int? PrintConsole { get; set; }
        [JsonProperty("trace_console", NullValueHandling = NullValueHandling.Ignore)]
        public int? TraceConsole { get; set; }


        [JsonProperty("num_threads")]
        public int NumThreads { get; set; }

        [JsonProperty("location_types")]
        public Dictionary<string, LocationType> LocationTypes { get; set; }

        [JsonProperty("people_types")]
        public Dictionary<string, PeopleType> PeopleTypes { get; set; }

        [JsonProperty("link_loc_people_types")]
        public List<LinkLocPeopleType> LinkLocPeopleTypes { get; set; }

        [JsonProperty("e_to_i_delay")]
        public RandomWeibullParams IncubationToSpreadDelay { get; set; }

        [JsonProperty("i_to_r_delay")]
        public RandomWeibullParams SpreadToImmuneDelay { get; set; }

        [JsonProperty("death_probability")]
        public double DeathProbability { get; set; }
        
        [JsonProperty("transport_types")]
        public Dictionary<string, TransportData> Transport { get; set; }

        [JsonProperty("trans_station_link")]
        public List<TransportStationLink> TransportStationLinks { get; set; }

        [JsonProperty("geozone")]
        public Point Geozone { get; set; }
    }

    public partial class TransportData
    {
        [JsonProperty("speed_mean")]
        public double SpeedMean { get; set; }

        [JsonProperty("speed_std")]
        public double SpeedStd { get; set; }

        [JsonProperty("infection_probability")]
        public double InfectionProbability { get; set; }

        [JsonProperty("num")]
        public int Count { get; set; }
    }

    public partial class TransportStationLink
    {
        [JsonProperty("transport_type")]
        public string TransportType { get; set; }

        [JsonProperty("station_type")]
        public string StationType { get; set; }

        [JsonProperty("minst")]
        public int RouteMinStations { get; set; }

        [JsonProperty("maxst")]
        public int RouteMaxStations { get; set; }

        [JsonProperty("routenum")]
        public int RouteCount { get; set; }

        [JsonProperty("epsilon")]
        public double StationsDistanceDelta { get; set; } = 0.1;
    }

    public partial class RandomWeibullParams
    {
        [JsonProperty("shape")]
        public double Shape { get; set; }

        [JsonProperty("scale")]
        public double Scale { get; set; }
    }

    public partial class LinkLocPeopleType
    {
        [JsonProperty("people_type")]
        public string PeopleType { get; set; }

        [JsonProperty("location_type")]
        public string LocationType { get; set; }

        [JsonProperty("workdays_mean")]
        public double WorkdaysMean { get; set; }

        [JsonProperty("workdays_std")]
        public double WorkdaysStd { get; set; }

        [JsonProperty("holiday_mean")]
        public double HolidayMean { get; set; }

        [JsonProperty("holiday_std")]
        public double HolidayStd { get; set; }

        [JsonProperty("ispermanent")]
        public long Ispermanent { get; set; }

        [JsonProperty("start_mean")]
        public double StartMean { get; set; }

        [JsonProperty("start_std")]
        public double StartStd { get; set; }

        [JsonProperty("duration_mean")]
        public double DurationMean { get; set; }

        [JsonProperty("duration_std")]
        public double DurationStd { get; set; }

        [JsonProperty("income", NullValueHandling = NullValueHandling.Ignore )]
        public List<Income> Income { get; set; }

        public override string ToString()
        {
            return PeopleType + " - " + LocationType;
        }
    }

    public partial class Income
    {
        public const string RatePerFact = "per fact";
        public const string RatePerDay = "per day";
        public const string RatePerMinute = "per minute";
        public const string RatePerHour = "per hour";

        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("summ")]
        public int Summ { get; set; }

        [JsonProperty("rate")]
        public string Rate { get; set; }

    }

    public partial class LocationType
    {
        [JsonProperty("num")]
        public int Num { get; set; }

        [JsonProperty("people_mean")]
        public double PeopleMean { get; set; }

        [JsonProperty("people_std")]
        public double PeopleStd { get; set; }

        [JsonProperty("infection_probability")]
        public double InfectionProbability { get; set; }
    }

    public partial class PeopleType
    {
        [JsonProperty("fraction")]
        public double Fraction { get; set; }
        [JsonProperty("start_infected")]
        public int StartInfected { get; set; }
    }
}
