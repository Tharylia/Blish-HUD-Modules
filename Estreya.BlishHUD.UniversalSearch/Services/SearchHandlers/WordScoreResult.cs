namespace Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;

public struct WordScoreResult<T>
{
    public T Result { get; set; }
    public int DiffScore { get; set; }

    public WordScoreResult(T result, int diffScore)
    {
        this.Result = result;
        this.DiffScore = diffScore;
    }
}