using Polly;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BashWrapper {
    // Run the arguments against bash.
	class Program {
		static void Main( string[] bash_args ) {

            // Determine if --verbose was used as the first argument.
            bool verbose = false;
            if (bash_args.FirstOrDefault() == "--verbose")
            {
                bash_args = bash_args.Skip(1).ToArray();
                verbose = true;
            }

            var tmp_win_path = Path.GetTempFileName();
            try
            {
                // Run bash and cache the error code
                Process p = RunBashAndSaveOutput(bash_args, tmp_win_path, verbose);
                var ret = p.ExitCode;

                // Unfortunately, under some conditions it seems like the file is locked even after the
                // process exits.
                Policy
                    .Handle<IOException>()
                    .WaitAndRetry(new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(10),
                    })
                    .Execute(() => CopyFileToStdout(tmp_win_path));

                // Report back what we saw happen with the process.
                Environment.Exit(ret);
            }
            finally
            {
                // Always clean up after ourselves if possible
                if (File.Exists(tmp_win_path))
                {
                    File.Delete(tmp_win_path);
                }
            }
		}

        /// <summary>
        /// Copy the data from the temp file to the output file.
        /// </summary>
        /// <param name="tmp_win_path"></param>
        private static void CopyFileToStdout(string tmp_win_path)
        {
            var sr = new StreamReader(tmp_win_path);

            while (!sr.EndOfStream)
                Console.WriteLine(sr.ReadLine());
            sr.Close();
        }

        private static Process RunBashAndSaveOutput(string[] args, string tmp_win_path, bool verbose)
        {
            try
            {
                var tmp_wsl_path = ConvertPathToWSL(tmp_win_path);

                var p = new Process();

                var p_args = "";
                bool first = true;
                foreach (var t in args)
                {
                    var a = t;
                    if (!first)
                        p_args += " ";

                    if (a.Contains(" "))
                        a = "\"" + a + "\"";

                    p_args += a;
                    first = false;
                }

                p.StartInfo.FileName = FindBash();
                p.StartInfo.Arguments = p_args + " &> " + tmp_wsl_path;
                p.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

                if (verbose)
                {
                    Console.WriteLine($"BashWrapper: bash executable {p.StartInfo.FileName}");
                    Console.WriteLine($"BashWrapper: bash arguments {p.StartInfo.Arguments}");
                    Console.WriteLine($"BashWrapper: working directory {p.StartInfo.WorkingDirectory}");
                    Console.WriteLine($"BashWrapper: Temporary output file to capture results: {tmp_win_path}");
                }

                p.StartInfo.LoadUserProfile = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                p.Start();
                p.WaitForExit();
                if (verbose)
                {
                    Console.WriteLine($"BashWrapper: bash process exit code: {p.ExitCode}");
                    Console.WriteLine($"BashWrapper: bash CPU time {p.TotalProcessorTime} and wall clock time {p.ExitTime - p.StartTime}");
                }
                return p;
            } catch (Exception e)
            {
                var sb = new StringBuilder();
                foreach(var a in args) { sb.Append($"a "); }
                throw new InvalidOperationException($"Unable to run bash with arguments {sb.ToString()}.", e);
            }
        }

        static string FindBash() {
			var path = Path.GetFullPath( Environment.ExpandEnvironmentVariables( @"%windir%\SysWow64\bash.exe" ) );
			if( File.Exists( path ) )
				return path;

			path = Path.GetFullPath( Environment.ExpandEnvironmentVariables( @"%windir%\sysnative\bash.exe" ) );
			if( File.Exists( path ) )
				return path;

			path = Path.GetFullPath( Environment.ExpandEnvironmentVariables( @"%windir%\System32\bash.exe" ) );
			if( File.Exists( path ) )
				return path;

			throw new Exception("Could not find a path to Bash!");
		}

		static string ConvertPathToWSL( string p ) {
			p = Environment.ExpandEnvironmentVariables( p );

			// TODO: Add support for network paths if possible

			return "/mnt/" + p[0].ToString().ToLower() + "/" + p.Substring( 3 ).Replace( '\\', '/' );
		}
	}
}
