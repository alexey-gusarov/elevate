using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;

using TaskScheduler;



namespace Elevate
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				args = new[] { "cmd.exe" };
			}

			if (args.Intersect(new[] { "/?", "-?" }).Any())
			{
				PrintUsage();
				return 2;
			}

			if (string.Equals(args[0], "/task", StringComparison.OrdinalIgnoreCase))
			{
				RunExeInElevatedmode(args[1]);
				return 0;
			}

			if (string.Equals(args[0], CreateTaskArgument, StringComparison.OrdinalIgnoreCase))
			{
				CreateSchedulerTask();
				Console.WriteLine(@"Task registered.");
				return 0;
			}

			ITaskService service = new TaskSchedulerClass();
			service.Connect(null, null, null, null);
			var folder = service.GetFolder("\\");

			IRegisteredTask task;

			try
			{
				task = folder.GetTask(ElevateTask);
			}
			catch (FileNotFoundException)
			{
				Console.WriteLine(@"Elevated task not found. Run with parameter {0}. Press enter to exit.", CreateTaskArgument);
				Console.ReadLine();
				return 1;
			}

			var sb = new StringBuilder();
			sb.AppendFormat("{0}\0", Directory.GetCurrentDirectory());
			foreach (var s in args)
			{
				sb.AppendFormat("{0}\0", s);
			}

			var @params = Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
			task.Run(@params);
			//Main(new string[] { "/task", @params });

			return 0;
		}

		private static void PrintUsage()
		{
			Console.WriteLine(@"el.exe [program] [arguments] [/quoteOff]");
			Console.WriteLine(@"  'program' - program for elevate. If value is omitted then run 'cmd.exe'.");
			Console.WriteLine(@"  'quoteOff' - don't use quotation mark with arguments.");
			Console.Write(@"Press enter to exit.");
			Console.ReadLine();
		}

		private static void CreateSchedulerTask()
		{
			ITaskService service = new TaskSchedulerClass();
			service.Connect(null, null, null, null);
			var folder = service.GetFolder("\\");

			var windowsIdentity = WindowsIdentity.GetCurrent();
			if (windowsIdentity == null)
			{
				throw new InvalidOperationException("Cannot get current windows identity.");
			}

			var taskXml = Task.ElevateTask
				.Replace("$userId$", windowsIdentity.Name)
				.Replace("$exePath$", Assembly.GetExecutingAssembly().Location)
				;

			const int taskCreate = 0x2;
			const int taskUpdate = 0x4;

			folder.RegisterTask(
				ElevateTask,
				taskXml,
				taskCreate | taskUpdate,
				null,
				null,
				_TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN);
		}

		private static void RunExeInElevatedmode(string base64Arg)
		{
			try
			{
				var strings = Encoding.UTF8.GetString(Convert.FromBase64String(base64Arg))
					.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries)
					.ToArray();

				bool quoteOff;

				strings = RemoveQuoteParameter(strings, out quoteOff).ToArray();

				var quote = quoteOff ? "" : "\"";

				var parameters = strings.Skip(2).Aggregate("", (s1, s2) => s1 + quote + s2 + quote + " ", s => s.TrimEnd(' '));

				const int workingDirectoryIndex = 0;
				const int programIndex = 1;

				var fileName = strings[programIndex];
				var workingDirectory = strings[workingDirectoryIndex];

				Console.WriteLine(@"Trying to start program '{0}' with parameters '{1}' in directory '{2}'.", fileName, parameters, workingDirectory);

				var psi = new ProcessStartInfo(fileName, parameters)
				{
					WorkingDirectory = workingDirectory
				};
				Process.Start(psi);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.Write(@"Press enter to exit.");
				Console.ReadLine();
			}
		}

		private static IEnumerable<string> RemoveQuoteParameter(IEnumerable<string> strings, out bool quoteOff)
		{
			quoteOff = false;

			var list = new List<string>();
			foreach (var s in strings)
			{
				if (string.Equals(s, "/elevatedquote", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(s, "/quoteOff", StringComparison.OrdinalIgnoreCase))
				{
					quoteOff = true;
				}
				else
				{
					list.Add(s);
				}
			}
			return list;
		}

		private const string ElevateTask = "\\ElevateTask";
		private const string CreateTaskArgument = "/createTask";
	}
}
