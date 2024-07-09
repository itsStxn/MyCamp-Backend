namespace Server.Extensions;

public static class LazyServiceProviderExtensions {
    public static void AddLazyResolution(this IServiceCollection services) {
        services.AddTransient(typeof(Lazy<>), typeof(LazyServiceProvider<>));
    }

    private class LazyServiceProvider<T> : Lazy<T> where T : notnull {
        public LazyServiceProvider(IServiceProvider serviceProvider)
            : base(() => serviceProvider.GetRequiredService<T>()) {}
    }
}

