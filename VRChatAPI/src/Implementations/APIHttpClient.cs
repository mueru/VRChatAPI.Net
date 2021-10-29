﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VRChatAPI.Extentions.DependancyInjection;
using VRChatAPI.Interfaces;
using VRChatAPI.Logging;
using System.Net;
using VRChatAPI.Utils;

namespace VRChatAPI.Implementations
{
	public class APIHttpClient : IAPIHttpClient, IDisposable
	{
		private readonly HttpClient client;
		private readonly HttpClientHandler handler;
		private readonly IOptions<VRCAPIOptions> option;
		private readonly ILogger logger;

		//public HttpClient Client => client;

		public APIHttpClient(
			IOptions<VRCAPIOptions> option,
			ILogger<APIHttpClient> logger)
		{
			this.option = option;
			this.logger = (ILogger)logger ?? NullLogger.Instance;
			handler = new HttpClientHandler
			{
				UseCookies = true,
			};
			client = new HttpClient(new VRCAPIDelegatingHandler(handler), true);
		}

		#region interface impl
		#region http methods
		public Task<HttpResponseMessage> Delete(string url, CancellationToken ct)
		{
			logger.LogDebug(LogEventID.Delete, "Delete url: {url}", url);
			return client.DeleteAsync(url, ct);
		}
		public Task<TResult> Delete<TResult>(string url, CancellationToken ct) => 
			Deserialize<TResult>(Delete(url, ct));

		public Task<HttpResponseMessage> Get(string url, CancellationToken ct)
		{
			logger.LogDebug(LogEventID.Get, "Get url: {url}", url);
			return client.GetAsync(url, ct);
		}
		public Task<TResult> Get<TResult>(string url, CancellationToken ct) =>
			Deserialize<TResult>(Get(url, ct));

		public Task<HttpResponseMessage> Post(string url, CancellationToken ct)
		{
			logger.LogDebug(LogEventID.Post, "Post url: {url}", url);
			return client.PostAsync(url, default, ct);
		}
		public Task<TResult> Post<TResult>(string url, CancellationToken ct) =>
			Deserialize<TResult>(Post(url, ct));

		public Task<HttpResponseMessage> Post<TContent>(string url, TContent content, CancellationToken ct)
		{
			logger.LogDebug(LogEventID.Post, "Post url: {url}", url);
			return client.PostAsync(url, CreateContent(content), ct);
		}
		public Task<TResult> Post<TResult, TContent>(string url, TContent content, CancellationToken ct) =>
			Deserialize<TResult>(Post(url, content, ct));

		public Task<HttpResponseMessage> Put(string url, CancellationToken ct)
		{
			logger.LogDebug(LogEventID.Put, "Put url: {url}", url);
			return client.PutAsync(url, default, ct);
		}
		public Task<TResult> Put<TResult>(string url, CancellationToken ct) =>
			Deserialize<TResult>(Put(url, ct));

		public Task<HttpResponseMessage> Put<TContent>(string url, TContent content, CancellationToken ct)
		{
			logger.LogDebug(LogEventID.Put, "Put url: {url}", url);
			return client.PutAsync(url, CreateContent(content), ct);
		}
		public Task<TResult> Put<TResult, TContent>(string url, TContent content, CancellationToken ct) =>
			Deserialize<TResult>(Put(url, content, ct));

		public Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken ct)
		{
			logger.LogDebug("Send request: {request}", request);
			return client.SendAsync(request, ct);
		}
		#endregion

		public ITokenCredential GetCredential()
		{
			var c = handler.CookieContainer.GetCookies(new Uri(option.Value.APIEndpointBaseAddress)).OfType<Cookie>();
			return new TokenCredential(c.First(v => v.Name == "auth"), c.First(v => v.Name == "twoFactorAuth"));
		}

		public void Dispose() => client?.Dispose();
		#endregion

		private JsonContent CreateContent<T>(T obj) => 
			JsonContent.Create(obj, options: option.Value.SerializerOption);
		private async Task<T> Deserialize<T>(HttpResponseMessage response) =>
			await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync());
		private async Task<T> Deserialize<T>(Task<HttpResponseMessage> response) =>
			await Deserialize<T>(await response);
	}
}