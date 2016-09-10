using System;
using System.Diagnostics;
using System.IO;

namespace BashWrapper {
	class Program {
		static void Main( string[] args ) {
			var tmp_win_path = Path.GetTempFileName();
			var tmp_wsl_path = ConvertPathToWSL( tmp_win_path );

			var p = new Process();

			var p_args = "";
			bool first = true;
			foreach( var t in args ) {
				var a = t;
				if( !first )
					p_args += " ";

				if( a.Contains( " " ) )
					a = "\"" + a + "\"";

				p_args += a;
				first = false;
			}

			var bash_path = @"%windir%\System32\bash.exe";

			p.StartInfo.FileName = Path.GetFullPath( Environment.ExpandEnvironmentVariables( bash_path ) );
			p.StartInfo.Arguments = p_args + " > " + tmp_wsl_path;

			p.StartInfo.WorkingDirectory = ConvertPathToWSL( Directory.GetCurrentDirectory() );
			p.StartInfo.LoadUserProfile = true;

			p.Start();
			p.WaitForExit();

			var ret = p.ExitCode;
			var sr = new StreamReader( tmp_win_path );

			while( !sr.EndOfStream )
				Console.WriteLine( sr.ReadLine() );

			sr.Close();
			File.Delete( tmp_win_path );

			Environment.Exit( ret );
		}

		static string ConvertPathToWSL( string p ) {
			p = Environment.ExpandEnvironmentVariables( p );

			// TODO: Add support for network paths if possible

			return "/mnt/" + p[0].ToString().ToLower() + "/" + p.Substring( 3 ).Replace( '\\', '/' );
		}
	}
}
