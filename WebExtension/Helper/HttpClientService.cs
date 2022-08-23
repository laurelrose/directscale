using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace WebExtension.Helper
{
    public interface IHttpClientService
    {
        HttpResponseMessage MakeRequest(HttpRequestMessage request);
        HttpResponseMessage MakeRequestByToken(HttpRequestMessage request, string tokenType, string token);
        HttpResponseMessage MakeRequestByUsername(HttpRequestMessage request, string username, string password);
        HttpResponseMessage PostRequestByUsername(HttpRequestMessage request, string username, string password);
    }
    public class HttpClientService : IHttpClientService
    {
        public HttpResponseMessage MakeRequest(HttpRequestMessage request)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
                    HttpResponseMessage response = httpClient.SendAsync(request).Result;
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public HttpResponseMessage MakeRequestByToken(HttpRequestMessage request, string tokenType, string token)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {                
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add(tokenType, token);
                    httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
                    HttpResponseMessage response = httpClient.SendAsync(request).Result;
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public HttpResponseMessage MakeRequestByUsername(HttpRequestMessage request, string username, string password)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);
                    httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
                    HttpResponseMessage response = httpClient.SendAsync(request).Result;
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public HttpResponseMessage PostRequestByUsername(HttpRequestMessage request, string username, string password)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);
                    HttpResponseMessage response = httpClient.PostAsync(request.RequestUri, request.Content).Result;
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
