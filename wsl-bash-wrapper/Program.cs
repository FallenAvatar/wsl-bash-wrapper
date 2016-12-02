using System;
using System.Diagnostics;
using System.IO;

namespace BashWrapper {
	class Program {
		static void Main( string[] args ) {
            try
            {
                var tmp_win_path = Path.GetTempFileName();
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
                p.StartInfo.Arguments = p_args + " > " + tmp_wsl_path;

                p.StartInfo.WorkingDirectory = ConvertPathToWSL(Directory.GetCurrentDirectory());
                p.StartInfo.LoadUserProfile = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                p.Start();
                p.WaitForExit();

                var ret = p.ExitCode;
                var sr = new StreamReader(tmp_win_path);

                while (!sr.EndOfStream)
                    Console.WriteLine(sr.ReadLine());

                sr.Close();
                File.Delete(tmp_win_path);

                Console.WriteLine($"Return code from bash: {ret}.");

                Environment.Exit(ret);
            } catch (Exception e)
            {
                Console.WriteLine($"Failed to run bash with error code: {e.Message}");
                throw;
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
