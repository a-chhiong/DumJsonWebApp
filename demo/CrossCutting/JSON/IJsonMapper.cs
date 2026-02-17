namespace CrossCutting.JSON;

public interface IJsonMapper
{
    public TTarget? Map<TSource, TTarget>(TSource? src);
    public List<TTarget>? MapList<TSource, TTarget>(IEnumerable<TSource>? src);

}