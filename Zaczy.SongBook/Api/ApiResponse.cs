using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Api;

/// <summary>
/// Klasa reprezentująca odpowiedź z API
/// </summary>
/// <typeparam name="T">Typ danych odpowiedzi</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Czy żądanie zakończyło się sukcesem
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Kod statusu HTTP
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Dane odpowiedzi (jeśli operacja się powiodła)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Tekst odpowiedzi
    /// </summary>
    public string? PlainTxt { get; set; }

    /// <summary>
    /// Komunikat o błędzie (jeśli operacja się nie powiodła)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Szczegóły błędu (np. treść odpowiedzi z błędem)
    /// </summary>
    public string? ErrorDetails { get; set; }

    public string? RequestUrl { get; set; }
}
