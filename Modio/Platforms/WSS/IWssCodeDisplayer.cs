using System;
using System.Threading.Tasks;

namespace Modio.Platforms.Wss
{
    public interface IWssCodeDisplayer
    {
        /// <summary>
        /// Displays the code prompt to the user.
        /// </summary>
        /// <param name="code">The code to inputted to mod.io/connect</param>
        /// <param name="cancelCallback">Optional callback to invoke when the user cancels the code input.</param>
        /// <returns>A task that completes when the prompt is shown.</returns>
        public Task ShowCodePrompt(string code, Func<Task> cancelCallback = null);

        /// <summary>
        /// Called when the code prompt has been displayed and exited normally.
        /// </summary>
        /// <returns>A task that completes when the prompt is hidden.</returns>
        public Task HideCodePrompt();
    }
}
