using Nest;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ElasticsearchRestaurantsApiNetcore.Helpers
{
    public class SearchHelper
    {
        public static Dictionary<string, object> GetJsonFromQueryString(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            try
            {
                var dict = HttpUtility.ParseQueryString(query);

                Dictionary<string, object> dic = new Dictionary<string, object>();
                foreach (string key in dict.Keys)
                {
                    string[] values = dict.GetValues(key);
                    string tempKey = key;
                    tempKey = tempKey.Replace("[", ".").Replace("]", "");
                    if (values.Length == 1)
                        dic.Add(tempKey, values[0]);
                    else
                        dic.Add(tempKey, values);
                }

                return MakeNestedObject(dic);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static JObject ConvertToParams(string query)
        {
            JObject result = null;

            if (string.IsNullOrWhiteSpace(query))
                return result;

            try
            {
                result = JObject.Parse(query);
            }
            catch (Exception ex) { }

            if (result == null || result.HasValues == false)
                result = JObject.FromObject(GetJsonFromQueryString(query));

            return result;
        }

        static Dictionary<string, object> MakeNestedObject(Dictionary<string, object> qsDictionary)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string key in qsDictionary.Keys)
            {
                if (key.Contains("."))
                {
                    List<string> keysList = key.Split('.').ToList();
                    Dictionary<string, object> lastAddedDictionary = result;
                    while (keysList.Count > 1)
                    {
                        if (!lastAddedDictionary.ContainsKey(keysList[0]))
                        {
                            Dictionary<string, object> temp = new Dictionary<string, object>();
                            lastAddedDictionary[keysList[0]] = temp;
                            lastAddedDictionary = temp;
                        }
                        else
                            lastAddedDictionary = (Dictionary<string, object>)lastAddedDictionary[keysList[0]];
                        keysList.RemoveAt(0);
                    }
                    lastAddedDictionary[keysList[0]] = qsDictionary[key];
                }
                else
                {
                    result.Add(key, qsDictionary[key]);
                }
            }
            return result;
        }





        public static JObject GetGeoSearchParams(JObject body)
        {
            if (body == null || body.HasValues == false)
                return null;

            string index = body.Value<string>("index");
            bool sleep = false;
            try { sleep = body.Value<bool?>("sleep") ?? false; } catch { }
            JProperty index_key = new JProperty("index", string.IsNullOrWhiteSpace(index) ? Client.IndexName : index);
            JProperty sleep_key = new JProperty("sleep", sleep);
            string type = body.Value<string>("type");
            if (type.ToLower() == "circle")
            {
                return new JObject() {
                    { "query", ParamByCircle(body.Value<string>("distance"), ToElasticsearchPoint(body.SelectToken("location"))) },
                    index_key,
                    sleep_key
                };
            }
            else
            {
                return new JObject() {
                    { "query", ParamByRectange(ToElasticsearchPoint(body.SelectToken("top_left")), ToElasticsearchPoint(body.SelectToken("bottom_right"))) },
                    index_key,
                    sleep_key
                };
            }
        }

        public static bool IsIEnumerable(Type type)
        {
            return type.GetInterfaces().Any(x => x.IsGenericType
                   && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        public static JObject ToElasticsearchPoint(object point)
        {
            if (point == null)
                return new JObject() {
                    {"lat", null }, {"lon", null },
                };

            float? lat = null, lon = null;
            Type type = point.GetType();
            if (type == typeof(string))
            {
                var numbers = ((string)point).Split(',').Where(x => float.TryParse(x, out _)).Select(float.Parse).ToList();
                lat = numbers.First();
                lon = numbers.Last();
            }
            else if (type == typeof(JToken) || type == typeof(JObject))
            {
                List<string> lst = new List<string>() { "lon", "lng", "longitude" };
                foreach (var item in lst)
                {
                    lon = (point as JToken).Value<float?>(item);
                    if (lon.HasValue)
                        break;
                }
                lst = new List<string>() { "lat", "latitude" };
                foreach (var item in lst)
                {
                    lat = (point as JToken).Value<float?>(item);
                    if (lat.HasValue)
                        break;
                }
            }
            else if (IsIEnumerable(type))
            {
                var numbers = JArray.FromObject(point).Select(i => i.ToString())
                    .Where(x => float.TryParse(x, out _)).Select(float.Parse).ToList();

                lat = numbers.First();
                lon = numbers.Last();
            }

            if (!IsLatitude(lat))
                lat = null;
            if (!IsLongitude(lon))
                lon = null;

            return new JObject() {
                {"lat", lat }, {"lon", lon },
            };
        }

        public static bool IsLatitude(object num)
        {
            double value = -200;
            try
            {
                value = Convert.ToDouble(num);
            }
            catch { }

            return Math.Abs(value) <= 90;
        }

        public static bool IsLongitude(object num)
        {
            double value = -200;
            try
            {
                value = Convert.ToDouble(num);
            }
            catch { }

            return Math.Abs(value) <= 180;
        }

        public static bool IsLocationEmpty(JToken point, bool force_convert = false)
        {
            if (force_convert)
                point = ToElasticsearchPoint(point);
            return point == null || point.HasValues == false || point.Value<float?>("lat") == null || point.Value<float?>("lon") == null;
        }

        public static JObject ParamByRectange(JToken top_left, JToken bottom_right)
        {
            if (IsLocationEmpty(top_left))
                return new JObject() { { "error", "Missing top left point." } };

            if (IsLocationEmpty(bottom_right))
                return new JObject() { { "error", "Missing bottom right point." } };

            return new JObject() {
                {
                    "geo_bounding_box", new JObject() {
                        {
                            "location", new JObject() {
                                { "top_left", top_left },
                                { "bottom_right", bottom_right }
                            }
                        }
                    }
                }
            };
        }

        public static JObject ParamByCircle(string distance, JToken location)
        {
            if (IsLocationEmpty(location))
                return new JObject() { { "error", "Missing center point." } };

            return new JObject() {
                {
                    "geo_distance", new JObject() {
                        {
                            "distance", distance
                        },
                        {
                            "location", location
                        }
                    }
                }
            };
        }
    }
}
