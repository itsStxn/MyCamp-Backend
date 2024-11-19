namespace Server.Extensions;

public static class LazyServiceProviderExtensions {
	public static void AddLazyResolution(this IServiceCollection services) {
		services.AddTransient(typeof(Lazy<>), typeof(LazyServiceProvider<>));
	}

	private sealed class LazyServiceProvider<T>(IServiceProvider serviceProvider) : Lazy<T>(
		() => serviceProvider.GetRequiredService<T>()
	) where T : notnull {}
}
