using Nest;
using System;

namespace ElasticsearchRestaurantsApiNetcore.Helpers
{
	public class Client
	{
		static string url = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL");
		public static string IndexName = Environment.GetEnvironmentVariable("defaultIndex");
		public static ElasticClient ElasticClient
		{
			get
			{
				Uri node = new Uri(string.IsNullOrWhiteSpace(url) ? "http://localhost:9200" : url);
				ConnectionSettings settings = new ConnectionSettings(node)
				   .DisableDirectStreaming();
				//.EnableTcpStats();

				if (!string.IsNullOrEmpty(IndexName))
					settings = settings.DefaultIndex(IndexName);
				return new ElasticClient(settings);
			}
		}
	}
}
