using System;

namespace Converter.Application.Abstractions
{
    public interface IFolderPicker
    {
        string? PickFolder(string description);
    }
}
