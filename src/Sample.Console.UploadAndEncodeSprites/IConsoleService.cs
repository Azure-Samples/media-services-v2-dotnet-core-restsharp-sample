using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample.Console.UploadAndEncodeSprites
{
    public interface IConsoleService
    {
        /// <summary>
        /// Console app that upload and encode a file.
        /// </summary>
        /// <param name="sourceFiles"></param>
        /// <returns>Job Id.</returns>
        Task<string> UploadAndEncodeAsync(List<string> sourceFiles);
    }
}