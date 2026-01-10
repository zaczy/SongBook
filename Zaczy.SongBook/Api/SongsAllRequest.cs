using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.Api;

public class SongsAllRequest
{
    public List<SongEntity> Songs { get; set; } = [];
}
