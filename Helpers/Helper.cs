using Nest;
using System.IO;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ElasticsearchRestaurantsApiNetcore.Helpers
{
    public class Helper
    {
        ElasticClient client = null;
        readonly ILogger _logger = null;
        static string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Config", "index.v7.json");
        public Helper(ElasticClient elasticClient = null, ILogger logger = null)
        {
            client = elasticClient ?? Client.ElasticClient;
            _logger = logger;
        }


        public dynamic GetIndice(string indexName = "")
        {
            if (client == null)
                return null;

            return client.Indices.Get(Indices.Index(indexName));
        }

        public dynamic GetDoc(string id, string indexName = "")
        {
            if (client == null)
                return null;

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = Client.IndexName;
            return client.Get(new DocumentPath<dynamic>(id).Index(Indices.Index(indexName)));
        }

        public async Task<bool> IndexExists(string indexName = "")
        {
            if (client == null)
                return false;

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = Client.IndexName;
            var result = await client.Indices.ExistsAsync(Indices.Index(indexName));
            return result.Exists;
        }

        public async Task<bool> DeleteIndex(string indexName = "")
        {
            if (client == null)
                return false;

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = Client.IndexName;
            var result = await client.Indices.DeleteAsync(Indices.Index(indexName));
            return result.Acknowledged;
        }

        public async Task<bool> InitIndex(string indexName = "")
        {
            if (client == null)
                return false;

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = Client.IndexName;
            if (await IndexExists(indexName))
                return true;

            string json = File.ReadAllText(filePath);
            var result = await client.LowLevel.Indices.CreateAsync<CreateIndexResponse>(indexName, PostData.String(json));
            return result.Acknowledged;
        }


        public async Task IndexDocument(string Id, string json, string indexName = "")
        {
            if (_logger != null)
                _logger.LogInformation("Index document: " + Id);

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = Client.IndexName;

            try
            {
                var response = await client.LowLevel.IndexAsync<BytesResponse>(indexName, Id, PostData.String(json));
                if (_logger != null)
                {
                    if (response.RequestBodyInBytes != null && response.RequestBodyInBytes.Length > 0)
                        _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.RequestBodyInBytes));
                    else
                        _logger.LogInformation("Something error on endpoint: " + JsonConvert.SerializeObject(client.ConnectionSettings.ConnectionPool.Nodes));

                    if (response.Body != null && response.Body.Length > 0)
                        _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.Body));
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error index document: " + ex.Message);
            }
        }

        public async Task RemoveIndexDocument(string Id, string indexName = "")
        {
            if (_logger != null)
                _logger.LogInformation("Remove document: " + Id);

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = Client.IndexName;

            try
            {
                var response = await client.LowLevel.DeleteAsync<BytesResponse>(indexName, Id);
                if (_logger != null)
                {
                    if (response.RequestBodyInBytes != null && response.RequestBodyInBytes.Length > 0)
                        _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.RequestBodyInBytes));

                    if (response.Body != null && response.Body.Length > 0)
                        _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.Body));
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error remove document: " + ex.Message);
            }
        }

        public async Task<bool> BulkIndexDocument(Dictionary<string, string> jsonList, string indexName = "")
        {
            _logger.LogInformation("Bulk index document: " + jsonList.Count);

            if (string.IsNullOrWhiteSpace(indexName))
                indexName = Client.IndexName;

            try
            {
                List<string> json = new List<string>();
                foreach (var item in jsonList)
                {
                    json.Add(JsonConvert.SerializeObject(new { index = new { _index = indexName, _id = item.Key } }));
                    json.Add(item.Value);
                }
                var response = await client.LowLevel.BulkAsync<BytesResponse>(PostData.MultiJson(json));
                if (_logger != null)
                {
                    if (response.RequestBodyInBytes != null && response.RequestBodyInBytes.Length > 0)
                        _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.RequestBodyInBytes));
                    else
                        _logger.LogInformation("Something error on endpoint: " + JsonConvert.SerializeObject(client.ConnectionSettings.ConnectionPool.Nodes));
                }

                if (response.Body != null && response.Body.Length > 0)
                {
                    if (_logger != null)
                        _logger.LogInformation(System.Text.Encoding.UTF8.GetString(response.Body));
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                if (_logger != null)
                    _logger.LogInformation("Error bulk index document: " + ex.Message);
            }

            return false;
        }

    }
}
