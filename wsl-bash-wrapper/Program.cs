using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BashWrapper
{
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
                Process p = StartBashAndSaveOutput(bash_args, tmp_win_path, verbose);

                ContinuousCopyFileToStdout(tmp_win_path, () => p.HasExited).Wait();

                var ret = p.ExitCode;
                if (verbose)
                {
                    Console.WriteLine($"BashWrapper: Process exit code {ret}");
                }
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

        /// <summary>
        /// This method will repeatedly query a file to see if it has gotten longer. When it has, it will dump anything new
        /// to the screen. It will also quick checking once the query function handed to it returns true. At that point
        /// it will make sure it has dumped everything in the file out.
        /// </summary>
        /// <param name="tmp_win_path"></param>
        private static async Task ContinuousCopyFileToStdout(string tmp_win_path, Func<bool> stopCheckingFile)
        {
            long lastlength = 0;
            long lastRenderedData = 0;
            var f = new FileInfo(tmp_win_path);

            // Loop till we are done, checking
            while (!stopCheckingFile())
            {
                f.Refresh();
                if (f.Length != lastlength)
                {
                    // File updated. Open it, move to the proper positions, and dump everything we can.
                    lastlength = f.Length;
                    lastRenderedData = await DumpFile(lastRenderedData, f);
                }
                await Task.Delay(100);
            }

            // Done. Make sure nothing is left behind.
            await DumpFile(lastRenderedData, f);
        }

        /// <summary>
        /// Dump the contents of a file to a stream starting from the location given.
        /// </summary>
        /// <param name="lastRenderedData"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        private static async Task<long> DumpFile(long lastRenderedPosition, FileInfo file)
        {
            // Use open/read/readwrite to make sure that we don't get in the way of anyone
            // else that is accessing the file (e.g. bash).
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(fs))
                {
                    fs.Seek(lastRenderedPosition, SeekOrigin.Begin);
                    while (!sr.EndOfStream)
                        Console.WriteLine(await sr.ReadLineAsync());
                    return fs.Position;
                }
            }
        }

        private static Process StartBashAndSaveOutput(string[] args, string tmp_win_path, bool verbose)
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
