using Serilog;
using Serilog.Events;
using Serilog.Sinks.RichTextBox.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Artifice
{
    public class Log
    {
        private readonly RichTextBox _richTextBox;

        public Log(RichTextBox richTextBox)
        {
            _richTextBox = richTextBox;
        }

        public async Task DebugAsync(string message)
        {
            // Add a delay before updating the RichTextBox control
            await Task.Delay(50);

            // Update the content of the RichTextBox control using the Dispatcher
            _richTextBox.Dispatcher.Invoke(() =>
            {
                // Create a logger that writes to the RichTextBox control with the ColoredConsoleTheme
                var logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.RichTextBox(_richTextBox, theme: RichTextBoxConsoleTheme.Colored)
                    .CreateLogger();

                // Write a debug log event to the RichTextBox control
                logger.Debug(message);
            });
        }

        public async Task InformationAsync(string message)
        {
            // Add a delay before updating the RichTextBox control
            await Task.Delay(50);

            // Update the content of the RichTextBox control using the Dispatcher
            _richTextBox.Dispatcher.Invoke(() =>
            {
                // Create a logger that writes to the RichTextBox control with the ColoredConsoleTheme
                var logger = new LoggerConfiguration()
                    .WriteTo.RichTextBox(_richTextBox, theme: RichTextBoxConsoleTheme.Colored)
                    .CreateLogger();

                // Write an information log event to the RichTextBox control
                logger.Information(message);
            });
        }

        public async Task WarningAsync(string message)
        {
            // Add a delay before updating the RichTextBox control
            await Task.Delay(50);

            // Update the content of the RichTextBox control using the Dispatcher
            _richTextBox.Dispatcher.Invoke(() =>
            {
                // Create a logger that writes to the RichTextBox control with the ColoredConsoleTheme
                var logger = new LoggerConfiguration()
                    .WriteTo.RichTextBox(_richTextBox, theme: RichTextBoxConsoleTheme.Colored)
                    .CreateLogger();

                // Write a warning log event to the RichTextBox control
                logger.Warning(message);
            });
        }

        public async Task ErrorAsync(string message)
        {
            // Add a delay before updating the RichTextBox control
            await Task.Delay(50);

            // Update the content of the RichTextBox control using the Dispatcher
            _richTextBox.Dispatcher.Invoke(() =>
            {
                // Create a logger that writes to the RichTextBox control with the ColoredConsoleTheme
                var logger = new LoggerConfiguration()
                    .WriteTo.RichTextBox(_richTextBox, theme: RichTextBoxConsoleTheme.Colored)
                    .CreateLogger();

                // Write an error log event to the RichTextBox control
                logger.Error(message);
            });
        }
    }
}
