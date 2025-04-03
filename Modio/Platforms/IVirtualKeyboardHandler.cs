using System;

namespace Modio.Platforms
{
    public interface IVirtualKeyboardHandler
    {
        void OpenVirtualKeyboard(string title,
                                 string text,
                                 string placeholder,
                                 ModioVirtualKeyboardType virtualKeyboardType,
                                 int characterLimit,
                                 bool multiline,
                                 Action<string> onClose);
    }
}
