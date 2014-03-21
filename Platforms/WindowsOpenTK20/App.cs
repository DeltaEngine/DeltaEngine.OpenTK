using DeltaEngine.Core;

namespace DeltaEngine.Platforms
{
	public abstract class App
	{
		private readonly OpenTK20Resolver resolver = new OpenTK20Resolver();

		protected App() {}

		protected App(Window windowToRegister)
		{
			resolver.RegisterInstance(windowToRegister);
		}

		protected void Run()
		{
			resolver.Run();
			resolver.Dispose();
		}

		internal protected T Resolve<T>()
			where T : class
		{
			return resolver.Resolve<T>();
		}
	}
}