using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Chords;

public class GuitarChord
{
    public GuitarChord(string name, HashSet<int>? mutedStrings= null, HashSet<int>? openStrings= null) 
    { 
        Name = name;
        MutedStrings = mutedStrings ?? new HashSet<int>();
        OpenStrings = openStrings ?? new HashSet<int>();
    }

    public string? Name { get; set; }
    public List<GuitarChordTone> Tones { get; set; } = new List<GuitarChordTone>();

    // Struny, które nie są grane (X) lub są otwarte (O)
    public HashSet<int> MutedStrings { get; set; } = new HashSet<int>();
    public HashSet<int> OpenStrings { get; set; } = new HashSet<int>();

    /// <summary>
    /// Oblicza próg początkowy dla wyświetlania diagramu
    /// Jeśli wszystkie palce mieszczą się w pierwszych fretCount progach, zaczynamy od 1
    /// </summary>
    private int GetStartFret(int fretCount)
    {
        int minFret = Tones.Where(t => t.Fret > 0).Select(t => t.Fret).DefaultIfEmpty(1).Min();
        int maxFret = Tones.Where(t => t.Fret > 0).Select(t => t.Fret).DefaultIfEmpty(1).Max();
        
        return maxFret <= fretCount ? 1 : minFret;
    }

    /// <summary>
    /// Generuje wizualizację akordu gitarowego w formacie ASCII (układ poziomy)
    /// Struna 1 (e) na górze, struna 6 (E) na dole
    /// </summary>
    /// <param name="fretCount">Liczba progów do wyświetlenia</param>
    /// <returns>String zawierający diagram ASCII</returns>
    public string ToAscii(int fretCount = 4)
    {
        var sb = new StringBuilder();
        
        int startFret = GetStartFret(fretCount);
        
        // Nazwa akordu
        if (!string.IsNullOrEmpty(Name))
        {
            sb.AppendLine(Name);
        }
        
        // Znajdź crossbar (barre) jeśli istnieje
        var crossbar = Tones.FirstOrDefault(t => t.Crossbar);
        int crossbarFret = crossbar?.Fret ?? 0;
        int crossbarMinString = 1;
        int crossbarMaxString = 6;
        
        if (crossbar != null)
        {
            var barreStrings = Tones.Where(t => t.Fret == crossbar.Fret).Select(t => t.GuitarString).ToList();
            if (barreStrings.Count > 1)
            {
                crossbarMinString = barreStrings.Min();
                crossbarMaxString = barreStrings.Max();
            }
        }
        
        // Rysuj każdą strunę (od 1 do 6) - struna 1 na górze, struna 6 na dole
        for (int stringNum = 1; stringNum <= 6; stringNum++)
        {
            // Symbol na początku (X, O lub puste)
            if (MutedStrings.Contains(stringNum))
            {
                sb.Append("x ");
            }
            else if (OpenStrings.Contains(stringNum))
            {
                sb.Append("o ");
            }
            else
            {
                sb.Append("  ");
            }
            
            // Siodełko lub numer progu
            if (startFret == 1)
            {
                sb.Append("||");
            }
            else
            {
                sb.Append($"{startFret,2}");
            }
            
            // Progi
            for (int fret = startFret; fret < startFret + fretCount; fret++)
            {
                // Sprawdź czy na tej strunie i progu jest palec
                var tone = Tones.FirstOrDefault(t => t.GuitarString == stringNum && t.Fret == fret && !t.Crossbar);
                
                // Sprawdź czy jest barre na tym progu i strunie
                bool hasBarre = crossbar != null && 
                                crossbarFret == fret && 
                                stringNum >= crossbarMinString && 
                                stringNum <= crossbarMaxString;
                
                if (tone != null)
                {
                    // Palec z numerem lub bez
                    string finger = tone.Finger > 0 ? tone.Finger.ToString() : "●";
                    sb.Append($"-{finger}-|");
                }
                else if (hasBarre)
                {
                    // Barre
                    sb.Append("-■-|");
                }
                else
                {
                    // Puste pole
                    sb.Append("---|");
                }
            }
            
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Generuje wizualizację akordu gitarowego w formacie ASCII (układ pionowy)
    /// </summary>
    /// <param name="fretCount">Liczba progów do wyświetlenia</param>
    /// <returns>String zawierający diagram ASCII</returns>
    public string ToAsciiVertical(int fretCount = 4)
    {
        var sb = new StringBuilder();
        
        int startFret = GetStartFret(fretCount);
        
        // Nazwa akordu (wycentrowana)
        if (!string.IsNullOrEmpty(Name))
        {
            sb.AppendLine($"  {Name}");
        }
        
        // Znajdź crossbar (barre) jeśli istnieje
        var crossbar = Tones.FirstOrDefault(t => t.Crossbar);
        int crossbarFret = crossbar?.Fret ?? 0;
        int crossbarMinString = 1;
        int crossbarMaxString = 6;
        
        if (crossbar != null)
        {
            var barreStrings = Tones.Where(t => t.Fret == crossbar.Fret).Select(t => t.GuitarString).ToList();
            if (barreStrings.Count > 1)
            {
                crossbarMinString = barreStrings.Min();
                crossbarMaxString = barreStrings.Max();
            }
        }
        
        // Symbole nad strunami (X, O)
        sb.Append("  ");
        for (int stringNum = 6; stringNum >= 1; stringNum--)
        {
            if (MutedStrings.Contains(stringNum))
                sb.Append("x ");
            else if (OpenStrings.Contains(stringNum))
                sb.Append("o ");
            else
                sb.Append("  ");
        }
        sb.AppendLine();
        
        // Siodełko lub numer progu
        if (startFret == 1)
        {
            sb.AppendLine("  ============");
        }
        else
        {
            sb.AppendLine($"{startFret,2}------------");
        }
        
        // Rysuj każdy próg
        for (int fret = startFret; fret < startFret + fretCount; fret++)
        {
            sb.Append("  ");
            
            for (int stringNum = 6; stringNum >= 1; stringNum--)
            {
                // Sprawdź czy na tej strunie i progu jest palec
                var tone = Tones.FirstOrDefault(t => t.GuitarString == stringNum && t.Fret == fret && !t.Crossbar);
                
                // Sprawdź czy jest barre na tym progu i strunie
                bool hasBarre = crossbar != null && 
                                crossbarFret == fret && 
                                stringNum >= crossbarMinString && 
                                stringNum <= crossbarMaxString;
                
                if (tone != null)
                {
                    string finger = tone.Finger > 0 ? tone.Finger.ToString() : "●";
                    sb.Append($"{finger} ");
                }
                else if (hasBarre)
                {
                    sb.Append("■ ");
                }
                else
                {
                    sb.Append("| ");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("  -----------");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Generuje wizualizację akordu gitarowego w formacie SVG (układ pionowy)
    /// </summary>
    /// <param name="width">Szerokość diagramu</param>
    /// <param name="height">Wysokość diagramu</param>
    /// <returns>String zawierający kod SVG</returns>
    public string ToSvg(int width = 80, int height = 100)
    {
        var svg = new StringBuilder();
        
        // Wymiary i marginesy
        int marginTop = 25;
        int marginLeft = 15;
        int marginRight = 10;
        int marginBottom = 10;
        
        int gridWidth = width - marginLeft - marginRight;
        int gridHeight = height - marginTop - marginBottom;
        
        int fretCount = 4; // Liczba progów do wyświetlenia
        int stringSpacing = gridWidth / 5; // 6 strun = 5 odstępów
        int fretSpacing = gridHeight / fretCount;
        int circleRadius = 6;
        
        int startFret = GetStartFret(fretCount);
        
        // Nagłówek SVG
        svg.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{height}"" viewBox=""0 0 {width} {height}"">");
        
        // Nazwa akordu
        if (!string.IsNullOrEmpty(Name))
        {
            svg.AppendLine($@"  <text x=""{width / 2}"" y=""12"" text-anchor=""middle"" font-family=""Arial"" font-size=""12"" font-weight=""bold"">{Name}</text>");
        }
        
        // Siodełko (gruba linia na górze jeśli zaczynamy od progu 1)
        if (startFret == 1)
        {
            svg.AppendLine($@"  <rect x=""{marginLeft}"" y=""{marginTop}"" width=""{gridWidth}"" height=""3"" fill=""black""/>");
        }
        else
        {
            // Numer progu początkowego
            svg.AppendLine($@"  <text x=""3"" y=""{marginTop + fretSpacing / 2 + 4}"" font-family=""Arial"" font-size=""10"">{startFret}</text>");
        }
        
        // Rysuj progi (linie poziome)
        for (int i = 0; i <= fretCount; i++)
        {
            int y = marginTop + i * fretSpacing;
            svg.AppendLine($@"  <line x1=""{marginLeft}"" y1=""{y}"" x2=""{marginLeft + gridWidth}"" y2=""{y}"" stroke=""black"" stroke-width=""1""/>");
        }
        
        // Rysuj struny (linie pionowe)
        for (int i = 0; i < 6; i++)
        {
            int x = marginLeft + i * stringSpacing;
            svg.AppendLine($@"  <line x1=""{x}"" y1=""{marginTop}"" x2=""{x}"" y2=""{marginTop + gridHeight}"" stroke=""black"" stroke-width=""1""/>");
        }
        
        // Znajdź crossbar (barre) jeśli istnieje
        var crossbar = Tones.FirstOrDefault(t => t.Crossbar);
        if (crossbar != null)
        {
            int fretOffset = crossbar.Fret - startFret;
            int y = marginTop + fretOffset * fretSpacing + fretSpacing / 2;
            
            // Znajdź zakres strun dla barre
            var barreStrings = Tones.Where(t => t.Fret == crossbar.Fret).Select(t => t.GuitarString).ToList();
            int minString = barreStrings.DefaultIfEmpty(1).Min();
            int maxString = barreStrings.DefaultIfEmpty(6).Max();
            
            // Domyślnie barre od struny 1 do 6 jeśli tylko crossbar
            if (barreStrings.Count <= 1)
            {
                minString = 1;
                maxString = 6;
            }
            
            int x1 = marginLeft + (6 - maxString) * stringSpacing;
            int x2 = marginLeft + (6 - minString) * stringSpacing;

            var barreRadius = circleRadius - 2;

            svg.AppendLine($@"  <rect x=""{x1 - barreRadius}"" y=""{y - barreRadius}"" width=""{x2 - x1 + barreRadius * 2}"" height=""{barreRadius * 2}"" rx=""{barreRadius}"" fill=""black""/>");
        }
        
        // Rysuj palce (czarne kółka z numerami)
        foreach (var tone in Tones.Where(t => !t.Crossbar && t.Fret > 0))
        {
            int stringIndex = 6 - tone.GuitarString; // Odwróć (struna 6 = lewa, struna 1 = prawa)
            int fretOffset = tone.Fret - startFret;
            
            int x = marginLeft + stringIndex * stringSpacing;
            int y = marginTop + fretOffset * fretSpacing + fretSpacing / 2;
            
            // Czarne kółko
            svg.AppendLine($@"  <circle cx=""{x}"" cy=""{y}"" r=""{circleRadius}"" fill=""black""/>");
            
            // Numer palca (jeśli > 0)
            if (tone.Finger > 0)
            {
                svg.AppendLine($@"  <text x=""{x}"" y=""{y + 3}"" text-anchor=""middle"" font-family=""Arial"" font-size=""8"" fill=""white"">{tone.Finger}</text>");
            }
        }
        
        // Rysuj symbole nad strunami (X dla wyciszonych, O dla otwartych)
        for (int stringNum = 1; stringNum <= 6; stringNum++)
        {
            int stringIndex = 6 - stringNum;
            int x = marginLeft + stringIndex * stringSpacing;
            int y = marginTop - 5;
            
            if (MutedStrings.Contains(stringNum))
            {
                // X dla wyciszonej struny
                svg.AppendLine($@"  <text x=""{x}"" y=""{y}"" text-anchor=""middle"" font-family=""Arial"" font-size=""10"">×</text>");
            }
            else if (OpenStrings.Contains(stringNum))
            {
                // O dla otwartej struny
                svg.AppendLine($@"  <text x=""{x}"" y=""{y}"" text-anchor=""middle"" font-family=""Arial"" font-size=""10"">○</text>");
            }
        }
        
        svg.AppendLine("</svg>");
        
        return svg.ToString();
    }

    /// <summary>
    /// Generuje wizualizację akordu gitarowego w formacie SVG (układ poziomy - najniższy próg po lewej)
    /// Struna 1 (e) na górze, struna 6 (E) na dole
    /// </summary>
    /// <param name="width">Szerokość diagramu</param>
    /// <param name="height">Wysokość diagramu</param>
    /// <returns>String zawierający kod SVG</returns>
    public string ToSvgHorizontal(int width = 100, int height = 80)
    {
        var svg = new StringBuilder();
        
        // Wymiary i marginesy
        int marginTop = 20;
        int marginLeft = 7;
        int marginRight = 5;
        int marginBottom = 15;
        
        int gridWidth = width - marginLeft - marginRight;
        int gridHeight = height - marginTop - marginBottom;
        
        int fretCount = 4; // Liczba progów do wyświetlenia
        int fretSpacing = gridWidth / fretCount;
        int stringSpacing = gridHeight / 5; // 6 strun = 5 odstępów
        int circleRadius = 6;
        
        int startFret = GetStartFret(fretCount);
        
        // Nagłówek SVG
        svg.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{height}"" viewBox=""0 0 {width} {height}"" fill=""#777"">");
        
        // Nazwa akordu
        if (!string.IsNullOrEmpty(Name))
        {
            svg.AppendLine($@"  <text x=""{width / 2}"" y=""12"" text-anchor=""middle"" font-family=""Arial"" font-size=""12"" font-weight=""bold"">{Name}</text>");
        }
        
        // Siodełko (gruba linia po lewej jeśli zaczynamy od progu 1)
        if (startFret == 1)
        {
            svg.AppendLine($@"  <rect x=""{marginLeft}"" y=""{marginTop}"" width=""3"" height=""{gridHeight}"" fill=""black""/>");
        }
        else
        {
            // Numer progu początkowego
            svg.AppendLine($@"  <text x=""{marginLeft + fretSpacing / 2}"" y=""{marginTop - 5}"" text-anchor=""middle"" font-family=""Arial"" font-size=""10"">{startFret}</text>");
        }
        
        // Rysuj progi (linie pionowe)
        for (int i = 0; i <= fretCount; i++)
        {
            int x = marginLeft + i * fretSpacing;
            svg.AppendLine($@"  <line x1=""{x}"" y1=""{marginTop}"" x2=""{x}"" y2=""{marginTop + gridHeight}"" stroke=""black"" stroke-width=""1""/>");
        }
        
        // Rysuj struny (linie poziome) - struna 1 (e) na górze, struna 6 (E) na dole
        for (int i = 0; i < 6; i++)
        {
            int y = marginTop + i * stringSpacing;
            svg.AppendLine($@"  <line x1=""{marginLeft}"" y1=""{y}"" x2=""{marginLeft + gridWidth}"" y2=""{y}"" stroke=""black"" stroke-width=""1""/>");
        }
        
        // Znajdź crossbar (barre) jeśli istnieje
        var crossbar = Tones.FirstOrDefault(t => t.Crossbar);
        if (crossbar != null)
        {
            int fretOffset = crossbar.Fret - startFret;
            int x = marginLeft + fretOffset * fretSpacing + fretSpacing / 2;
            
            // Znajdź zakres strun dla barre
            var barreStrings = Tones.Where(t => t.Fret == crossbar.Fret).Select(t => t.GuitarString).ToList();
            int minString = barreStrings.DefaultIfEmpty(1).Min();
            int maxString = barreStrings.DefaultIfEmpty(6).Max();
            
            // Domyślnie barre od struny 1 do 6 jeśli tylko crossbar
            if (barreStrings.Count <= 1)
            {
                minString = 1;
                maxString = 6;
            }
            
            // Struna 1 na górze (index 0), struna 6 na dole (index 5)
            int y1 = marginTop + (minString - 1) * stringSpacing;
            int y2 = marginTop + (maxString - 1) * stringSpacing;
            
            int barCircleRadius = circleRadius-2;

            svg.AppendLine($@"  <rect x=""{x - barCircleRadius}"" y=""{y1 - barCircleRadius}"" width=""{barCircleRadius * 2}"" height=""{y2 - y1 + barCircleRadius * 2}"" rx=""{barCircleRadius}"" fill=""black""/>");
        }
        
        // Rysuj palce (czarne kółka z numerami)
        foreach (var tone in Tones.Where(t => !t.Crossbar && t.Fret > 0))
        {
            // Struna 1 na górze (index 0), struna 6 na dole (index 5)
            int stringIndex = tone.GuitarString - 1;
            int fretOffset = tone.Fret - startFret;
            
            int x = marginLeft + fretOffset * fretSpacing + fretSpacing / 2;
            int y = marginTop + stringIndex * stringSpacing;
            
            // Czarne kółko
            svg.AppendLine($@"  <circle cx=""{x}"" cy=""{y}"" r=""{circleRadius}"" fill=""black""/>");
            
            // Numer palca (jeśli > 0)
            if (tone.Finger > 0)
            {
                svg.AppendLine($@"  <text x=""{x}"" y=""{y + 3}"" text-anchor=""middle"" font-family=""Arial"" font-size=""8"" fill=""white"">{tone.Finger}</text>");
            }
        }
        
        // Rysuj symbole po lewej stronie strun (X dla wyciszonych, O dla otwartych)
        for (int stringNum = 1; stringNum <= 6; stringNum++)
        {
            // Struna 1 na górze (index 0), struna 6 na dole (index 5)
            int stringIndex = stringNum - 1;
            int x = marginLeft - 10 + 5;
            int y = marginTop + stringIndex * stringSpacing + 4 - 1;
            
            if (MutedStrings.Contains(stringNum))
            {
                // X dla wyciszonej struny
                svg.AppendLine($@"  <text x=""{x}"" y=""{y}"" text-anchor=""middle"" font-family=""Arial"" font-size=""10"">×</text>");
            }
            else if (OpenStrings.Contains(stringNum))
            {
                // O dla otwartej struny
                svg.AppendLine($@"  <text x=""{x}"" y=""{y}"" text-anchor=""middle"" font-family=""Arial"" font-size=""10"">○</text>");
            }
        }
        
        svg.AppendLine("</svg>");
        
        return svg.ToString();
    }

    /// <summary>
    /// Transponuje akord w wersji z poprzeczką
    /// </summary>
    /// <param name="semitones"></param>
    public void TransponeUpBar(int semitones)
    {
        bool hasBarr = Tones.Any(t => t.Crossbar == true);

        foreach (var tone in Tones)
        {
            tone.Fret += semitones;
            if (!hasBarr)
                tone.Finger += 1;
        }

        if (!hasBarr)
        {
            Tones.Add(new GuitarChordTone(semitones));
            MutedStrings.Clear();
            OpenStrings.Clear();
        }
    }


public override string ToString()
    {
        return ToAscii();
    }
}
