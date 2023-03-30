namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct WordScoreResult<T>
{

    public T Result { get; set; }
    public int DiffScore { get; set; }

    public WordScoreResult(T result, int diffScore)
    {
        Result = result;
        DiffScore = diffScore;
    }
}
