using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Zaczy.SongBook.Extensions;

namespace Zaczy.SongBook.Api;

/// <summary>
/// Klasa bazowa do wysy³ania ¿¹dañ HTTP do API
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly int _timeout;

    public HttpMethod Method { get; set; } = HttpMethod.Post;

    /// <summary>
    /// Inicjalizuje now¹ instancjê klasy <see cref="ApiClient"/>.
    /// </summary>
    /// <param name="baseUrl">Bazowy adres URL API</param>
    /// <param name="timeout">Timeout ¿¹dania w milisekundach (domyœlnie 5000ms)</param>
    public ApiClient(string baseUrl, int timeout = 5000)
    {
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _timeout = timeout;
        
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(timeout)
        };
    }

    /// <summary>
    /// Wysy³a ¿¹danie POST z danymi JSON do okreœlonego endpointu API
    /// </summary>
    /// <typeparam name="TRequest">Typ obiektu ¿¹dania</typeparam>
    /// <typeparam name="TResponse">Typ odpowiedzi (lub object jeœli nieznany)</typeparam>
    /// <param name="endpoint">Endpoint API (bez wiod¹cego /)</param>
    /// <param name="data">Dane do wys³ania</param>
    /// <param name="headers">Opcjonalne dodatkowe nag³ówki</param>
    /// <param name="sendAsForm">Okreœla czy dane maj¹ byæ wys³ane jako formularz</param>
    /// <param name="acceptHeader">Opcjonalny nag³ówek Accept, okreœlaj¹cy oczekiwany format odpowiedzi (np. "application/json")</param>
    /// <returns>OdpowiedŸ z serwera lub null w przypadku b³êdu</returns>
    public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest data,
        Dictionary<string, string>? headers = null,
        bool sendAsForm = false,
        string? acceptHeader = null)
    {
        string url = $"{_baseUrl}/{endpoint.TrimStart('/')}";

        HttpContent content;

        if (sendAsForm)
        {
            if (data is Dictionary<string, string> formData)
            {
                content = new FormUrlEncodedContent(formData);
            }
            else
            {
                var dict = Model2Dictionary(data);
                content = new FormUrlEncodedContent(dict);
            }
        }
        else
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }
            catch (JsonException ex)
            {
                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = $"B³¹d serializacji danych: {ex.Message}"
                };
            }
        }

        try
        {
            // Ustaw nag³ówek Accept, jeœli zosta³ podany
            if (!string.IsNullOrEmpty(acceptHeader))
            {
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));
            }
            // Domyœlny nag³ówek Accept, jeœli nie podano innego
            else
            {
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }


            // Zastosuj dodatkowe nag³ówki, jeœli zosta³y przekazane
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (string.Equals(header.Key, "Accept", StringComparison.OrdinalIgnoreCase))
                    {
                        // U¿ytkownik poda³ nag³ówek Accept w s³owniku headers - u¿yj go zamiast domyœlnego
                        _httpClient.DefaultRequestHeaders.Accept.Clear();
                        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header.Value));
                    }
                    else
                    {
                        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            HttpResponseMessage? response = null;

            if (Method.Equals(HttpMethod.Put))
            {
                response = await _httpClient.PutAsync(url, content);
            }
            else if (Method.Equals(HttpMethod.Patch))
            {
                response = await _httpClient.PatchAsync(url, content);
            }
            else if (Method.Equals(HttpMethod.Post))
            {
                response = await _httpClient.PostAsync(url, content);
            }

            // Przygotuj obiekt odpowiedzi
            var apiResponse = new ApiResponse<TResponse>() { RequestUrl = url };

            if (response != null)
            {
                apiResponse.IsSuccess = response.IsSuccessStatusCode;
                apiResponse.StatusCode = (int)response.StatusCode;
            }

            if (response?.IsSuccessStatusCode == true)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                apiResponse.PlainTxt = responseBody;

                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    try
                    {
                        // Uwzglêdniamy format odpowiedzi na podstawie Content-Type
                        var contentType = response.Content.Headers.ContentType?.MediaType;

                        if (typeof(TResponse) == typeof(string))
                        {
                            apiResponse.Data = (TResponse)(object)responseBody;
                        }
                        else if (contentType != null && contentType.Contains("application/json"))
                        {
                            apiResponse.Data = JsonSerializer.Deserialize<TResponse>(responseBody, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                        }
                        else
                        {
                            // Gdy format odpowiedzi nie jest JSON, spróbujemy dopasowaæ do typu TResponse
                            // Jeœli TResponse to string, u¿yjemy responseBody bezpoœrednio
                            if (typeof(TResponse) == typeof(string))
                            {
                                apiResponse.Data = (TResponse)(object)responseBody;
                            }
                            else
                            {
                                // W przeciwnym razie próbujemy deserializacji jako JSON (fallback)
                                apiResponse.Data = JsonSerializer.Deserialize<TResponse>(responseBody);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        apiResponse.Data = default;
                        apiResponse.IsSuccess = false;
                        apiResponse.ErrorMessage = $"B³¹d deserializacji odpowiedzi: {ex.Message}";
                    }
                }
            }
            else
            {
                if (response != null)
                {
                    apiResponse.ErrorMessage = $"HTTP Error: {(int)response.StatusCode} {response.ReasonPhrase}";

                    string errorContent = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errorContent))
                    {
                        apiResponse.ErrorDetails = errorContent;
                    }
                }
                else
                {
                    apiResponse.ErrorMessage = "Nieznany b³¹d: odpowiedŸ serwera jest pusta.";
                }
            }

            return apiResponse;
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<TResponse> 
            {
                RequestUrl = url,
                IsSuccess = false,
                ErrorMessage = $"Przekroczono limit czasu ¿¹dania ({_timeout}ms)"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<TResponse>
            {
                RequestUrl = url,
                IsSuccess = false,
                ErrorMessage = $"B³¹d podczas wysy³ania ¿¹dania: {ex.Message}"
            };
        }
        finally
        {
            // Czyœæ nag³ówki po ¿¹daniu
            _httpClient.DefaultRequestHeaders.Clear();
        }
    }

    /// <summary>
    /// Wysy³a ¿¹danie POST z danymi JSON do okreœlonego endpointu API bez oczekiwania na typowan¹ odpowiedŸ
    /// </summary>
    /// <typeparam name="TRequest">Typ obiektu ¿¹dania</typeparam>
    /// <param name="endpoint">Endpoint API (bez wiod¹cego /)</param>
    /// <param name="data">Dane do wys³ania</param>
    /// <param name="headers">Opcjonalne dodatkowe nag³ówki</param>
    /// <returns>Status operacji</returns>
    public async Task<ApiResponse<object>> PostAsync<TRequest>(string endpoint, TRequest data, Dictionary<string, string>? headers = null)
    {
        return await PostAsync<TRequest, object>(endpoint, data, headers);
    }

    /// <summary>
    /// Wysy³a surowy tekst JSON jako ¿¹danie POST do okreœlonego endpointu API
    /// </summary>
    /// <param name="endpoint">Endpoint API</param>
    /// <param name="jsonData">Tekst JSON do wys³ania</param>
    /// <param name="headers">Opcjonalne dodatkowe nag³ówki</param>
    /// <returns>Status operacji</returns>
    public async Task<ApiResponse<object>> PostJsonAsync(string endpoint, string jsonData, Dictionary<string, string>? headers = null)
    {
        try
        {
            string url = $"{_baseUrl}/{endpoint.TrimStart('/')}";

            using var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Zastosuj dodatkowe nag³ówki, jeœli zosta³y przekazane
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Wyœlij ¿¹danie POST i pobierz odpowiedŸ
            HttpResponseMessage response;

            if(Method.Equals(HttpMethod.Put))
            {
                response = await _httpClient.PutAsync(url, content);
            }
            else if (Method.Equals(HttpMethod.Patch))
            {
                response = await _httpClient.PatchAsync(url, content);
            }
            else
            {
                response = await _httpClient.PostAsync(url, content);
            }

            // Przygotuj obiekt odpowiedzi
            var apiResponse = new ApiResponse<object>
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode
            };

            if (!response.IsSuccessStatusCode)
            {
                apiResponse.ErrorMessage = $"HTTP Error: {(int)response.StatusCode} {response.ReasonPhrase}";

                // Próbuj odczytaæ szczegó³y b³êdu z treœci odpowiedzi
                string errorContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(errorContent))
                {
                    apiResponse.ErrorDetails = errorContent;
                }
            }
            else
            {
                // Odczytaj treœæ odpowiedzi, jeœli ¿¹danie siê powiod³o
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    apiResponse.PlainTxt = responseBody;

                    try
                    {
                        apiResponse.Data = JsonSerializer.Deserialize<object>(responseBody);
                    }
                    catch (JsonException ex)
                    {
                        // Jeœli nie mo¿na zdeserializowaæ, pozostaw PlainTxt jako treœæ odpowiedzi
                        apiResponse.Data = null;
                        apiResponse.IsSuccess = false;
                        apiResponse.ErrorMessage = $"B³¹d deserializacji odpowiedzi: " + ex.Message;
                    }
                }
            }
            return apiResponse;
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<object>
            {
                IsSuccess = false,
                ErrorMessage = $"Przekroczono limit czasu ¿¹dania ({_timeout}ms)"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object>
            {
                IsSuccess = false,
                ErrorMessage = $"B³¹d podczas wysy³ania ¿¹dania: {ex.Message}"
            };
        }
        finally
        {
            // Czyœæ nag³ówki po ¿¹daniu
            _httpClient.DefaultRequestHeaders.Clear();
        }
    }

    /// <summary>
    /// Pobiera dane z okreœlonego endpointu API za pomoc¹ ¿¹dania GET
    /// </summary>
    /// <typeparam name="TResponse">Typ odpowiedzi (lub object jeœli nieznany)</typeparam>
    /// <param name="endpoint">Endpoint API (bez wiod¹cego /)</param>
    /// <param name="queryParams">Opcjonalne parametry zapytania</param>
    /// <param name="headers">Opcjonalne dodatkowe nag³ówki</param>
    /// <returns>OdpowiedŸ z serwera</returns>
    public async Task<ApiResponse<TResponse>> GetAsync<TResponse>(string endpoint, Dictionary<string, string>? queryParams = null, Dictionary<string, string>? headers = null)
    {
        try
        {
            string url = $"{_baseUrl}/{endpoint.TrimStart('/')}";
        
            // Dodaj parametry zapytania do URL, jeœli zosta³y przekazane
            if (queryParams != null && queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            }
        
            // Zastosuj dodatkowe nag³ówki, jeœli zosta³y przekazane
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        
            HttpResponseMessage response = await _httpClient.GetAsync(url);
        
            var apiResponse = new ApiResponse<TResponse>
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode
            };
        
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
            
                apiResponse.PlainTxt = responseBody;
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    try
                    {
                        apiResponse.Data = JsonSerializer.Deserialize<TResponse>(responseBody, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                    catch (JsonException ex)
                    {
                        apiResponse.Data = default;
                        apiResponse.IsSuccess = false;
                        apiResponse.ErrorMessage = $"B³¹d deserializacji odpowiedzi: {ex.Message}";
                    }
                }
            }
            else
            {
                apiResponse.ErrorMessage = $"HTTP Error: {(int)response.StatusCode} {response.ReasonPhrase}";
            
                string errorContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(errorContent))
                {
                    apiResponse.ErrorDetails = errorContent;
                }
            }
        
            return apiResponse;
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<TResponse>
            {
                IsSuccess = false,
                ErrorMessage = $"Przekroczono limit czasu ¿¹dania ({_timeout}ms)"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<TResponse>
            {
                IsSuccess = false,
                ErrorMessage = $"B³¹d podczas wysy³ania ¿¹dania: {ex.Message}"
            };
        }
        finally
        {
            // Czyœæ nag³ówki po ¿¹daniu
            _httpClient.DefaultRequestHeaders.Clear();
        }
    }

    /// <summary>
    /// Pobiera dane z okreœlonego endpointu API za pomoc¹ ¿¹dania GET bez oczekiwania na typowan¹ odpowiedŸ
    /// </summary>
    /// <param name="endpoint">Endpoint API (bez wiod¹cego /)</param>
    /// <param name="queryParams">Opcjonalne parametry zapytania</param>
    /// <param name="headers">Opcjonalne dodatkowe nag³ówki</param>
    /// <returns>Status operacji</returns>
    public async Task<ApiResponse<object>> GetAsync(string endpoint, Dictionary<string, string>? queryParams = null, Dictionary<string, string>? headers = null)
    {
        return await GetAsync<object>(endpoint, queryParams, headers);
    }

    /// <summary>
    /// Konwertuje obiekt na Dictionary zawieraj¹cy nazwy pól i ich wartoœci dla pól ró¿nych od domyœlnych
    /// </summary>
    /// <param name="model">Obiekt do konwersji</param>
    /// <returns>Dictionary z nazwami pól i ich wartoœciami</returns>
    public static Dictionary<string, string> Model2Dictionary(object? model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var result = new Dictionary<string, string>();
        var type = model.GetType();
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(model);

            if (value == null)
                continue;

            bool isDefaultValue = false;

            // Jeœli nie ma instancji domyœlnej, sprawdŸ typowe wartoœci domyœlne
            if (property.PropertyType == typeof(int) || property.PropertyType == typeof(long) ||
                property.PropertyType == typeof(short) || property.PropertyType == typeof(byte))
                isDefaultValue = Convert.ToInt64(value) == 0;
            else if (property.PropertyType == typeof(bool))
                isDefaultValue = !(bool)value; // false jest wartoœci¹ domyœln¹ dla bool
            else if (property.PropertyType == typeof(DateTime))
                isDefaultValue = (DateTime)value == default;
            else
                isDefaultValue = false; 

            if (!isDefaultValue)
            {
                string stringValue;

                if (value is DateTime dateTime)
                    stringValue = dateTime.ToString("o"); // ISO 8601 format
                else
                    stringValue = value.ToString() ?? string.Empty;

                var attr = property.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true).FirstOrDefault() as JsonPropertyNameAttribute;

                string propertyName = attr != null ? attr.Name : property.Name.CamelCase();

                result[propertyName] = stringValue;
            }
        }

        return result;
    }


}