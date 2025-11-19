using System;
using Converter.Application.Abstractions;
using Converter.Application.Builders;

namespace Converter
{
    public class TestResolution
    {
        public static void TestCommandBuilder()
        {
            var builder = new ConversionCommandBuilder();
            
            // Тест 1: С разрешением
            var profile1 = new ConversionProfile("Test Profile", "libx264", "aac", "192k", 23);
            var request1 = new ConversionRequest("input.mp4", "output.mp4", profile1, 1920, 1080);
            var command1 = builder.Build(request1);
            Console.WriteLine("Command with resolution:");
            Console.WriteLine(command1);
            Console.WriteLine();
            
            // Тест 2: Без разрешения
            var profile2 = new ConversionProfile("Test Profile", "libx264", "aac", "192k", 23);
            var request2 = new ConversionRequest("input.mp4", "output.mp4", profile2);
            var command2 = builder.Build(request2);
            Console.WriteLine("Command without resolution:");
            Console.WriteLine(command2);
            Console.WriteLine();
            
            // Тест 3: Только высота
            var request3 = new ConversionRequest("input.mp4", "output.mp4", profile1, null, 720);
            var command3 = builder.Build(request3);
            Console.WriteLine("Command with height only:");
            Console.WriteLine(command3);
        }
    }
}