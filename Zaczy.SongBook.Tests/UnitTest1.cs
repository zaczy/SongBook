using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Zaczy.SongBook.Chords;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SpiewnikWywrota_Html2Text_ReturnsCorrectText()
        {
            var htmlContent = File.ReadAllText("Piosenki/dds.html");
            var converter = new Html2Text_W(htmlContent);

            var result = converter.ConvertToText();

            Console.WriteLine(result);

            Assert.That(!string.IsNullOrEmpty(result));
        }

        [Test]
        public void SpiewnikWywrota_Html2Song_ReturnsCorrectSong()
        {
            var htmlContent = File.ReadAllText("Piosenki/dds.html");
            var converter = new Html2Text_W(htmlContent);

            var result = converter.ConvertToText();

            var song = Song.CreateFromW(htmlContent);

            Console.WriteLine($"{song.Title}");
            Console.WriteLine($"{song.Artist}");
            if(!string.IsNullOrEmpty(song.Capo))
                Console.WriteLine($"Capo: {song.Capo}");

            Console.WriteLine("-----");

            if (song.Lines != null)
            {
                foreach (var line in song.Lines)
                {
                    Console.WriteLine(line + (Chord.IsChordLine(line) ? "[akordy]" : ""));
                }
            }

            Assert.That(song, Is.Not.Null);
            Assert.That(song.Title, Is.EqualTo(converter.GetTitle()));
            Assert.That(song.Artist, Is.EqualTo(converter.GetArtist()));
            Assert.That(song.Lyrics, Is.EqualTo(result));
        }

        [Test]
        [TestCase(-5)]
        [TestCase(2)]
        public void SpiewnikWywrota_AdjustTonation_ReturnsCorrectSong(int semitones)
        {
            var htmlContent = File.ReadAllText("Piosenki/dds.html");
            var converter = new Html2Text_W(htmlContent);

            var result = converter.ConvertToText();

            var song = Song.CreateFromW(htmlContent);

            Console.WriteLine($"{song.Title}");
            Console.WriteLine($"{song.Artist}");

            song.AdjustTonation(semitones);

            if (!string.IsNullOrEmpty(song.Capo))
                Console.WriteLine($"Capo: {song.Capo}");


            if (song.Lines != null)
            {
                foreach (var line in song.Lines)
                {
                    Console.WriteLine(line + (Chord.IsChordLine(line) ? "[akordy]" : ""));
                }
            }

            Assert.That(song, Is.Not.Null);
            Assert.That(song.Title, Is.EqualTo(converter.GetTitle()));
            Assert.That(song.Artist, Is.EqualTo(converter.GetArtist()));
        }


        [Test]
        public void GuitarChord_CdurToSvg_ReturnsCorrectSvg()
        {
            var chord = new GuitarChord("C", openStrings: new HashSet<int> { 1, 3 }, mutedStrings: new HashSet<int> { 6 });
            chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna 2, palec 1, próg 1
            chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna 4, palec 2, próg 2
            chord.Tones.Add(new GuitarChordTone(5, 3, 3));  // struna 5, palec 3, próg 3

            string svg = chord.ToSvg();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}.svg", svg);

            svg = chord.ToSvgHorizontal();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}_poziom.svg", svg);

            Console.WriteLine(chord.ToString());

            Console.WriteLine(svg);
            Assert.That(!string.IsNullOrEmpty(svg));
        }

        [Test]
        public void GuitarChord_DdurToSvg_ReturnsCorrectSvg()
        {
            var chord = new GuitarChord("D", openStrings: new HashSet<int> { 4 }, mutedStrings: new HashSet<int> { 5, 6 });
            chord.Tones.Add(new GuitarChordTone(1, 2, 2));  // struna 2, palec 1, próg 1
            chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna 4, palec 2, próg 2
            chord.Tones.Add(new GuitarChordTone(3, 1, 2));  // struna 5, palec 3, próg 3

            string svg = chord.ToSvg();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}.svg", svg);

            svg = chord.ToSvgHorizontal();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}_poziom.svg", svg);

            Console.WriteLine(chord.ToString());

            Console.WriteLine(svg);
            Assert.That(!string.IsNullOrEmpty(svg));
        }
        
        [Test]
        public void GuitarChord_FdurToSvg_ReturnsCorrectSvg()
        {
            var chord = new GuitarChord("F");
            chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 2, palec 1, próg 1
            chord.Tones.Add(new GuitarChordTone(4, 4, 3));  // struna 4, palec 2, próg 2
            chord.Tones.Add(new GuitarChordTone(5, 3, 3));  // struna 5, palec 3, próg 3
            chord.Tones.Add(new GuitarChordTone(1));  // struna 1-6, palec 1, próg 1, crossbar

            string svg = chord.ToSvg();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}.svg", svg);

            svg = chord.ToSvgHorizontal();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}_poziom.svg", svg);

            Console.WriteLine(chord.ToString());

            Console.WriteLine(svg);
            Assert.That(!string.IsNullOrEmpty(svg));
        }

        [Test]
        public void GuitarChord_AdurToSvg_ReturnsCorrectSvg()
        {
            var chord = new GuitarChord("A");
            chord.Tones.Add(new GuitarChordTone(2, 3, 2));  // struna 2, palec 1, próg 2
            chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 2, palec 1, próg 2
            chord.Tones.Add(new GuitarChordTone(4, 1, 2));  // struna 2, palec 1, próg 2

            string svg = chord.ToSvg();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}.svg", svg);

            svg = chord.ToSvgHorizontal();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}_poziom.svg", svg);

            Console.WriteLine(chord.ToString());

            Console.WriteLine(svg);
            Assert.That(!string.IsNullOrEmpty(svg));
        }

        [Test]
        public void GuitarChord_BdurToSvg_ReturnsCorrectSvg()
        {
            var chord = new GuitarChord("B");
            chord.Tones.Add(new GuitarChordTone(2, 3, 2));  // struna 2, palec 1, próg 2
            chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 2, palec 1, próg 2
            chord.Tones.Add(new GuitarChordTone(4, 1, 2));  // struna 2, palec 1, próg 2

            chord.TransponeUpBar(1);

            string svg = chord.ToSvg();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}.svg", svg);

            svg = chord.ToSvgHorizontal();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}_poziom.svg", svg);

            Console.WriteLine(chord.ToString());

            Console.WriteLine(svg);
            Assert.That(!string.IsNullOrEmpty(svg));
        }


        [Test]
        public void GuitarChord_FisDurToSvg_ReturnsCorrectSvg()
        {
            var chord = new GuitarChord("F#");
            chord.Tones.Add(new GuitarChordTone(3, 2, 3));  // struna 3, palec 2, próg 3
            chord.Tones.Add(new GuitarChordTone(4, 4, 4));  // struna 4, palec 4, próg 4
            chord.Tones.Add(new GuitarChordTone(5, 3, 4));  // struna 5, palec 3, próg 4
            chord.Tones.Add(new GuitarChordTone(2));  // struna 1-6, palec 1, próg 2, crossbar

            string svg = chord.ToSvg();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}.svg", svg);

            svg = chord.ToSvgHorizontal();
            File.WriteAllText(@$"C:\Tmp\{chord.Name}_poziom.svg", svg);

            Console.WriteLine(chord.ToString());

            Console.WriteLine(svg);
            Assert.That(!string.IsNullOrEmpty(svg));
        }

        [Test]
        public void GuitarChord_ChordsLibrary_ReturnsCorrectVisualisation()
        {

            foreach(var kv in ChordsLibrary.ChordsDict)
            {
                var chord = ChordsLibrary.StandardChord(kv.Key, kv.Value);

                if (chord != null)
                    Console.WriteLine(chord.ToString());
                else
                {
                    Console.WriteLine($"{kv.Key} {kv.Value} - nie znaleziono");
                }
            }

            Assert.Pass();
        }

        [Test]
        public async Task SpiewnikZaczy_ImportFromWord_ParsesTxt()
        {
            // Given
            string connectionString = "Server=localhost;Database=songbook;User=songbook;Password=Qaz43210;";
            string filename = "C:\\Users\\Rafal.Zak\\Dropbox\\Worek\\Spiewnik_content.docx";

            // When
            WordParser wordParser = new WordParser();
            var songs = wordParser.ParseFile(filename);

            var factory = new SongBookDbContextFactory();
            var songRepository = new SongRepository(factory.CreateDbContext(connectionString));


            foreach (var s in songs)
            {
                if(s.Lines?.Count == 0)
                    continue;

                s.Lyrics = string.Join("\n", s.Lines!);

                var songEntity = await songRepository.SearchOnlySongAsync(s);

                if (songEntity == null)
                    await songRepository.AddAsync(s);
                else
                {
                    songEntity.initFromSong(s);
                    await songRepository.UpdateAsync(songEntity);
                }
            }

                Assert.That(songs?.Count > 190);
        }


    }
}
