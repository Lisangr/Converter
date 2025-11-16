using System;

namespace Converter.Application.Abstractions
{
    public interface IFilePicker
    {
        string[] PickFiles(string title, string filter);
    }
}
