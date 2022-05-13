using System;
using System.Threading.Tasks;

namespace ModIO.Implementation.API
{
	public class RequestHandle<T>
	{
		public Task<T> task;
		public ProgressHandle progress;
		public Action cancel;
	}
}
