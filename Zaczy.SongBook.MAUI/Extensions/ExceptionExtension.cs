using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;

namespace Zaczy.SongBook.MAUI.Extensions;

public static class ExceptionExtension
{
    public static async Task SaveExceptionToFileAsync(this Exception ex, string eventPostfix = "", string moreDetails = "", EventApi? eventApi=null)
    {
        string filename = $"Exception_{ex.GetType().Name}" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string message = $"Wyjątek: {ex.Message}\n";

        Exception? inner = ex?.InnerException;

        while (inner != null)
        {
            message += $"{inner.Message}\n";
            inner = inner?.InnerException;
        }

        message += $"Source: {ex?.Source}\n\n"
                + $"Source: {ex?.StackTrace}\n";

        if (!string.IsNullOrEmpty(moreDetails))
            message += $"\nSzczegóły\n: {moreDetails}";

        if(eventApi != null)
            await eventApi.SendEventAsync($"exception{eventPostfix}", message);
    }
}
