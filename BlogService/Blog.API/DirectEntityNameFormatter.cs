using Blog.Domain.Events;
using MassTransit;

namespace Blog.API
{
    public class DirectEntityNameFormatter : IEntityNameFormatter
    {
        private readonly IEntityNameFormatter _defaultFormatter;

        public DirectEntityNameFormatter(IEntityNameFormatter defaultFormatter)
        {
            _defaultFormatter = defaultFormatter;
        }

        public string FormatEntityName<T>()
        {
            // Для прямых exchange используем фиксированное имя
            if (typeof(T) == typeof(CombineFileChunksCommand) || typeof(T) == typeof(ConvertVideoCommand))
                return "video-events";

            return _defaultFormatter.FormatEntityName<T>();
        }
    }
}
