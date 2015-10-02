﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using ServiceStack.Text;
using TrueShipAccess.Misc;
using TrueShipAccess.Models;

namespace TrueShipAccess.WebServices
{
	public sealed class WebRequestServices : IWebRequestServices
	{
		private readonly TrueShipConfiguration _config;
		private readonly TrueShipLogger _logservice = new TrueShipLogger();

		public WebRequestServices( TrueShipConfiguration config )
		{
			Condition.Requires( config, "config" ).IsNotNull();
			this._config = config;
		}

		public HttpRequestMessage CreateUpdateOrderItemPickLocationRequest( KeyValuePair< string, PickLocation > oneorderitem )
		{
			var reSerializedOrder = JsonSerializer.SerializeToString( oneorderitem.Value );

			var putApi = new Uri( string.Format( "{0}/{1}?bearer_token={2}",
				this._config.ServiceBaseUri,
				oneorderitem.Key,
				this._config.Credentials.AccessToken ) );

			var request = new HttpRequestMessage( new HttpMethod( "PATCH" ), putApi )
			{
				Content = new StringContent( reSerializedOrder, Encoding.UTF8, "application/json" )
			};

			return request;
		}

		public async Task< T > SubmitGet< T >( string serviceUrl, string querystring, CancellationToken ct ) where T : class
		{
			var request = this.CreateHttpWebRequest( serviceUrl, querystring );

			var response = await GetWrappedAsyncResponse( request, ct );
			var stream = response.GetResponseStream();

			return JsonSerializer.DeserializeFromStream< T >( stream );
		}

		public static Uri MakeAbsoluteUri( string path, string query )
		{
			return new Uri( string.Format( "{0}?{1}", path, query ) );
		}

		public async Task< T > SubmitGet< T >( Uri absoluteUri, CancellationToken ct ) where T : class
		{
			var request = this.CreateHttpWebRequest( absoluteUri );

			var response = await GetWrappedAsyncResponse( request, ct );
			var stream = response.GetResponseStream();

			return JsonSerializer.DeserializeFromStream<T>( stream );
		}

		public T SubmitGetBlocking< T >( string serviceUrl, string querystring ) where T : class
		{
			var request = this.CreateHttpWebRequest( serviceUrl, querystring );

			var response = request.GetResponse();
			var stream = response.GetResponseStream();

			return JsonSerializer.DeserializeFromStream< T >( stream );
		}

		private static async Task< HttpWebResponse > GetWrappedAsyncResponse( HttpWebRequest request, CancellationToken ct )
		{
			using( ct.Register( request.Abort ) )
			{
				try
				{
					var response = await request.GetResponseAsync();
					ct.ThrowIfCancellationRequested();

					return ( HttpWebResponse )response;
				}
				catch( WebException ex )
				{
					if( ct.IsCancellationRequested )
						throw new OperationCanceledException( ex.Message, ex, ct );

					throw;
				}
			}
		}

		private HttpWebRequest CreateHttpWebRequest( string serviceUrl, string querystring )
		{
			var getApi = new Uri( string.Format( "{0}?{1}",
				serviceUrl,
				querystring ) );

			var request = ( HttpWebRequest )WebRequest.Create( getApi );
			request.Method = WebRequestMethods.Http.Get;
			request.ContentType = "application/json";

			return request;
		}

		private HttpWebRequest CreateHttpWebRequest( Uri absoluteUri )
		{
			var request = ( HttpWebRequest ) WebRequest.Create( absoluteUri );
			request.Method = WebRequestMethods.Http.Get;
			request.ContentType = "application/json";

			return request;
		}
	}
}