using System;
using System.Linq;

namespace MailClient
{
    public static class ExceptionExtensions
    {
        private static readonly Type[] UnhandledExceptions =
            { typeof(StackOverflowException), typeof(OutOfMemoryException) };

        /// <summary>
        /// Test the exception can be handled safely
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        internal static bool NeedNotBeAvoided(this Exception exception) =>
            exception != null && UnhandledExceptions.Any(type => type.IsInstanceOfType(exception));
    }
}
