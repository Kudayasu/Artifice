using Microsoft.Tools.WindowsDevicePortal;
using static Microsoft.Tools.WindowsDevicePortal.DevicePortal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XboxWdpDriver
{
    public class TerminateProcessOperation
    {
        /// <summary>
        /// Main entry point for handling terminating a process
        /// </summary>
        /// <param name="portal">DevicePortal reference for communicating with the device.</param>
        /// <param name="parameters">Parsed command line parameters.</param>
        public static void HandleOperation(DevicePortal portal, ParameterHelper parameters)
        {
            // Get the process ID from the command line parameters
            int processId = int.Parse(parameters.GetParameterValue("pid"));

            // Construct the URL for the DELETE request
            string url = $"/api/taskmanager/process?pid={processId}";

            // Send the DELETE request synchronously using the Result property
            Task deleteTask = portal.DeleteAsync(url);
            deleteTask.Wait();

            Console.WriteLine($"Terminated process with ID {processId}");
        }
    }
}