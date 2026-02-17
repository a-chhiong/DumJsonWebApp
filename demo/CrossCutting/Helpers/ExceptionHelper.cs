namespace CrossCutting.Helpers;

public static class ExceptionHelper
{
    public static T? Find<T>(Exception? ex) where T : Exception
    {
        while (ex != null)
        {
            if (ex is T match) return match;

            if (ex is AggregateException aggEx)
            {
                foreach (var inner in aggEx.InnerExceptions)
                {
                    var found = Find<T>(inner);
                    if (found != null) return found;
                }
            }
            
            ex = ex.InnerException;
        }
        return null;
    }
}