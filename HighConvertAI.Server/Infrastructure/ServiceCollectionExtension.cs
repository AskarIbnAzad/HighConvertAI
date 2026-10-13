namespace HighConvertAI.Server.Infrastructure;

public static class ServiceCollectionExtension
{
    public static void AddExtractionServices<TService>(this IServiceCollection services)
    {
        var extractorTypes = typeof(TService).Assembly
            .GetTypes()
            .Where(type =>
                typeof(TService).IsAssignableFrom(type) &&
                type.IsClass &&
                !type.IsAbstract);

        foreach (var extractorType in extractorTypes)
        {
            services.AddScoped(typeof(TService), extractorType);
        }
    }
}
