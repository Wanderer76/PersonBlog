using Microsoft.Extensions.DependencyInjection;

namespace MessageBus
{
    public interface IMessageBusBuilder
    {
        public IServiceCollection Services { get; }

    }
}
