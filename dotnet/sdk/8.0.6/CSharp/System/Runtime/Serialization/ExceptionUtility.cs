using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;

namespace System.Runtime.Serialization;

internal static class ExceptionUtility
{
	public static bool IsFatal(Exception exception)
	{
		while (exception != null)
		{
			if ((exception is OutOfMemoryException && !(exception is InsufficientMemoryException)) || exception is ThreadAbortException)
			{
				return true;
			}
			if (exception is TypeInitializationException || exception is TargetInvocationException)
			{
				exception = exception.InnerException;
				continue;
			}
			if (!(exception is AggregateException))
			{
				break;
			}
			ReadOnlyCollection<Exception> innerExceptions = ((AggregateException)exception).InnerExceptions;
			foreach (Exception item in innerExceptions)
			{
				if (IsFatal(item))
				{
					return true;
				}
			}
			break;
		}
		return false;
	}
}
