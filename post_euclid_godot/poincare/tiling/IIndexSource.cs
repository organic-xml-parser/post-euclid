namespace PostEuclid.poincare.tiling;

public interface IIndexSource
{
    public int GetNextIndex();
}

public class IndexSource : IIndexSource
{
    private int _currentIndex;
    
    public int GetNextIndex()
    {
        var result = _currentIndex;
        _currentIndex++;
        return result;
    }
}