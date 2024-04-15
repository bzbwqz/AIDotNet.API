using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIDotNet.Abstractions.ObjectModels.ObjectModels.ResponseModels;

namespace AIDotNet.Abstractions.Extensions;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> HttpRequestRaw(this HttpClient httpClient, string url,
        object? postData,
        string token)
    {
        HttpRequestMessage req = new(HttpMethod.Post, url);

        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(postData, AIDtoNetJsonSerializer.DefaultOptions);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Add("Authorization", $"Bearer {token}");
        }


        var response = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

        return response;
    }

    public static async Task<HttpResponseMessage> HttpRequestRaw(this HttpClient httpClient, HttpRequestMessage req,
        object? postData)
    {
        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(postData, AIDtoNetJsonSerializer.DefaultOptions);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        var response = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

        return response;
    }

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient httpClient, string url, object? postData,
        string token)
    {
        HttpRequestMessage req = new(HttpMethod.Post, url);

        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(postData, AIDtoNetJsonSerializer.DefaultOptions);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Add("Authorization", $"Bearer {token}");
        }

        return httpClient.SendAsync(req);
    }


    public static async Task<TResponse> PostAndReadAsAsync<TResponse>(this HttpClient client, string uri,
        object? requestModel, CancellationToken cancellationToken = default) where TResponse : BaseResponse, new()
    {
        var response = await client.PostAsJsonAsync(uri, requestModel, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        }, cancellationToken);
        return await HandleResponseContent<TResponse>(response, cancellationToken);
    }

    public static async Task<TResponse> PostFileAndReadAsAsync<TResponse>(this HttpClient client, string uri,
        HttpContent content, CancellationToken cancellationToken = default) where TResponse : BaseResponse, new()
    {
        var response = await client.PostAsync(uri, content, cancellationToken);
        return await HandleResponseContent<TResponse>(response, cancellationToken);
    }

    public static async Task<string> PostFileAndReadAsStringAsync(this HttpClient client, string uri,
        HttpContent content, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsync(uri, content, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken) ?? throw new InvalidOperationException();
    }

    public static async Task<TResponse> DeleteAndReadAsAsync<TResponse>(this HttpClient client, string uri,
        CancellationToken cancellationToken = default) where TResponse : BaseResponse, new()
    {
        var response = await client.DeleteAsync(uri, cancellationToken);
        return await HandleResponseContent<TResponse>(response, cancellationToken);
    }

    private static async Task<TResponse> HandleResponseContent<TResponse>(this HttpResponseMessage response,
        CancellationToken cancellationToken) where TResponse : BaseResponse, new()
    {
        TResponse result;

        if (!response.Content.Headers.ContentType?.MediaType?.Equals("application/json",
                StringComparison.OrdinalIgnoreCase) ?? true)
        {
            result = new()
            {
                Error = new()
                {
                    MessageObject = await response.Content.ReadAsStringAsync(cancellationToken)
                }
            };
        }
        else
        {
            result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken) ??
                     throw new InvalidOperationException();
        }

        result.HttpStatusCode = response.StatusCode;
        return result;
    }
}